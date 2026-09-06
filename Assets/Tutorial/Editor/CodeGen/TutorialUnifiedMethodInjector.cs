using System;
using System.Collections.Generic;
using System.IO;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

using ILPostProcessorBase = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;

namespace Tutorial.CodeGen
{
    /// <summary>
    /// Inject tutorial hooks directly after Assembly-CSharp compilation
    /// </summary>
    internal sealed class TutorialUnifiedMethodInjector : ILPostProcessorBase
    {
        #region Constants

        private const string TargetAssemblyName = "Assembly-CSharp";

        private const string NotifierTypeName = "Tutorial.Runtime.Hooks.TutorialMethodNotifier";
        private const string StepNotifierMethodName = "Notify";
        private const string SkipNotifierMethodName = "NotifySkip";

        private const string DiagnosticPrefix = "[Tutorial Unified IL Hook]";

        #endregion

        #region Private Fields

        private readonly List<DiagnosticMessage> diagnostics = new List<DiagnosticMessage>();

        #endregion

        #region IL Post Processor

        public override ILPostProcessorBase GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly != null && string.Equals(compiledAssembly.Name, TargetAssemblyName, StringComparison.Ordinal);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            diagnostics.Clear();

            try
            {
                if (!TutorialInstrumentationManifestReader.TryRead(compiledAssembly, out List<TutorialInstrumentationBinding> bindings, out TutorialInstrumentationSkipBinding skipBinding, out string manifestFailureReason))
                {
                    AddDiagnostic(DiagnosticType.Error, $"{DiagnosticPrefix} {manifestFailureReason}");
                    return ReturnUnmodified(compiledAssembly);
                }

                if (bindings.Count == 0 && skipBinding == null)
                {
                    return ReturnUnmodified(compiledAssembly);
                }

                using DefaultAssemblyResolver resolver = CreateAssemblyResolver(compiledAssembly);

                byte[] pdbData = compiledAssembly.InMemoryAssembly.PdbData;
                bool hasSymbols = pdbData != null && pdbData.Length > 0;

                using MemoryStream peInput = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData, false);
                using MemoryStream pdbInput = hasSymbols ? new MemoryStream(pdbData, false) : null;

                ReaderParameters readerParameters = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Immediate,
                    ReadSymbols = hasSymbols
                };

                if (hasSymbols)
                {
                    readerParameters.SymbolStream = pdbInput;
                    readerParameters.SymbolReaderProvider = new PortablePdbReaderProvider();
                }

                using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(peInput, readerParameters);

                ModuleDefinition module = assembly.MainModule;
                MethodReference notifyMethod = bindings.Count > 0 ? FindStepNotifierMethod(module) : null;
                MethodReference notifySkipMethod = skipBinding != null ? FindSkipNotifierMethod(module) : null;
                bool assemblyModified = false;

                foreach (TutorialInstrumentationBinding binding in bindings)
                {
                    if (binding == null)
                    {
                        continue;
                    }

                    TypeDefinition targetType = FindType(module, binding.ScriptName);

                    if (targetType == null)
                    {
                        AddDiagnostic(DiagnosticType.Warning, $"{DiagnosticPrefix} Type '{binding.ScriptName}' was not found for Step '{binding.StepGuid}'.");
                        continue;
                    }

                    MethodDefinition targetMethod = FindTargetMethod(targetType, binding.MethodName);

                    if (targetMethod == null)
                    {
                        AddDiagnostic(DiagnosticType.Warning, $"{DiagnosticPrefix} Method '{binding.ScriptName}.{binding.MethodName}' was not found or is not compatible for Step '{binding.StepGuid}'.");
                        continue;
                    }

                    if (IsAlreadyInstrumented(targetMethod, notifyMethod, binding.StepGuid))
                    {
                        continue;
                    }

                    InjectNotify(targetMethod, notifyMethod, binding.StepGuid);
                    assemblyModified = true;
                }

