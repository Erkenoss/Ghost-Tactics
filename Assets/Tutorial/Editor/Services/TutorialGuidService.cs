using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Tutorial.Runtime.Data;
using Tutorial.Runtime.Components;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Generate and validate the GUIDs used by the tutorial system
    /// </summary>
    internal sealed class TutorialGuidService
    {
        #region Constants

        private const string ObjectGuidPropertyName = "objectGUID";

        #endregion

        #region Step GUID

        /// <summary>
        /// Generate the GUID of a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public bool TryGenerateStepGuid(StepSO step)
        {
            if (step == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(step.StepGUID))
            {
                return ValidateStepGuid(step);
            }

            Undo.RecordObject(step, "Generate tutorial StepSO GUID");

            if (!step.GenerateStepGUID())
            {
                Debug.LogError($"Unable to generate the GUID of the StepSO '{step.name}'.", step);

                return false;
            }

            if (!ValidateStepGuid(step))
            {
                return false;
            }

            EditorUtility.SetDirty(step);
            SaveAssetIfPossible(step);

            return true;
        }

        /// <summary>
        /// Validate the GUID of a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private static bool ValidateStepGuid(StepSO step)
        {
            if (step == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(step.StepGUID))
            {
                Debug.LogError($"The StepSO '{step.name}' has no Step GUID. Generate it before creating a binding.", step);

                return false;
            }

            if (IsStepGuidUnique(step, out StepSO duplicatedStep))
            {
                return true;
            }

            if (duplicatedStep == null)
            {
                Debug.LogError($"Unable to validate the GUID of the StepSO '{step.name}'.", step);

                return false;
            }

            Debug.LogError($"The StepSO '{step.name}' and '{duplicatedStep.name}' share the same GUID: {step.StepGUID}.", step);

            Selection.activeObject = duplicatedStep;
            EditorGUIUtility.PingObject(duplicatedStep);

            return false;
        }

        /// <summary>
        /// Check whether a StepSO GUID is unique inside the Project
        /// </summary>
        /// <param name="step"></param>
        /// <param name="duplicatedStep"></param>
        /// <returns></returns>
        private static bool IsStepGuidUnique(StepSO step, out StepSO duplicatedStep)
        {
            duplicatedStep = null;

            if (step == null || string.IsNullOrWhiteSpace(step.StepGUID))
            {
                return false;
            }

            string[] assetGuids = AssetDatabase.FindAssets("t:StepSO");

            foreach (string assetGuid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                StepSO otherStep = AssetDatabase.LoadAssetAtPath<StepSO>(assetPath);

                if (otherStep == null || otherStep == step)
                {
                    continue;
                }

                if (!string.Equals(otherStep.StepGUID, step.StepGUID, StringComparison.Ordinal))
                {
                    continue;
                }

                duplicatedStep = otherStep;

                return false;
            }

            return true;
        }

        #endregion

        #region Binding Preparation

        /// <summary>
        /// Validate the StepSO and TutoIdentifier GUIDs before creating a binding
        /// </summary>
        /// <param name="step"></param>
        /// <param name="identifier"></param>
        /// <param name="objectGuid"></param>
        /// <returns></returns>
        public bool TryPrepareBinding(StepSO step, TutoIdentifier identifier, out string objectGuid)
        {
            objectGuid = string.Empty;

            if (step == null || identifier == null)
            {
                return false;
            }

            if (!ValidateStepGuid(step))
            {
                return false;
            }

            if (!ValidateIdentifierTarget(identifier))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(identifier.ObjectGUID))
            {
                if (!TryGenerateObjectGuid(identifier, out objectGuid))
                {
                    return false;
                }
            }
            else
            {
                objectGuid = identifier.ObjectGUID;
            }

            if (!ValidateObjectGuid(identifier))
            {
                objectGuid = string.Empty;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Check whether a TutoIdentifier belongs to a valid scene object
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private static bool ValidateIdentifierTarget(TutoIdentifier identifier)
        {
            if (identifier == null)
            {
                return false;
            }

            if (EditorUtility.IsPersistent(identifier))
            {
                Debug.LogError($"The TutoIdentifier '{identifier.name}' belongs to a persistent Project asset instead of a scene object.", identifier);

                return false;
            }

            if (!identifier.gameObject.scene.IsValid() || !identifier.gameObject.scene.isLoaded)
            {
                Debug.LogError($"The GameObject '{identifier.gameObject.name}' does not belong to a valid loaded scene.", identifier.gameObject);

                return false;
            }

            return true;
        }

        #endregion

        #region Object GUID

        /// <summary>
        /// Generate the object GUID of a TutoIdentifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="objectGuid"></param>
        /// <returns></returns>
        private static bool TryGenerateObjectGuid(TutoIdentifier identifier, out string objectGuid)
        {
            objectGuid = string.Empty;

            if (identifier == null)
            {
                return false;
            }

            SerializedObject serializedIdentifier = new SerializedObject(identifier);

            serializedIdentifier.Update();

            SerializedProperty objectGuidProperty = serializedIdentifier.FindProperty(ObjectGuidPropertyName);

            if (objectGuidProperty == null)
            {
                Debug.LogError($"The serialized field '{ObjectGuidPropertyName}' was not found on '{identifier.name}'.", identifier);

                return false;
            }

            if (!string.IsNullOrWhiteSpace(objectGuidProperty.stringValue))
            {
                objectGuid = objectGuidProperty.stringValue;

                return true;
            }

            Undo.RecordObject(identifier, "Generate tutorial object GUID");

            objectGuid = Guid.NewGuid().ToString("N");
            objectGuidProperty.stringValue = objectGuid;

            serializedIdentifier.ApplyModifiedProperties();

            EditorUtility.SetDirty(identifier);
            PrefabUtility.RecordPrefabInstancePropertyModifications(identifier);

            if (identifier.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(identifier.gameObject.scene);
            }

            return true;
        }

        /// <summary>
        /// Validate the GUID of a TutoIdentifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private static bool ValidateObjectGuid(TutoIdentifier identifier)
        {
            if (identifier == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(identifier.ObjectGUID))
            {
                Debug.LogError($"The GameObject '{identifier.gameObject.name}' has no tutorial object GUID.", identifier.gameObject);

                return false;
            }

            if (IsObjectGuidUnique(identifier, out TutoIdentifier duplicatedIdentifier))
            {
                return true;
            }

            if (duplicatedIdentifier == null)
            {
                Debug.LogError($"Unable to validate the tutorial GUID of '{identifier.gameObject.name}'.", identifier.gameObject);

                return false;
            }

            Debug.LogError($"The GameObjects '{identifier.gameObject.name}' and '{duplicatedIdentifier.gameObject.name}' share the same tutorial GUID: {identifier.ObjectGUID}.", identifier.gameObject);

            Selection.activeObject = duplicatedIdentifier.gameObject;
            EditorGUIUtility.PingObject(duplicatedIdentifier.gameObject);

            return false;
        }

        /// <summary>
        /// Check whether a TutoIdentifier GUID is unique among the loaded scene objects
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="duplicatedIdentifier"></param>
        /// <returns></returns>
        private static bool IsObjectGuidUnique(TutoIdentifier identifier, out TutoIdentifier duplicatedIdentifier)
        {
            duplicatedIdentifier = null;

            if (identifier == null || string.IsNullOrWhiteSpace(identifier.ObjectGUID))
            {
                return false;
            }

            TutoIdentifier[] identifiers = Resources.FindObjectsOfTypeAll<TutoIdentifier>();

            foreach (TutoIdentifier otherIdentifier in identifiers)
            {
                if (otherIdentifier == null || otherIdentifier == identifier)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(otherIdentifier))
                {
                    continue;
                }

                if (!otherIdentifier.gameObject.scene.IsValid() || !otherIdentifier.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (!string.Equals(otherIdentifier.ObjectGUID, identifier.ObjectGUID, StringComparison.Ordinal))
                {
                    continue;
                }

                duplicatedIdentifier = otherIdentifier;

                return false;
            }

            return true;
        }

        #endregion

        #region Asset

        /// <summary>
        /// Save an object when it belongs to a Project asset
        /// </summary>
        /// <param name="target"></param>
        private static void SaveAssetIfPossible(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(target);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            AssetDatabase.SaveAssetIfDirty(target);
        }

        #endregion
    }
}