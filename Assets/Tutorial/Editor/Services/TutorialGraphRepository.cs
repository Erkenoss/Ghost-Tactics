using System;
using System.Collections.Generic;
using System.IO;
using Tutorial.Editor.Persistence;
using UnityEditor;
using UnityEngine;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Manage TutorialGraphAsset files inside the Unity Project
    /// </summary>
    public sealed class TutorialGraphRepository
    {
        #region Constants

        private const string AssetRootPath = "Assets";
        private const string AssetExtension = ".asset";
        private const string GraphSearchFilter = "t:TutorialGraphAsset";

        #endregion

        #region Graph Creation

        /// <summary>
        /// Create a new TutorialGraphAsset at the requested Project path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        public bool TryCreateGraph(string assetPath, out TutorialGraphAsset graph)
        {
            graph = null;

            if (!TryPrepareGraphAssetPath(assetPath, out string preparedAssetPath))
            {
                return false;
            }

            string folderPath = GetFolderPath(preparedAssetPath);

            if (!TryEnsureFolderExists(folderPath))
            {
                Debug.LogError($"Unable to create or access the tutorial graph folder '{folderPath}'.");

                return false;
            }

            preparedAssetPath = AssetDatabase.GenerateUniqueAssetPath(preparedAssetPath);

            TutorialGraphAsset createdGraph = ScriptableObject.CreateInstance<TutorialGraphAsset>();

            if (createdGraph == null)
            {
                Debug.LogError("Unable to instantiate a TutorialGraphAsset.");

                return false;
            }

            createdGraph.name = Path.GetFileNameWithoutExtension(preparedAssetPath);
            createdGraph.InitializeNewGraph();

            try
            {
                AssetDatabase.CreateAsset(createdGraph, preparedAssetPath);
                Undo.RegisterCreatedObjectUndo(createdGraph, "Create tutorial graph");

                EditorUtility.SetDirty(createdGraph);
                AssetDatabase.SaveAssetIfDirty(createdGraph);

                graph = createdGraph;

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CleanupFailedAssetCreation(createdGraph, preparedAssetPath);

                return false;
            }
        }

        #endregion

        #region Graph Save

        /// <summary>
        /// Save an existing TutorialGraphAsset
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public bool TrySaveGraph(TutorialGraphAsset graph)
        {
            if (graph == null)
            {
                return false;
            }

            if (!AssetDatabase.Contains(graph))
            {
                Debug.LogError($"The tutorial graph '{graph.name}' is not stored inside the Unity Project.", graph);

                return false;
            }

            graph.EnsureInitialized();

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);

            return true;
        }

        /// <summary>
        /// Duplicate a TutorialGraphAsset into a new independent asset
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="assetPath"></param>
        /// <param name="duplicatedGraph"></param>
        /// <returns></returns>
        public bool TrySaveGraphAs(TutorialGraphAsset sourceGraph, string assetPath, out TutorialGraphAsset duplicatedGraph)
        {
            duplicatedGraph = null;

            if (sourceGraph == null)
            {
                return false;
            }

            if (!TryPrepareGraphAssetPath(assetPath, out string preparedAssetPath))
            {
                return false;
            }

            string folderPath = GetFolderPath(preparedAssetPath);

            if (!TryEnsureFolderExists(folderPath))
            {
                Debug.LogError($"Unable to create or access the tutorial graph folder '{folderPath}'.");

                return false;
            }

            preparedAssetPath = AssetDatabase.GenerateUniqueAssetPath(preparedAssetPath);

            TutorialGraphAsset createdGraph = ScriptableObject.CreateInstance<TutorialGraphAsset>();

            if (createdGraph == null)
            {
                Debug.LogError("Unable to instantiate the duplicated TutorialGraphAsset.");

                return false;
            }

            try
            {
                string serializedGraph = EditorJsonUtility.ToJson(sourceGraph);

                EditorJsonUtility.FromJsonOverwrite(serializedGraph, createdGraph);

                createdGraph.name = Path.GetFileNameWithoutExtension(preparedAssetPath);
                createdGraph.EnsureInitialized();
                createdGraph.RegenerateGraphGuid();

                AssetDatabase.CreateAsset(createdGraph, preparedAssetPath);
                Undo.RegisterCreatedObjectUndo(createdGraph, "Duplicate tutorial graph");

                EditorUtility.SetDirty(createdGraph);
                AssetDatabase.SaveAssetIfDirty(createdGraph);

                duplicatedGraph = createdGraph;

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CleanupFailedAssetCreation(createdGraph, preparedAssetPath);

                return false;
            }
        }

        #endregion

        #region Graph Loading

        /// <summary>
        /// Load a TutorialGraphAsset from a Project asset path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        public bool TryLoadGraphAtPath(string assetPath, out TutorialGraphAsset graph)
        {
            graph = null;

            if (!TryPrepareGraphAssetPath(assetPath, out string preparedAssetPath))
            {
                return false;
            }

            graph = AssetDatabase.LoadAssetAtPath<TutorialGraphAsset>(preparedAssetPath);

            if (graph == null)
            {
                return false;
            }

            graph.EnsureInitialized();

            return true;
        }

        /// <summary>
        /// Find every TutorialGraphAsset contained inside the Unity Project
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<TutorialGraphAsset> FindAllGraphs()
        {
            List<TutorialGraphAsset> graphs = new List<TutorialGraphAsset>();
            string[] assetGuids = AssetDatabase.FindAssets(GraphSearchFilter);

            foreach (string assetGuid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                TutorialGraphAsset graph = AssetDatabase.LoadAssetAtPath<TutorialGraphAsset>(assetPath);

                if (graph == null)
                {
                    continue;
                }

                graph.EnsureInitialized();
                graphs.Add(graph);
            }

            graphs.Sort(CompareGraphs);

            return graphs;
        }

        #endregion

        #region Asset Location

        /// <summary>
        /// Get the Unity Project path of a TutorialGraphAsset
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public string GetGraphAssetPath(TutorialGraphAsset graph)
        {
            if (graph == null)
            {
                return string.Empty;
            }

            return NormalizeAssetPath(AssetDatabase.GetAssetPath(graph));
        }

        /// <summary>
        /// Select and highlight a TutorialGraphAsset inside the Project window
        /// </summary>
        /// <param name="graph"></param>
        public void LocateGraph(TutorialGraphAsset graph)
        {
            if (graph == null)
            {
                return;
            }

            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);
        }

        #endregion

        #region Path Validation

        /// <summary>
        /// Validate and normalize a requested TutorialGraphAsset path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="preparedAssetPath"></param>
        /// <returns></returns>
        private static bool TryPrepareGraphAssetPath(string assetPath, out string preparedAssetPath)
        {
            preparedAssetPath = NormalizeAssetPath(assetPath);

            if (string.IsNullOrWhiteSpace(preparedAssetPath))
            {
                return false;
            }

            if (!IsPathInsideAssets(preparedAssetPath))
            {
                Debug.LogError($"Tutorial graph assets must be created inside the Assets folder: '{preparedAssetPath}'.");

                preparedAssetPath = string.Empty;

                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(preparedAssetPath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                preparedAssetPath = string.Empty;

                return false;
            }

            if (!string.Equals(Path.GetExtension(preparedAssetPath), AssetExtension, StringComparison.OrdinalIgnoreCase))
            {
                preparedAssetPath = Path.ChangeExtension(preparedAssetPath, AssetExtension);
                preparedAssetPath = NormalizeAssetPath(preparedAssetPath);
            }

            string folderPath = GetFolderPath(preparedAssetPath);

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                preparedAssetPath = string.Empty;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Check whether an asset path belongs to the Assets directory
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private static bool IsPathInsideAssets(string assetPath)
        {
            if (string.Equals(assetPath, AssetRootPath, StringComparison.Ordinal))
            {
                return true;
            }

            return assetPath.StartsWith($"{AssetRootPath}/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Normalize an Unity Project asset path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private static string NormalizeAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return string.Empty;
            }

            return assetPath.Replace('\\', '/').Trim();
        }

        /// <summary>
        /// Get the folder containing an asset path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private static string GetFolderPath(string assetPath)
        {
            string folderPath = Path.GetDirectoryName(assetPath);

            return NormalizeAssetPath(folderPath);
        }

        #endregion

        #region Folder Creation

        /// <summary>
        /// Ensure that an Assets folder and its parents exist
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        private static bool TryEnsureFolderExists(string folderPath)
        {
            folderPath = NormalizeAssetPath(folderPath);

            if (string.IsNullOrWhiteSpace(folderPath) || !IsPathInsideAssets(folderPath))
            {
                return false;
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            string[] folderParts = folderPath.Split('/');
            string currentPath = AssetRootPath;

            for (int i = 1; i < folderParts.Length; i++)
            {
                string folderName = folderParts[i];

                if (string.IsNullOrWhiteSpace(folderName))
                {
                    continue;
                }

                string nextPath = $"{currentPath}/{folderName}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string folderGuid = AssetDatabase.CreateFolder(currentPath, folderName);

                    if (string.IsNullOrWhiteSpace(folderGuid))
                    {
                        return false;
                    }
                }

                currentPath = nextPath;
            }

            return AssetDatabase.IsValidFolder(folderPath);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Remove an asset or instance created during a failed operation
        /// </summary>
        /// <param name="createdGraph"></param>
        /// <param name="assetPath"></param>
        private static void CleanupFailedAssetCreation(TutorialGraphAsset createdGraph, string assetPath)
        {
            TutorialGraphAsset existingAsset = AssetDatabase.LoadAssetAtPath<TutorialGraphAsset>(assetPath);

            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(assetPath);

                return;
            }

            if (createdGraph != null)
            {
                UnityEngine.Object.DestroyImmediate(createdGraph);
            }
        }

        #endregion

        #region Comparison

        /// <summary>
        /// Sort tutorial graphs by name and then by asset path
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static int CompareGraphs(TutorialGraphAsset first, TutorialGraphAsset second)
        {
            if (first == second)
            {
                return 0;
            }

            if (first == null)
            {
                return 1;
            }

            if (second == null)
            {
                return -1;
            }

            int nameComparison = string.Compare(first.name, second.name, StringComparison.OrdinalIgnoreCase);

            if (nameComparison != 0)
            {
                return nameComparison;
            }

            string firstPath = AssetDatabase.GetAssetPath(first);
            string secondPath = AssetDatabase.GetAssetPath(second);

            return string.Compare(firstPath, secondPath, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}