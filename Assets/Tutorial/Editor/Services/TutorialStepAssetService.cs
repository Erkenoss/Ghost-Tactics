using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using Tutorial.Editor.Settings;
using Tutorial.Runtime.Data;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Manage physical StepSO asset creation
    /// </summary>
    public sealed class TutorialStepAssetService
    {
        #region Constants

        private const string DefaultStepName = "TutorialStep";

        #endregion

        #region Private Fields

        private readonly TutorialToolProjectSettings settings = null;
        private readonly TutorialAssetPathService assetPathService = null;

        #endregion

        #region Constructor

        public TutorialStepAssetService(TutorialToolProjectSettings settings, TutorialAssetPathService assetPathService)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.assetPathService = assetPathService ?? throw new ArgumentNullException(nameof(assetPathService));
        }

        #endregion

        #region Asset Creation

        /// <summary>
        /// Create a StepSO inside the configured Step folder
        /// </summary>
        public bool TryCreateStepAsset(string requestedName, out StepSO step, out string failureReason)
        {
            step = null;
            failureReason = string.Empty;

            if (!assetPathService.TryEnsureFolderExists(settings.StepFolderPath, out string folderPath, out failureReason))
            {
                return false;
            }

            string stepName = SanitizeFileName(requestedName);

            if (string.IsNullOrWhiteSpace(stepName))
            {
                stepName = DefaultStepName;
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{stepName}.asset");
            StepSO createdStep = ScriptableObject.CreateInstance<StepSO>();

            if (createdStep == null)
            {
                failureReason = "Unable to instantiate a StepSO.";

                return false;
            }

            createdStep.name = Path.GetFileNameWithoutExtension(assetPath);
            createdStep.GenerateStepGUID();

            try
            {
                AssetDatabase.CreateAsset(createdStep, assetPath);
                Undo.RegisterCreatedObjectUndo(createdStep, "Create tutorial step");

                EditorUtility.SetDirty(createdStep);
                AssetDatabase.SaveAssetIfDirty(createdStep);

                step = createdStep;

                return true;
            }
            catch (Exception exception)
            {
                failureReason = $"Unable to create StepSO '{assetPath}'. {exception.Message}";
                Debug.LogException(exception);

                if (AssetDatabase.LoadAssetAtPath<StepSO>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(createdStep);
                }

                return false;
            }
        }

        #endregion

        #region File Name

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            string sanitizedName = fileName.Trim();

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                sanitizedName = sanitizedName.Replace(invalidCharacter, '_');
            }

            sanitizedName = sanitizedName.Replace('/', '_');
            sanitizedName = sanitizedName.Replace('\\', '_');

            return sanitizedName;
        }

        #endregion
    }
}