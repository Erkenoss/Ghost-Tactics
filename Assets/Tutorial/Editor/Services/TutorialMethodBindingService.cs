using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using Tutorial.Runtime.Data;
using Tutorial.Runtime.Components;
using Tutorial.Editor.Core;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Discover compatible MonoBehaviour methods and store their binding inside StepSO assets
    /// </summary>
    internal sealed class TutorialMethodBindingService
    {
        #region Public Methods

        /// <summary>
        /// Get every unique MonoBehaviour type attached to a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public IReadOnlyList<MonoBehaviour> GetScripts(GameObject gameObject)
        {
            List<MonoBehaviour> scripts = new List<MonoBehaviour>();

            if (gameObject == null)
            {
                return scripts;
            }

            MonoBehaviour[] attachedScripts = gameObject.GetComponents<MonoBehaviour>();
            HashSet<string> registeredScriptTypes = new HashSet<string>(StringComparer.Ordinal);

            foreach (MonoBehaviour script in attachedScripts)
            {
                if (script == null)
                {
                    continue;
                }

                string scriptTypeName = script.GetType().FullName;

                if (string.IsNullOrWhiteSpace(scriptTypeName) || !registeredScriptTypes.Add(scriptTypeName))
                {
                    continue;
                }

                scripts.Add(script);
            }

            scripts.Sort((first, second) => string.Compare(first.GetType().Name, second.GetType().Name, StringComparison.Ordinal));

            return scripts;
        }

        /// <summary>
        /// Get every compatible method available on a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public IReadOnlyList<MethodBindingOption> GetBindingOptions(GameObject gameObject)
        {
            List<MethodBindingOption> options = new List<MethodBindingOption>();
            HashSet<string> registeredBindings = new HashSet<string>(StringComparer.Ordinal);

            IReadOnlyList<MonoBehaviour> scripts = GetScripts(gameObject);

            foreach (MonoBehaviour script in scripts)
            {
                IReadOnlyList<MethodBindingOption> scriptOptions = GetScriptBindingOptions(script);

                foreach (MethodBindingOption option in scriptOptions)
                {
                    if (option == null || !option.IsValid)
                    {
                        continue;
                    }

                    string bindingKey = $"{option.StoredScriptName}|{option.StoredMethodName}";

                    if (!registeredBindings.Add(bindingKey))
                    {
                        continue;
                    }

                    options.Add(option);
                }
            }

            options.Sort((first, second) => string.Compare(first.DisplayName, second.DisplayName, StringComparison.Ordinal));

            return options;
        }

        /// <summary>
        /// Get every compatible public method declared by one MonoBehaviour
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public IReadOnlyList<MethodBindingOption> GetScriptBindingOptions(MonoBehaviour script)
        {
            List<MethodBindingOption> options = new List<MethodBindingOption>();

            if (script == null)
            {
                return options;
            }

            MethodInfo[] methods = GetCompatibleMethods(script);

            foreach (MethodInfo method in methods)
            {
                MethodBindingOption option = new MethodBindingOption(script, method);

                if (!option.IsValid)
                {
                    continue;
                }

                options.Add(option);
            }

            return options;
        }

        /// <summary>
        /// Get the dropdown index corresponding to the binding stored inside a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public int GetCurrentBindingIndex(StepSO step, IReadOnlyList<MethodBindingOption> options)
        {
            if (step == null || options == null || string.IsNullOrWhiteSpace(step.ScriptName) || string.IsNullOrWhiteSpace(step.MethodNameToCall))
            {
                return 0;
            }

            for (int i = 0; i < options.Count; i++)
            {
                MethodBindingOption option = options[i];

                if (option == null || !option.IsValid)
                {
                    continue;
                }

                bool sameScript = string.Equals(step.ScriptName, option.StoredScriptName, StringComparison.Ordinal);
                bool sameMethod = string.Equals(step.MethodNameToCall, option.StoredMethodName, StringComparison.Ordinal);

                if (sameScript && sameMethod)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Store a script and method binding inside a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public bool TrySetBinding(StepSO step, MethodBindingOption option)
        {
            if (step == null || option == null || !option.IsValid)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(step.TutoGUID))
            {
                Debug.LogError($"The StepSO '{step.name}' is not linked to a tutorial GameObject.", step);

                return false;
            }

            if (!IsCompatibleMethod(option.Script, option.Method))
            {
                Debug.LogError($"The method '{option.StoredMethodName}' is not compatible with the tutorial system.", option.Script);

                return false;
            }

            if (!TryValidateBindingTarget(step, option.Script))
            {
                return false;
            }

            Undo.RecordObject(step, "Select tutorial script and method");

            step.ScriptName = option.StoredScriptName;
            step.MethodNameToCall = option.StoredMethodName;

            EditorUtility.SetDirty(step);
            AssetDatabase.SaveAssetIfDirty(step);

            return true;
        }

        #endregion

        #region Method Discovery

        /// <summary>
        /// Get every compatible public method declared by a MonoBehaviour
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private static MethodInfo[] GetCompatibleMethods(MonoBehaviour script)
        {
            if (script == null)
            {
                return Array.Empty<MethodInfo>();
            }

            MethodInfo[] methods = script.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            List<MethodInfo> compatibleMethods = new List<MethodInfo>();

            foreach (MethodInfo method in methods)
            {
                if (!IsCompatibleMethod(script, method))
                {
                    continue;
                }

                compatibleMethods.Add(method);
            }

            compatibleMethods.Sort((first, second) => string.Compare(first.Name, second.Name, StringComparison.Ordinal));

            return compatibleMethods.ToArray();
        }

        /// <summary>
        /// Check whether a method can be called by the tutorial system
        /// </summary>
        /// <param name="script"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private static bool IsCompatibleMethod(MonoBehaviour script, MethodInfo method)
        {
            if (script == null || method == null)
            {
                return false;
            }

            if (!method.IsPublic || method.IsStatic || method.IsSpecialName)
            {
                return false;
            }

            if (method.IsGenericMethod || method.ContainsGenericParameters)
            {
                return false;
            }

            if (method.ReturnType != typeof(void))
            {
                return false;
            }

            if (method.GetParameters().Length != 0)
            {
                return false;
            }

            if (method.DeclaringType != script.GetType())
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Target Validation

        /// <summary>
        /// Check whether a method belongs to the GameObject linked to a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        private static bool TryValidateBindingTarget(StepSO step, MonoBehaviour script)
        {
            if (step == null || script == null)
            {
                return false;
            }

            GameObject targetGameObject = script.gameObject;

            if (!targetGameObject.TryGetComponent(out TutoIdentifier identifier))
            {
                Debug.LogError($"The GameObject '{targetGameObject.name}' does not contain a TutoIdentifier.", targetGameObject);

                return false;
            }

            if (string.IsNullOrWhiteSpace(identifier.ObjectGUID))
            {
                Debug.LogError($"The TutoIdentifier of '{targetGameObject.name}' has no object GUID.", targetGameObject);

                return false;
            }

            if (!string.Equals(step.TutoGUID, identifier.ObjectGUID, StringComparison.Ordinal))
            {
                Debug.LogError($"The method cannot be assigned because the StepSO '{step.name}' is linked to another GameObject.", step);

                return false;
            }

            return true;
        }

        #endregion
    }
}