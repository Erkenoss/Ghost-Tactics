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

        // POC only
        private const string TargetTypeName = "Crimson.Utilities.OpenOrClosePanel";
        private const string TargetMethodName = "PlayTuto";

        private const string LogPrefix = "[Tutorial Unified IL Hook]";

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
            return compiledAssembly != null &&
                   string.Equals(compiledAssembly.Name, TargetAssemblyName, StringComparison.Ordinal);
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
                TypeDefinition targetType = FindType(module, TargetTypeName);

                if (targetType == null)
                {
                    AddDiagnostic(
                        DiagnosticType.Warning,
                        $"{LogPrefix} Type '{TargetTypeName}' was not found."
                    );

                    return ReturnUnmodified(compiledAssembly);
                }

                MethodDefinition targetMethod = FindTargetMethod(targetType, TargetMethodName);

                if (targetMethod == null)
                {
                    AddDiagnostic(
                        DiagnosticType.Warning,
                        $"{LogPrefix} Method '{TargetTypeName}.{TargetMethodName}' was not found or is not compatible."
                    );

                    return ReturnUnmodified(compiledAssembly);
                }

                MethodReference debugLogMethod = CreateDebugLogMethod(module);
                string methodId = $"{TargetTypeName}.{TargetMethodName}";

                if (IsAlreadyInstrumented(targetMethod, methodId))
                {
                    return ReturnUnmodified(compiledAssembly);
                }

                InjectDebugLog(targetMethod, debugLogMethod, methodId);

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

                byte[] outputPdb = hasSymbols
                    ? pdbOutput.ToArray()
                    : Array.Empty<byte>();

                return new ILPostProcessResult(
                    new InMemoryAssembly(peOutput.ToArray(), outputPdb),
                    diagnostics
                );
            }
            catch (Exception exception)
            {
                AddDiagnostic(
                    DiagnosticType.Error,
                    $"{LogPrefix} Injection failed: {exception}"
                );

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

                if (!method.IsPublic ||
                    method.IsStatic ||
                    method.HasParameters ||
                    method.ReturnType.MetadataType != MetadataType.Void ||
                    !method.HasBody)
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        #endregion

        #region Injection

        private static MethodReference CreateDebugLogMethod(ModuleDefinition module)
        {
            AssemblyNameReference unityEngineAssembly = null;

            foreach (AssemblyNameReference reference in module.AssemblyReferences)
            {
                if (string.Equals(reference.Name, "UnityEngine.CoreModule", StringComparison.Ordinal))
                {
                    unityEngineAssembly = reference;
                    break;
                }
            }

            if (unityEngineAssembly == null)
            {
                throw new InvalidOperationException(
                    "UnityEngine.CoreModule reference was not found inside Assembly-CSharp."
                );
            }

            TypeReference debugType = new TypeReference(
                "UnityEngine",
                "Debug",
                module,
                unityEngineAssembly
            );

            MethodReference debugLogMethod = new MethodReference(
                "Log",
                module.TypeSystem.Void,
                debugType
            )
            {
                HasThis = false,
                ExplicitThis = false,
                CallingConvention = MethodCallingConvention.Default
            };

            debugLogMethod.Parameters.Add(
                new ParameterDefinition(module.TypeSystem.Object)
            );

            return module.ImportReference(debugLogMethod);
        }

        private static bool IsAlreadyInstrumented(MethodDefinition method, string methodId)
        {
            if (method == null || !method.HasBody || method.Body.Instructions.Count < 2)
            {
                return false;
            }

            Instruction firstInstruction = method.Body.Instructions[0];
            Instruction secondInstruction = method.Body.Instructions[1];

            if (firstInstruction.OpCode != OpCodes.Ldstr ||
                !string.Equals(
                    firstInstruction.Operand as string,
                    $"{LogPrefix} {methodId}",
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (secondInstruction.OpCode != OpCodes.Call ||
                secondInstruction.Operand is not MethodReference calledMethod)
            {
                return false;
            }

            return string.Equals(
                calledMethod.DeclaringType.FullName,
                "UnityEngine.Debug",
                StringComparison.Ordinal
            ) &&
            string.Equals(
                calledMethod.Name,
                "Log",
                StringComparison.Ordinal
            );
        }

        private static void InjectDebugLog(
            MethodDefinition method,
            MethodReference debugLogMethod,
            string methodId)
        {
            ILProcessor processor = method.Body.GetILProcessor();
            Instruction firstInstruction = method.Body.Instructions[0];

            processor.InsertBefore(
                firstInstruction,
                processor.Create(
                    OpCodes.Ldstr,
                    $"{LogPrefix} {methodId}"
                )
            );

            processor.InsertBefore(
                firstInstruction,
                processor.Create(
                    OpCodes.Call,
                    debugLogMethod
                )
            );
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
            return new ILPostProcessResult(
                compiledAssembly.InMemoryAssembly,
                diagnostics
            );
        }

        #endregion
    }
}