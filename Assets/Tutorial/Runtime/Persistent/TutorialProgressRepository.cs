using System;
using System.IO;
using UnityEngine;

namespace Tutorial.Runtime.Persistence
{
    public enum ETutorialProgressLoadResult
    {
        NotFound = 0,
        Success = 1,
        InvalidData = 2,
        Failed = 3
    }

    /// <summary>
    /// Read, write and delete persistent tutorial progress files
    /// </summary>
    public sealed class TutorialProgressRepository
    {
        #region Constants

        /// <summary>
        /// Directory containing every tutorial progress file
        /// </summary>
        private const string DirectoryName = "TutorialProgress";

        /// <summary>
        /// File extension used by tutorial progress files
        /// </summary>
        private const string FileExtension = ".json";

        #endregion

        #region Private Fields

        /// <summary>
        /// Absolute directory containing every tutorial progress file
        /// </summary>
        private readonly string directoryPath = string.Empty;

        #endregion

        #region Properties

        public string DirectoryPath => directoryPath;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a tutorial progress repository inside one persistent application directory
        /// </summary>
        /// <param name="persistentDataPath"></param>
        public TutorialProgressRepository(string persistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath))
            {
                throw new ArgumentException("Tutorial progress repository requires a valid persistent data path.", nameof(persistentDataPath));
            }

            directoryPath = Path.Combine(persistentDataPath, DirectoryName);
        }

        #endregion

        #region Save

        /// <summary>
        /// Write one tutorial progress snapshot to persistent storage
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TrySave(TutorialProgressSaveData saveData, out string error)
        {
            error = string.Empty;

            if (saveData == null)
            {
                error = "Tutorial progress save data is null.";

                return false;
            }

            if (!IsValidTutorialGuid(saveData.TutorialGuid))
            {
                error = $"Tutorial progress GUID '{saveData.TutorialGuid}' cannot be used as a persistent file identifier.";

                return false;
            }

            try
            {
                saveData.EnsureInitialized();

                Directory.CreateDirectory(directoryPath);

                string json = JsonUtility.ToJson(saveData, true);

                if (string.IsNullOrWhiteSpace(json))
                {
                    error = $"Tutorial progress '{saveData.TutorialGuid}' could not be serialized.";

                    return false;
                }

                File.WriteAllText(GetFilePath(saveData.TutorialGuid), json);

                return true;
            }
            catch (Exception exception)
            {
                error = $"Tutorial progress '{saveData.TutorialGuid}' could not be saved. {exception.Message}";

                return false;
            }
        }

        #endregion

        #region Load

        /// <summary>
        /// Load one tutorial progress snapshot from persistent storage
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <param name="saveData"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public ETutorialProgressLoadResult TryLoad(string tutorialGuid, out TutorialProgressSaveData saveData, out string error)
        {
            saveData = null;
            error = string.Empty;

            if (!IsValidTutorialGuid(tutorialGuid))
            {
                error = $"Tutorial GUID '{tutorialGuid}' cannot be used as a persistent file identifier.";

                return ETutorialProgressLoadResult.Failed;
            }

            string filePath = GetFilePath(tutorialGuid);

            if (!File.Exists(filePath))
            {
                return ETutorialProgressLoadResult.NotFound;
            }

            string json = string.Empty;

            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception exception)
            {
                error = $"Tutorial progress '{tutorialGuid}' could not be read. {exception.Message}";

                return ETutorialProgressLoadResult.Failed;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                error = $"Tutorial progress file '{tutorialGuid}' is empty.";

                return ETutorialProgressLoadResult.InvalidData;
            }

            try
            {
                saveData = JsonUtility.FromJson<TutorialProgressSaveData>(json);
            }
            catch (Exception exception)
            {
                error = $"Tutorial progress file '{tutorialGuid}' contains invalid JSON. {exception.Message}";

                return ETutorialProgressLoadResult.InvalidData;
            }

            if (saveData == null)
            {
                error = $"Tutorial progress file '{tutorialGuid}' could not be deserialized.";

                return ETutorialProgressLoadResult.InvalidData;
            }

            saveData.EnsureInitialized();

            if (!string.Equals(saveData.TutorialGuid, tutorialGuid, StringComparison.Ordinal))
            {
                error = $"Tutorial progress file '{tutorialGuid}' contains progress for tutorial '{saveData.TutorialGuid}'.";
                saveData = null;

                return ETutorialProgressLoadResult.InvalidData;
            }

            return ETutorialProgressLoadResult.Success;
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete persistent progress associated with one tutorial
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryDelete(string tutorialGuid, out string error)
        {
            error = string.Empty;

            if (!IsValidTutorialGuid(tutorialGuid))
            {
                error = $"Tutorial GUID '{tutorialGuid}' cannot be used as a persistent file identifier.";

                return false;
            }

            string filePath = GetFilePath(tutorialGuid);

            if (!File.Exists(filePath))
            {
                return true;
            }

            try
            {
                File.Delete(filePath);

                return true;
            }
            catch (Exception exception)
            {
                error = $"Tutorial progress '{tutorialGuid}' could not be deleted. {exception.Message}";

                return false;
            }
        }

        /// <summary>
        /// Delete every persistent tutorial progress file
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryDeleteAll(out string error)
        {
            error = string.Empty;

            if (!Directory.Exists(directoryPath))
            {
                return true;
            }

            try
            {
                Directory.Delete(directoryPath, true);

                return true;
            }
            catch (Exception exception)
            {
                error = $"Tutorial progress directory could not be deleted. {exception.Message}";

                return false;
            }
        }

        #endregion

        #region Paths

        /// <summary>
        /// Build the persistent file path associated with one tutorial
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <returns></returns>
        private string GetFilePath(string tutorialGuid)
        {
            return Path.Combine(directoryPath, $"{tutorialGuid}{FileExtension}");
        }

        /// <summary>
        /// Determine whether one tutorial GUID can safely be used as a file identifier
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <returns></returns>
        private static bool IsValidTutorialGuid(string tutorialGuid)
        {
            if (string.IsNullOrWhiteSpace(tutorialGuid))
            {
                return false;
            }

            return tutorialGuid.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        #endregion
    }
}