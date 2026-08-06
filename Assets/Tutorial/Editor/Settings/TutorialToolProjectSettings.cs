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

        /// <summary>
        /// Project-relative path of the persistent settings file
        /// </summary>
        private const string SettingsFilePath = "ProjectSettings/TutorialToolSettings.asset";

        /// <summary>
        /// Default delay applied before an automatic graph save
        /// </summary>
        public const float DefaultAutosaveDelay = 1.25f;

        /// <summary>
        /// Minimum accepted autosave delay
        /// </summary>
        public const float MinimumAutosaveDelay = 0.1f;

        /// <summary>
        /// Maximum accepted autosave delay
        /// </summary>
        public const float MaximumAutosaveDelay = 60f;

        #endregion

        #region Public Properties

        /// <summary>
        /// Current StepSequenceSO folder path
        /// </summary>
        public string SequenceFolderPath
        {
            get
            {
                return TryGetSequenceFolderPath(out string folderPath) ? folderPath : string.Empty;
            }
        }

        /// <summary>
        /// Current TutorialGraphAsset folder path
        /// </summary>
        public string GraphFolderPath
        {
            get
            {
                return TryGetGraphFolderPath(out string folderPath) ? folderPath : string.Empty;
            }
        }

        /// <summary>
        /// Whether automatic graph saving is enabled
        /// </summary>
        public bool AutosaveEnabled
        {
            get { return autosaveEnabled; }
        }

        /// <summary>
        /// Delay applied before an automatic graph save
        /// </summary>
        public double AutosaveDelay
        {
            get { return autosaveDelay; }
        }

        #endregion

        #region Serialized Fields

        /// <summary>
        /// GUID of the folder containing StepSequenceSO assets
        /// </summary>
        [SerializeField]
        private string sequenceFolderGuid = string.Empty;

        /// <summary>
        /// GUID of the folder containing TutorialGraphAsset assets
        /// </summary>
        [SerializeField]
        private string graphFolderGuid = string.Empty;

        /// <summary>
        /// Whether automatic graph saving is enabled
        /// </summary>
        [SerializeField]
        private bool autosaveEnabled = true;

        /// <summary>
        /// Delay applied before an automatic graph save
        /// </summary>
        [SerializeField]
        private float autosaveDelay = DefaultAutosaveDelay;

        #endregion

        #region Sequence Folder

        /// <summary>
        /// Check whether a valid StepSequenceSO folder is configured
        /// </summary>
        /// <returns></returns>
        public bool HasValidSequenceFolder()
        {
            return TryGetSequenceFolderPath(out _);
        }

        /// <summary>
        /// Try to resolve the configured StepSequenceSO folder
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public bool TryGetSequenceFolderPath(out string folderPath)
        {
            return TryResolveFolderGuid(sequenceFolderGuid, out folderPath);
        }

        /// <summary>
        /// Store the folder used for StepSequenceSO assets
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public bool TrySetSequenceFolder(string folderPath)
        {
            if (!TryGetFolderGuid(folderPath, out string folderGuid))
            {
                return false;
            }

            if (string.Equals(sequenceFolderGuid, folderGuid, StringComparison.Ordinal))
            {
                return true;
            }

            sequenceFolderGuid = folderGuid;

            SaveSettings();

            return true;
        }

        /// <summary>
        /// Clear the configured StepSequenceSO folder
        /// </summary>
        public void ClearSequenceFolder()
        {
            if (string.IsNullOrWhiteSpace(sequenceFolderGuid))
            {
                return;
            }

            sequenceFolderGuid = string.Empty;

            SaveSettings();
        }

        #endregion

        #region Graph Folder

        /// <summary>
        /// Check whether a valid TutorialGraphAsset folder is configured
        /// </summary>
        /// <returns></returns>
        public bool HasValidGraphFolder()
        {
            return TryGetGraphFolderPath(out _);
        }

        /// <summary>
        /// Try to resolve the configured TutorialGraphAsset folder
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public bool TryGetGraphFolderPath(out string folderPath)
        {
            return TryResolveFolderGuid(graphFolderGuid, out folderPath);
        }

        /// <summary>
        /// Store the folder used for TutorialGraphAsset assets
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Clear the configured TutorialGraphAsset folder
        /// </summary>
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

        #region Autosave

        /// <summary>
        /// Enable or disable automatic graph saving
        /// </summary>
        /// <param name="isEnabled"></param>
        public void SetAutosaveEnabled(bool isEnabled)
        {
            if (autosaveEnabled == isEnabled)
            {
                return;
            }

            autosaveEnabled = isEnabled;

            SaveSettings();
        }

        /// <summary>
        /// Set the delay applied before an automatic graph save
        /// </summary>
        /// <param name="delay"></param>
        public void SetAutosaveDelay(float delay)
        {
            float validatedDelay = Mathf.Clamp(delay, MinimumAutosaveDelay, MaximumAutosaveDelay);

            if (Mathf.Approximately(autosaveDelay, validatedDelay))
            {
                return;
            }

            autosaveDelay = validatedDelay;

            SaveSettings();
        }

        #endregion

        #region Reset

        /// <summary>
        /// Restore the tutorial tool settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            sequenceFolderGuid = string.Empty;
            graphFolderGuid = string.Empty;
            autosaveEnabled = true;
            autosaveDelay = DefaultAutosaveDelay;

            SaveSettings();
        }

        #endregion

        #region Folder Validation

        /// <summary>
        /// Resolve a stored Unity folder GUID
        /// </summary>
        /// <param name="folderGuid"></param>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        private static bool TryResolveFolderGuid(string folderGuid, out string folderPath)
        {
            folderPath = string.Empty;

            if (string.IsNullOrWhiteSpace(folderGuid))
            {
                return false;
            }

            folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = string.Empty;

                return false;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                folderPath = string.Empty;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate an Assets folder and retrieve its GUID
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="folderGuid"></param>
        /// <returns></returns>
        private static bool TryGetFolderGuid(string folderPath, out string folderGuid)
        {
            folderGuid = string.Empty;

            if (!TryNormalizeAssetFolderPath(folderPath, out string normalizedPath))
            {
                return false;
            }

            if (!AssetDatabase.IsValidFolder(normalizedPath))
            {
                return false;
            }

            folderGuid = AssetDatabase.AssetPathToGUID(normalizedPath);

            return !string.IsNullOrWhiteSpace(folderGuid);
        }

        /// <summary>
        /// Normalize a Unity Assets folder path
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="normalizedPath"></param>
        /// <returns></returns>
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

        #region Persistence

        /// <summary>
        /// Save the settings inside the ProjectSettings folder
        /// </summary>
        private void SaveSettings()
        {
            Save(true);
        }

        #endregion
    }
}