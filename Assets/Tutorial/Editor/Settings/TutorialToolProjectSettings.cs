using System;
using UnityEditor;
using UnityEngine;

namespace Tutorial.Editor.Settings
{
    /// <summary>
    /// Store project-wide settings used by the tutorial editor tool
    /// </summary>
    [FilePath(SettingsFilePath, FilePathAttribute.Location.ProjectFolder)]
    public sealed class TutorialToolProjectSettings : ScriptableSingleton<TutorialToolProjectSettings>
    {
        #region Constants

        private const string SettingsFilePath = "ProjectSettings/TutorialToolSettings.asset";
        private const string DefaultStepFolderPath = "Assets/Tutorial/Steps";
        private const string DefaultSequenceFolderPath = "Assets/Tutorial/Sequences";

        #endregion

        #region Public Properties

        public string StepFolderPath => stepFolderPath;
        public string SequenceFolderPath => sequenceFolderPath;

        public string GraphFolderPath
        {
            get
            {
                return TryGetGraphFolderPath(out string folderPath) ? folderPath : string.Empty;
            }
        }

        public string SkipScriptName => skipScriptName;
        public string SkipMethodName => skipMethodName;
        public bool HasSkipBinding => !string.IsNullOrWhiteSpace(skipScriptName) && !string.IsNullOrWhiteSpace(skipMethodName);

        #endregion

        #region Serialized Fields

        [SerializeField]
        private string stepFolderPath = DefaultStepFolderPath;

        [SerializeField]
        private string sequenceFolderPath = DefaultSequenceFolderPath;

        [SerializeField]
        private string graphFolderGuid = string.Empty;

        /// <summary>
        /// Full type name of the MonoBehaviour containing the global Skip Current Step method
        /// </summary>
        [SerializeField]
        private string skipScriptName = string.Empty;

        /// <summary>
        /// Name of the public method used as the global Skip Current Step entry point
        /// </summary>
        [SerializeField]
        private string skipMethodName = string.Empty;

        #endregion

        #region Step Folder

        /// <summary>
        /// Check whether the configured StepSO folder currently exists
        /// </summary>
        public bool HasValidStepFolder()
        {
            return TryGetStepFolderPath(out _);
        }

        /// <summary>
        /// Try to resolve the configured StepSO folder when it already exists
        /// </summary>
        public bool TryGetStepFolderPath(out string folderPath)
        {
            folderPath = stepFolderPath;

            return !string.IsNullOrWhiteSpace(folderPath) && AssetDatabase.IsValidFolder(folderPath);
        }

        /// <summary>
        /// Store the desired StepSO folder path. The folder does not need to exist yet.
        /// </summary>
        public bool TrySetStepFolder(string folderPath)
        {
            if (!TryNormalizeAssetFolderPath(folderPath, out string normalizedPath))
            {
                return false;
            }

            if (string.Equals(stepFolderPath, normalizedPath, StringComparison.Ordinal))
            {
                return true;
            }

            stepFolderPath = normalizedPath;
            SaveSettings();

            return true;
        }

        public void ClearStepFolder()
        {
            if (string.IsNullOrWhiteSpace(stepFolderPath))
            {
                return;
            }

            stepFolderPath = string.Empty;
            SaveSettings();
        }

        #endregion

        #region Sequence Folder

        /// <summary>
        /// Check whether the configured StepSequenceSO folder currently exists
        /// </summary>
        public bool HasValidSequenceFolder()
        {
            return TryGetSequenceFolderPath(out _);
        }

        /// <summary>
        /// Try to resolve the configured StepSequenceSO folder when it already exists
        /// </summary>
        public bool TryGetSequenceFolderPath(out string folderPath)
        {
            folderPath = sequenceFolderPath;

            return !string.IsNullOrWhiteSpace(folderPath) && AssetDatabase.IsValidFolder(folderPath);
        }

        /// <summary>
        /// Store the desired StepSequenceSO folder path. The folder does not need to exist yet.
        /// </summary>
        public bool TrySetSequenceFolder(string folderPath)
        {
            if (!TryNormalizeAssetFolderPath(folderPath, out string normalizedPath))
            {
                return false;
            }

            if (string.Equals(sequenceFolderPath, normalizedPath, StringComparison.Ordinal))
            {
                return true;
            }

            sequenceFolderPath = normalizedPath;
            SaveSettings();

            return true;
        }

