using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using Tutorial.Runtime.Data;
using Tutorial.Editor.Settings;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Manage the folder selection and physical creation of StepSequenceSO assets
    /// </summary>
    public sealed class TutorialSequenceAssetService
    {
        #region Constants

        private const string DefaultSequenceName = "StepSequence";
        private const string SequenceSuffix = "Sequence";

        #endregion

        #region Private Fields

        /// <summary>
        /// Project-wide tutorial tool settings
        /// </summary>
        private readonly TutorialToolProjectSettings settings = null;

        /// <summary>
        /// Use to manage the folder creation recursively
        /// </summary>
        private readonly TutorialAssetPathService assetPathService = null;

        #endregion

        #region Constructor

        public TutorialSequenceAssetService(TutorialToolProjectSettings settings, TutorialAssetPathService assetPathService)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.assetPathService = assetPathService ?? throw new ArgumentNullException(nameof(assetPathService));
        }

        #endregion

        #region Folder

        /// <summary>
        /// Check whether a valid sequence folder is currently configured
        /// </summary>
        /// <returns></returns>
        public bool HasValidSequenceFolder()
        {
            return settings.HasValidSequenceFolder();
        }

        /// <summary>
        /// Get the currently configured sequence folder
        /// </summary>
        /// <returns></returns>
        public string GetSequenceFolderPath()
        {
            return settings.SequenceFolderPath;
        }

        /// <summary>
        /// Open the folder selector and store the selected directory
        /// </summary>
        /// <returns></returns>
        public bool TrySelectSequenceFolder()
        {
            string initialPath = GetInitialAbsolutePath();
            string selectedAbsolutePath = EditorUtility.OpenFolderPanel("Select StepSequenceSO folder", initialPath, string.Empty);

            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
            {
                return false;
            }

            if (!TryConvertAbsolutePathToAssetPath(selectedAbsolutePath, out string assetFolderPath))
            {
                EditorUtility.DisplayDialog(
                    "Invalid sequence folder",
                    "The StepSequenceSO folder must be located inside the Assets directory of the current Unity project.",
                    "Close"
                );

                return false;
            }

            if (!settings.TrySetSequenceFolder(assetFolderPath))
            {
                EditorUtility.DisplayDialog(
                    "Invalid sequence folder",
                    $"The selected directory is not a valid Unity asset folder:\n\n{assetFolderPath}",
                    "Close"
                );

                return false;
            }

            return true;
        }

        /// <summary>
        /// Select and highlight the configured sequence folder in the Project window
        /// </summary>
        public void LocateSequenceFolder()
        {
            if (!settings.TryGetSequenceFolderPath(out string folderPath))
            {
                return;
            }

            DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);

            if (folderAsset == null)
            {
                return;
            }

            Selection.activeObject = folderAsset;
            EditorGUIUtility.PingObject(folderAsset);
        }

        /// <summary>
        /// Clear the currently configured sequence folder
        /// </summary>
        public void ClearSequenceFolder()
        {
            settings.ClearSequenceFolder();
        }

        #endregion

        #region Asset Creation

        /// <summary>
        /// Create a new StepSequenceSO inside the configured sequence folder
        /// </summary>
        /// <param name="sourceStep"></param>
        /// <param name="targetStep"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public bool TryCreateSequenceAsset(StepSO sourceStep, StepSO targetStep, out StepSequenceSO sequence)
        {
            sequence = null;

            if (!TryEnsureSequenceFolder(out string folderPath))
            {
                return false;
            }

            string fileName = BuildSequenceFileName(sourceStep, targetStep);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{fileName}");

            if (!TryCreateSequenceAssetAtPath(assetPath, out sequence, out string failureReason))
            {
                Debug.LogError(failureReason);

                return false;
            }

            return true;
        }

        private bool TryEnsureSequenceFolder(out string folderPath)
        {
            if (assetPathService.TryEnsureFolderExists(settings.SequenceFolderPath, out folderPath, out string failureReason))
            {
                return true;
            }

            Debug.LogError(failureReason);

            return false;
        }

        /// <summary>
        /// Create an empty StepSequenceSO inside the configured sequence folder
        /// </summary>
        /// <param name="requestedName"></param>
        /// <param name="sequence"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryCreateSequenceAsset(string requestedName, out StepSequenceSO sequence, out string failureReason)
        {
            sequence = null;
            failureReason = string.Empty;

            if (!TryEnsureSequenceFolder(out string folderPath))
            {
                failureReason = $"Unable to resolve or create the configured StepSequenceSO folder '{settings.SequenceFolderPath}'.";

                return false;
            }

            string sequenceName = SanitizeFileName(requestedName);

            if (string.IsNullOrWhiteSpace(sequenceName))
            {
                sequenceName = DefaultSequenceName;
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{sequenceName}.asset");

            return TryCreateSequenceAssetAtPath(assetPath, out sequence, out failureReason);
        }

        #endregion

        #region File Name

        /// <summary>
        /// Create a StepSequenceSO at the supplied Unity asset path
        /// </summary>
        private static bool TryCreateSequenceAssetAtPath(string assetPath, out StepSequenceSO sequence, out string failureReason)
        {
            sequence = null;
            failureReason = string.Empty;

            StepSequenceSO createdSequence = ScriptableObject.CreateInstance<StepSequenceSO>();

            if (createdSequence == null)
            {
                failureReason = "Unable to instantiate a StepSequenceSO.";

                return false;
            }

            createdSequence.name = Path.GetFileNameWithoutExtension(assetPath);
            createdSequence.GenerateStepGUID();

            try
            {
                AssetDatabase.CreateAsset(createdSequence, assetPath);
                Undo.RegisterCreatedObjectUndo(createdSequence, "Create tutorial sequence");

                EditorUtility.SetDirty(createdSequence);
                AssetDatabase.SaveAssetIfDirty(createdSequence);

                sequence = createdSequence;

                return true;
            }
            catch (Exception exception)
            {
                failureReason = $"Unable to create StepSequenceSO '{assetPath}'. {exception.Message}";
                Debug.LogException(exception);

                if (AssetDatabase.LoadAssetAtPath<StepSequenceSO>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(createdSequence);
                }

                return false;
            }
        }

        /// <summary>
        /// Build a readable asset name from the connected StepSO assets
        /// </summary>
        /// <param name="sourceStep"></param>
        /// <param name="targetStep"></param>
        /// <returns></returns>
        private static string BuildSequenceFileName(StepSO sourceStep, StepSO targetStep)
        {
            string sourceName = sourceStep != null ? SanitizeFileName(sourceStep.name) : string.Empty;
            string targetName = targetStep != null ? SanitizeFileName(targetStep.name) : string.Empty;

            if (string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(targetName))
            {
                return $"{DefaultSequenceName}.asset";
            }

            return $"{sourceName}_To_{targetName}_{SequenceSuffix}.asset";
        }

        /// <summary>
        /// Remove invalid characters from an asset file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
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

        #region Path Conversion

        /// <summary>
        /// Get the initial absolute directory displayed by the folder selector
        /// </summary>
        /// <returns></returns>
        private string GetInitialAbsolutePath()
        {
            if (!settings.TryGetSequenceFolderPath(out string folderPath))
            {
                return Application.dataPath;
            }

            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;

            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                return Application.dataPath;
            }

            return Path.GetFullPath(Path.Combine(projectRootPath, folderPath));
        }

        /// <summary>
        /// Convert an absolute system path into an Unity Assets path
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private static bool TryConvertAbsolutePathToAssetPath(string absolutePath, out string assetPath)
        {
            assetPath = string.Empty;

            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            string normalizedAbsolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/').TrimEnd('/');
            string normalizedDataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');

            if (string.Equals(normalizedAbsolutePath, normalizedDataPath, StringComparison.OrdinalIgnoreCase))
            {
                assetPath = "Assets";

                return true;
            }

            string dataPathPrefix = $"{normalizedDataPath}/";

            if (!normalizedAbsolutePath.StartsWith(dataPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relativePath = normalizedAbsolutePath.Substring(normalizedDataPath.Length);

            assetPath = $"Assets{relativePath}";

            return AssetDatabase.IsValidFolder(assetPath);
        }

        #endregion
    }
}