                if (skipBinding != null)
                {
                    TypeDefinition skipTargetType = FindType(module, skipBinding.ScriptName);

                    if (skipTargetType == null)
                    {
                        AddDiagnostic(DiagnosticType.Warning, $"{DiagnosticPrefix} Type '{skipBinding.ScriptName}' was not found for the global Skip Current Step binding.");
                    }
                    else
                    {
                        MethodDefinition skipTargetMethod = FindTargetMethod(skipTargetType, skipBinding.MethodName);

                        if (skipTargetMethod == null)
                        {
                            AddDiagnostic(DiagnosticType.Warning, $"{DiagnosticPrefix} Method '{skipBinding.ScriptName}.{skipBinding.MethodName}' was not found or is not compatible for the global Skip Current Step binding.");
                        }
                        else if (!IsSkipAlreadyInstrumented(skipTargetMethod, notifySkipMethod))
                        {
                            InjectSkipNotify(skipTargetMethod, notifySkipMethod);
                            assemblyModified = true;
                        }
                    }
                }

                if (!assemblyModified)
                {
                    return ReturnUnmodified(compiledAssembly);
                }

                using MemoryStream peOutput = new MemoryStream();
                using MemoryStream pdbOutput = new MemoryStream();

                WriterParameters writerParameters = new WriterParameters
                {
                    WriteSymbols = hasSymbols
                };

                if (hasSymbols)
                {
                    writerParameters.SymbolStream = pdbOutput;
                    writerParameters.SymbolWriterProvider = new PortablePdbWriterProvider();
                }