        public void ClearSequenceFolder()
        {
            if (string.IsNullOrWhiteSpace(sequenceFolderPath))
            {
                return;
            }

            sequenceFolderPath = string.Empty;
            SaveSettings();
        }

        #endregion

        #region Graph Folder

        public bool HasValidGraphFolder()
        {
            return TryGetGraphFolderPath(out _);
        }

        public bool TryGetGraphFolderPath(out string folderPath)
        {
            return TryResolveFolderGuid(graphFolderGuid, out folderPath);
        }

        public bool TrySetGraphFolder(string folderPath)
        {
            if (!TryGetFolderGuid(folderPath, out string folderGuid))
            {
                return false;
            }

            if (string.Equals(graphFolderGuid, folderGuid, StringComparison.Ordinal))
            {
                return true;
            }

            graphFolderGuid = folderGuid;
            SaveSettings();

            return true;
        }

        public void ClearGraphFolder()
        {
            if (string.IsNullOrWhiteSpace(graphFolderGuid))
            {
                return;
            }

            graphFolderGuid = string.Empty;
            SaveSettings();
        }

        #endregion

        #region Reset

        public void ResetToDefaults()
        {
            stepFolderPath = DefaultStepFolderPath;
            sequenceFolderPath = DefaultSequenceFolderPath;
            graphFolderGuid = string.Empty;
            skipScriptName = string.Empty;
            skipMethodName = string.Empty;

            SaveSettings();
        }

        #endregion

        #region Folder Validation

        private static bool TryResolveFolderGuid(string folderGuid, out string folderPath)
        {
            folderPath = string.Empty;

            if (string.IsNullOrWhiteSpace(folderGuid))
            {
                return false;
            }

            folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);

            if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                folderPath = string.Empty;

                return false;
            }

            return true;
        }

        private static bool TryGetFolderGuid(string folderPath, out string folderGuid)
        {
            folderGuid = string.Empty;

            if (!TryNormalizeAssetFolderPath(folderPath, out string normalizedPath) ||
                !AssetDatabase.IsValidFolder(normalizedPath))
            {
                return false;
            }

            folderGuid = AssetDatabase.AssetPathToGUID(normalizedPath);

            return !string.IsNullOrWhiteSpace(folderGuid);
        }

        private static bool TryNormalizeAssetFolderPath(string folderPath, out string normalizedPath)
        {
            normalizedPath = string.Empty;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            normalizedPath = folderPath.Trim().Replace('\\', '/');

            while (normalizedPath.Contains("//"))
            {
                normalizedPath = normalizedPath.Replace("//", "/");
            }

            normalizedPath = normalizedPath.TrimEnd('/');

            if (string.Equals(normalizedPath, "Assets", StringComparison.Ordinal))
            {
                return true;
            }

            return normalizedPath.StartsWith("Assets/", StringComparison.Ordinal);
        }

        #endregion

        #region Skip Binding

        /// <summary>
        /// Store the method used as the global Skip Current Step entry point
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public bool TrySetSkipBinding(string scriptName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(scriptName) || string.IsNullOrWhiteSpace(methodName))
            {
                return false;
            }

            scriptName = scriptName.Trim();
            methodName = methodName.Trim();

            if (string.Equals(skipScriptName, scriptName, StringComparison.Ordinal) && string.Equals(skipMethodName, methodName, StringComparison.Ordinal))
            {
                return true;
            }

            skipScriptName = scriptName;
            skipMethodName = methodName;

            SaveSettings();

            return true;
        }

        /// <summary>
        /// Remove the global Skip Current Step method binding
        /// </summary>
        public void ClearSkipBinding()
        {
            if (string.IsNullOrWhiteSpace(skipScriptName) && string.IsNullOrWhiteSpace(skipMethodName))
            {
                return;
            }

            skipScriptName = string.Empty;
            skipMethodName = string.Empty;

            SaveSettings();
        }

        #endregion

        #region Persistence

        private void SaveSettings()
        {
            Save(true);
        }

        #endregion
    }
}