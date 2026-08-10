using System;
using System.IO;
using UnityEditor;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Validate tutorial asset paths and create missing Unity folders
    /// </summary>
    public sealed class TutorialAssetPathService
    {
        #region Public Methods

        /// <summary>
        /// Ensure that the supplied Assets folder exists, creating every missing directory
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="normalizedPath"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryEnsureFolderExists(string folderPath, out string normalizedPath, out string failureReason)
        {
            normalizedPath = string.Empty;
            failureReason = string.Empty;

            if (!TryNormalizeAssetFolderPath(folderPath, out normalizedPath))
            {
                failureReason = $"The folder path '{folderPath}' is invalid. Tutorial asset folders must be located inside Assets.";

                return false;
            }

            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                return true;
            }

            string[] pathParts = normalizedPath.Split('/');

            if (pathParts.Length == 0 || !string.Equals(pathParts[0], "Assets", StringComparison.Ordinal))
            {
                failureReason = $"The folder path '{normalizedPath}' is not located inside Assets.";

                return false;
            }

            string currentPath = "Assets";

            for (int i = 1; i < pathParts.Length; i++)
            {
                string folderName = pathParts[i];

                if (!IsValidFolderName(folderName))
                {
                    failureReason = $"The folder name '{folderName}' is invalid inside path '{normalizedPath}'.";

                    return false;
                }

                string nextPath = $"{currentPath}/{folderName}";

                if (AssetDatabase.IsValidFolder(nextPath))
                {
                    currentPath = nextPath;

                    continue;
                }

                string createdFolderGuid = AssetDatabase.CreateFolder(currentPath, folderName);

                if (string.IsNullOrWhiteSpace(createdFolderGuid) || !AssetDatabase.IsValidFolder(nextPath))
                {
                    failureReason = $"Unable to create tutorial folder '{nextPath}'.";

                    return false;
                }

                currentPath = nextPath;
            }

            return AssetDatabase.IsValidFolder(normalizedPath);
        }

        /// <summary>
        /// Normalize and validate a Unity Assets folder path
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="normalizedPath"></param>
        /// <returns></returns>
        public bool TryNormalizeAssetFolderPath(string folderPath, out string normalizedPath)
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

            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                normalizedPath = string.Empty;

                return false;
            }

            string[] pathParts = normalizedPath.Split('/');

            for (int i = 1; i < pathParts.Length; i++)
            {
                if (!IsValidFolderName(pathParts[i]))
                {
                    normalizedPath = string.Empty;

                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check whether a folder name can safely be used inside an Unity Assets path
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private static bool IsValidFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || string.Equals(folderName, ".", StringComparison.Ordinal) || string.Equals(folderName, "..", StringComparison.Ordinal))
            {
                return false;
            }

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                if (folderName.IndexOf(invalidCharacter) >= 0)
                {
                    return false;
                }
            }

            return folderName.IndexOf('/') < 0 && folderName.IndexOf('\\') < 0;
        }

        #endregion
    }
}