                assembly.Write(peOutput, writerParameters);
                byte[] outputPdb = hasSymbols ? pdbOutput.ToArray() : Array.Empty<byte>();
                return new ILPostProcessResult(new InMemoryAssembly(peOutput.ToArray(), outputPdb), diagnostics);
            }
            catch (Exception exception)
            {
                AddDiagnostic(DiagnosticType.Error, $"{DiagnosticPrefix} Injection failed: {exception}");
                return ReturnUnmodified(compiledAssembly);
            }
        }

        #endregion

        #region Assembly Resolution

        /// <summary>
        /// Create Cecil resolver from references provided by Unity compilation
        /// </summary>
        private static DefaultAssemblyResolver CreateAssemblyResolver(ICompiledAssembly compiledAssembly)
        {
            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string reference in compiledAssembly.References)
            {
                string directory = Path.GetDirectoryName(reference);

                if (string.IsNullOrWhiteSpace(directory) || !directories.Add(directory))
                {
                    continue;
                }

                resolver.AddSearchDirectory(directory);
            }

            return resolver;
        }

        #endregion

        #region Type Resolution

        private static TypeDefinition FindType(ModuleDefinition module, string typeName)
        {
            if (module == null || string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            string normalizedTypeName = typeName.Replace('+', '/');

            foreach (TypeDefinition type in module.Types)
            {
                TypeDefinition result = FindTypeRecursive(type, normalizedTypeName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static TypeDefinition FindTypeRecursive(TypeDefinition type, string typeName)
        {
            if (type == null)
            {
                return null;
            }

            if (string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                return type;
            }

            if (!type.HasNestedTypes)
            {
                return null;
            }

            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                TypeDefinition result = FindTypeRecursive(nestedType, typeName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        #endregion

        #region Method Resolution

        private static MethodDefinition FindTargetMethod(TypeDefinition type, string methodName)
        {
            if (type == null || string.IsNullOrWhiteSpace(methodName))
            {
                return null;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!method.IsPublic || method.IsStatic || method.HasParameters || method.ReturnType.MetadataType != MetadataType.Void || !method.HasBody)
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        /// <summary>
        /// Find the Step notification method used by injected tutorial bindings
        /// </summary>
        private static MethodReference FindStepNotifierMethod(ModuleDefinition module)
        {
            TypeDefinition notifierType = FindType(module, NotifierTypeName);

            if (notifierType == null)
            {
                throw new InvalidOperationException($"Notifier type '{NotifierTypeName}' was not found inside Assembly-CSharp.");
            }

            foreach (MethodDefinition method in notifierType.Methods)
            {
                if (!string.Equals(method.Name, StepNotifierMethodName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!method.IsPublic || !method.IsStatic || method.Parameters.Count != 1 || method.Parameters[0].ParameterType.MetadataType != MetadataType.String || method.ReturnType.MetadataType != MetadataType.Void || !method.HasBody)
                {
                    continue;
                }

                return module.ImportReference(method);
            }

            throw new InvalidOperationException($"Notifier method '{NotifierTypeName}.{StepNotifierMethodName}(string)' was not found.");
        }

        /// <summary>
        /// Find the global Skip notification method
        /// </summary>
        private static MethodReference FindSkipNotifierMethod(ModuleDefinition module)
        {
            TypeDefinition notifierType = FindType(module, NotifierTypeName);

            if (notifierType == null)
            {
                throw new InvalidOperationException($"Notifier type '{NotifierTypeName}' was not found inside Assembly-CSharp.");
            }

            foreach (MethodDefinition method in notifierType.Methods)
            {
                if (!string.Equals(method.Name, SkipNotifierMethodName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!method.IsPublic || !method.IsStatic || method.Parameters.Count != 0 || method.ReturnType.MetadataType != MetadataType.Void || !method.HasBody)
                {
                    continue;
                }

                return module.ImportReference(method);
            }

            throw new InvalidOperationException($"Notifier method '{NotifierTypeName}.{SkipNotifierMethodName}()' was not found.");
        }

        #endregion

        #region Injection

        /// <summary>
        /// Determine whether the global Skip notifier is already injected inside one method
        /// </summary>
        private static bool IsSkipAlreadyInstrumented(MethodDefinition method, MethodReference notifySkipMethod)
        {
            if (method == null || !method.HasBody)
            {
                return false;
            }

            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.OpCode != OpCodes.Call || instruction.Operand is not MethodReference calledMethod)
                {
                    continue;
                }

                bool sameType = string.Equals(calledMethod.DeclaringType.FullName, notifySkipMethod.DeclaringType.FullName, StringComparison.Ordinal);
                bool sameMethod = string.Equals(calledMethod.Name, notifySkipMethod.Name, StringComparison.Ordinal);

                if (sameType && sameMethod)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Inject the global Skip Current Step notification at the beginning of one gameplay method
        /// </summary>
        private static void InjectSkipNotify(MethodDefinition method, MethodReference notifySkipMethod)
        {
            ILProcessor processor = method.Body.GetILProcessor();
            Instruction firstInstruction = method.Body.Instructions[0];

            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Call, notifySkipMethod));
        }

        private static bool IsAlreadyInstrumented(MethodDefinition method, MethodReference notifyMethod, string stepGUID)
        {
            if (method == null || !method.HasBody || method.Body.Instructions.Count < 2)
            {
                return false;
            }

            IList<Instruction> instructions = method.Body.Instructions;

            for (int i = 0; i < instructions.Count - 1; i++)
            {
                Instruction guidInstruction = instructions[i];
                Instruction callInstruction = instructions[i + 1];

                if (guidInstruction.OpCode != OpCodes.Ldstr || !string.Equals(guidInstruction.Operand as string, stepGUID, StringComparison.Ordinal))
                {
                    continue;
                }

                if (callInstruction.OpCode != OpCodes.Call || callInstruction.Operand is not MethodReference calledMethod)
                {
                    continue;
                }

                bool sameType = string.Equals(calledMethod.DeclaringType.FullName, notifyMethod.DeclaringType.FullName, StringComparison.Ordinal);
                bool sameMethod = string.Equals(calledMethod.Name, notifyMethod.Name, StringComparison.Ordinal);

                if (sameType && sameMethod)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InjectNotify(MethodDefinition method, MethodReference notifyMethod, string stepGUID)
        {
            ILProcessor processor = method.Body.GetILProcessor();
            Instruction firstInstruction = method.Body.Instructions[0];

            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldstr, stepGUID));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Call, notifyMethod));
        }

        #endregion

        #region Diagnostics

        private void AddDiagnostic(DiagnosticType diagnosticType, string message)
        {
            diagnostics.Add(
                new DiagnosticMessage
                {
                    DiagnosticType = diagnosticType,
                    MessageData = message,
                    File = string.Empty,
                    Line = 0,
                    Column = 0
                }
            );
        }

        private ILPostProcessResult ReturnUnmodified(ICompiledAssembly compiledAssembly)
        {
            return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);
        }

        #endregion
    }
}