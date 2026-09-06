using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Tutorial.Runtime.Persistence;


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
        /// Create a new empty TutorialGraphAsset at the supplied project path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="createdGraph"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryCreateGraph(string assetPath, out TutorialGraphAsset createdGraph, out string failureReason)
        {
            createdGraph = null;
            failureReason = string.Empty;

            if (!TryValidateGraphAssetPath(assetPath, out failureReason))
            {
                return false;
            }

            TutorialGraphAsset graph = ScriptableObject.CreateInstance<TutorialGraphAsset>();

            if (graph == null)
            {
                failureReason = "Unable to instantiate the TutorialGraphAsset.";
                return false;
            }

            graph.name = Path.GetFileNameWithoutExtension(assetPath);

            try
            {
                AssetDatabase.CreateAsset(graph, assetPath);
                Undo.RegisterCreatedObjectUndo(graph, "Create tutorial graph");

                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssetIfDirty(graph);
                AssetDatabase.ImportAsset(assetPath);

                createdGraph = graph;

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (AssetDatabase.LoadAssetAtPath<TutorialGraphAsset>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(graph);
                }

                failureReason = $"Unable to create the tutorial graph: {exception.Message}";

                return false;
            }
        }

        /// <summary>
        /// Validate a TutorialGraphAsset creation path
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private static bool TryValidateGraphAssetPath(string assetPath, out string failureReason)
        {
            failureReason = string.Empty;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                failureReason = "The tutorial graph path is empty.";
                return false;
            }

            assetPath = assetPath.Replace('\\', '/');

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) && assetPath != "Assets")
            {
                failureReason = "The tutorial graph must be created inside the Assets folder.";
                return false;
            }

            if (!string.Equals(Path.GetExtension(assetPath), ".asset", StringComparison.OrdinalIgnoreCase))
            {
                failureReason = "The tutorial graph must use the .asset extension.";
                return false;
            }

            string folderPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                failureReason = "The tutorial graph folder does not exist.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                failureReason = "An asset already exists at the selected path.";
                return false;
            }

            return true;
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

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            graph.EnsureInitialized();

            Debug.Log($"[TUTO SAVE] EnsureInitialized: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

            stopwatch.Restart();

            EditorUtility.SetDirty(graph);

            Debug.Log($"[TUTO SAVE] SetDirty: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

            stopwatch.Restart();

            AssetDatabase.SaveAssetIfDirty(graph);

            Debug.Log($"[TUTO SAVE] SaveAssetIfDirty: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

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
        /// Find every TutorialGraphAsset currently stored in the project
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<TutorialGraphAsset> FindAllGraphs()
        {
            string[] graphGuids = AssetDatabase.FindAssets("t:TutorialGraphAsset");
            List<TutorialGraphAsset> graphs = new List<TutorialGraphAsset>(graphGuids.Length);

            foreach (string graphGuid in graphGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(graphGuid);
                TutorialGraphAsset graph = AssetDatabase.LoadAssetAtPath<TutorialGraphAsset>(assetPath);

                if (graph != null)
                {
                    graphs.Add(graph);
                }
            }

            graphs.Sort(CompareGraphsByName);

            return graphs;
        }

        /// <summary>
        /// Compare two tutorial graphs by asset name
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static int CompareGraphsByName(TutorialGraphAsset left, TutorialGraphAsset right)
        {
            string leftName = left != null ? left.name : string.Empty;
            string rightName = right != null ? right.name : string.Empty;

            return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
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