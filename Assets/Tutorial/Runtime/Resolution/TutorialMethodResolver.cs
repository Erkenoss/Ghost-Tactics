using System;
using System.Reflection;
using Tutorial.Runtime.Component;
using Tutorial.Runtime.Data;
using UnityEngine;

namespace Tutorial.Runtime.Resolution
{
    /// <summary>
    /// Resolve the runtime GameObject, script and method associated with a tutorial StepSO
    /// </summary>
    public sealed class TutorialMethodResolver
    {
        #region Private Fields

        /// <summary>
        /// Registry containing every currently available tutorial identifier
        /// </summary>
        private readonly TutorialIdentifierRegistry identifierRegistry = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a tutorial method resolver using the provided identifier registry
        /// </summary>
        /// <param name="identifierRegistry"></param>
        public TutorialMethodResolver(TutorialIdentifierRegistry identifierRegistry)
        {
            this.identifierRegistry = identifierRegistry ?? throw new ArgumentNullException(nameof(identifierRegistry));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resolve every runtime reference associated with one StepSO method binding
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="resolvedMethod"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryResolve(StepSO runtimeStep, out TutorialResolvedMethod resolvedMethod, out string error)
        {
            resolvedMethod = null;
            error = string.Empty;

            if (!TryValidateStepBinding(runtimeStep, out error))
            {
                return false;
            }

            if (!TryResolveIdentifier(runtimeStep, out TutoIdentifier identifier, out error))
            {
                return false;
            }

            if (!TryResolveScript(runtimeStep, identifier, out MonoBehaviour script, out error))
            {
                return false;
            }

            if (!TryResolveMethod(runtimeStep, script, out MethodInfo method, out error))
            {
                return false;
            }

            resolvedMethod = new TutorialResolvedMethod(runtimeStep, identifier, script, method);

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate that one StepSO contains every value required to resolve its method binding
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateStepBinding(StepSO runtimeStep, out string error)
        {
            error = string.Empty;

            if (runtimeStep == null)
            {
                error = "The runtime StepSO is null.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(runtimeStep.TutoGUID))
            {
                error = $"StepSO '{runtimeStep.name}' contains no TutoGUID.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(runtimeStep.ScriptName))
            {
                error = $"StepSO '{runtimeStep.name}' contains no ScriptName.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(runtimeStep.MethodNameToCall))
            {
                error = $"StepSO '{runtimeStep.name}' contains no MethodNameToCall.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolve the TutoIdentifier referenced by the StepSO persistent object GUID
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="identifier"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryResolveIdentifier(StepSO runtimeStep, out TutoIdentifier identifier, out string error)
        {
            identifier = null;
            error = string.Empty;

            if (!identifierRegistry.TryGet(runtimeStep.TutoGUID, out identifier))
            {
                error =
                    $"No loaded TutoIdentifier could be resolved for StepSO '{runtimeStep.name}' " +
                    $"with TutoGUID '{runtimeStep.TutoGUID}'.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolve the MonoBehaviour whose full type name matches the StepSO script binding
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="identifier"></param>
        /// <param name="script"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryResolveScript(StepSO runtimeStep, TutoIdentifier identifier, out MonoBehaviour script, out string error)
        {
            script = null;
            error = string.Empty;

            // INTENTIONAL GETCOMPONENTS:
            // Required to resolve a serialized script type name to its runtime
            // MonoBehaviour instance on an already identified GameObject.

            // Double binding is intentionally avoided.
            // Storing the MonoBehaviour reference in TutoIdentifier would duplicate binding data already held by StepSO.
            // This would require synchronization between both representations and introduce additional failure states.
            // The localized GetComponents call is therefore preferred to keep a single source of truth.

            MonoBehaviour[] scripts = identifier.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour candidate in scripts)
            {
                if (candidate == null)
                {
                    continue;
                }

                Type candidateType = candidate.GetType();

                if (!string.Equals(candidateType.FullName, runtimeStep.ScriptName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (script != null)
                {
                    error =
                        $"GameObject '{identifier.gameObject.name}' contains more than one component " +
                        $"matching script type '{runtimeStep.ScriptName}'.";

                    return false;
                }

                script = candidate;
            }

            if (script == null)
            {
                error =
                    $"GameObject '{identifier.gameObject.name}' contains no MonoBehaviour matching " +
                    $"script type '{runtimeStep.ScriptName}'.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolve and validate the public parameterless void method stored by the StepSO binding
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="script"></param>
        /// <param name="method"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryResolveMethod(StepSO runtimeStep, MonoBehaviour script, out MethodInfo method, out string error)
        {
            method = null;
            error = string.Empty;

            MethodInfo[] methods = script.GetType().GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly
            );

            foreach (MethodInfo candidate in methods)
            {
                if (!string.Equals(candidate.Name, runtimeStep.MethodNameToCall, StringComparison.Ordinal))
                {
                    continue;
                }

                if (candidate.IsSpecialName)
                {
                    continue;
                }

                if (candidate.IsGenericMethod)
                {
                    continue;
                }

                if (candidate.GetParameters().Length != 0)
                {
                    continue;
                }

                if (candidate.ReturnType != typeof(void))
                {
                    continue;
                }

                if (method != null)
                {
                    error =
                        $"Script '{runtimeStep.ScriptName}' contains more than one compatible method " +
                        $"named '{runtimeStep.MethodNameToCall}'.";

                    return false;
                }

                method = candidate;
            }

            if (method == null)
            {
                error =
                    $"No compatible public parameterless void method named '{runtimeStep.MethodNameToCall}' " +
                    $"was found on script '{runtimeStep.ScriptName}'.";

                return false;
            }

            return true;
        }

        #endregion
    }
}