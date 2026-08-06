using System;
using Tutorial.Editor.Persistence;
using Tutorial.Runtime.Component;
using Tutorial.Runtime.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Resolve the persistent references stored inside a tutorial graph save
    /// </summary>
    public sealed class TutorialGraphReferenceResolver
    {
        #region Node Resolution

        /// <summary>
        /// Resolve the Unity object represented by a saved graph node
        /// </summary>
        /// <param name="nodeData"></param>
        /// <param name="target"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryResolveNodeTarget(TutorialNodeSaveData nodeData, out UnityObject target, out string failureReason)
        {
            target = null;
            failureReason = string.Empty;

            if (nodeData == null)
            {
                failureReason = "The saved node data is missing.";

                return false;
            }

            switch (nodeData.NodeType)
            {
                case ETutorialNodeType.Step:
                    if (!TryResolveStep(nodeData.AssetGuid, out StepSO step))
                    {
                        failureReason = $"Unable to resolve the StepSO asset associated with node '{nodeData.NodeGuid}'.";

                        return false;
                    }

                    target = step;

                    return true;

                case ETutorialNodeType.GameObject:
                    if (!TryResolveGameObject(nodeData.SceneAssetGuid, nodeData.ScenePath, nodeData.ObjectGuid, out GameObject gameObject, out failureReason))
                    {
                        return false;
                    }

                    target = gameObject;

                    return true;

                default:
                    failureReason = $"The saved node '{nodeData.NodeGuid}' has an unsupported node type: {nodeData.NodeType}.";

                    return false;
            }
        }

        #endregion

        #region Step Resolution

        /// <summary>
        /// Resolve a StepSO from its Unity asset GUID
        /// </summary>
        /// <param name="assetGuid"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public bool TryResolveStep(string assetGuid, out StepSO step)
        {
            step = null;

            if (string.IsNullOrWhiteSpace(assetGuid))
            {
                return false;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid.Trim());

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            step = AssetDatabase.LoadAssetAtPath<StepSO>(assetPath);

            return step != null;
        }

        #endregion

        #region Sequence Resolution

        /// <summary>
        /// Resolve a StepSequenceSO from its Unity asset GUID
        /// </summary>
        /// <param name="assetGuid"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public bool TryResolveSequence(string assetGuid, out StepSequenceSO sequence)
        {
            sequence = null;

            if (string.IsNullOrWhiteSpace(assetGuid))
            {
                return false;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid.Trim());

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            sequence = AssetDatabase.LoadAssetAtPath<StepSequenceSO>(assetPath);

            return sequence != null;
        }

        /// <summary>
        /// Resolve the StepSequenceSO referenced by saved sequence data
        /// </summary>
        /// <param name="sequenceData"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public bool TryResolveSequence(TutorialSequenceSaveData sequenceData, out StepSequenceSO sequence)
        {
            sequence = null;

            if (sequenceData == null)
            {
                return false;
            }

            return TryResolveSequence(sequenceData.SequenceAssetGuid, out sequence);
        }

        #endregion

        #region GameObject Resolution

        /// <summary>
        /// Resolve a scene GameObject from its scene and TutoIdentifier GUID
        /// </summary>
        /// <param name="sceneAssetGuid"></param>
        /// <param name="scenePath"></param>
        /// <param name="objectGuid"></param>
        /// <param name="gameObject"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryResolveGameObject(string sceneAssetGuid, string scenePath, string objectGuid, out GameObject gameObject, out string failureReason)
        {
            gameObject = null;
            failureReason = string.Empty;

            if (string.IsNullOrWhiteSpace(objectGuid))
            {
                failureReason = "The saved GameObject reference contains no TutoIdentifier GUID.";

                return false;
            }

            if (!TryResolveScenePath(sceneAssetGuid, scenePath, out string resolvedScenePath))
            {
                failureReason = $"Unable to resolve the scene containing the tutorial object '{objectGuid}'.";

                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(resolvedScenePath);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                failureReason = $"The scene '{resolvedScenePath}' is not currently loaded.";

                return false;
            }

            if (!TryFindIdentifierInsideScene(scene, objectGuid.Trim(), out TutoIdentifier identifier, out bool hasDuplicate))
            {
                failureReason = $"No TutoIdentifier with GUID '{objectGuid}' was found inside scene '{resolvedScenePath}'.";

                return false;
            }

            if (hasDuplicate)
            {
                failureReason = $"Several TutoIdentifier components inside scene '{resolvedScenePath}' share GUID '{objectGuid}'.";

                return false;
            }

            gameObject = identifier.gameObject;

            return gameObject != null;
        }

        /// <summary>
        /// Find a TutoIdentifier inside a loaded scene
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="objectGuid"></param>
        /// <param name="identifier"></param>
        /// <param name="hasDuplicate"></param>
        /// <returns></returns>
        private static bool TryFindIdentifierInsideScene(Scene scene, string objectGuid, out TutoIdentifier identifier, out bool hasDuplicate)
        {
            identifier = null;
            hasDuplicate = false;

            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectGuid))
            {
                return false;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject == null)
                {
                    continue;
                }

                TutoIdentifier[] identifiers = rootObject.GetComponentsInChildren<TutoIdentifier>(true);

                foreach (TutoIdentifier currentIdentifier in identifiers)
                {
                    if (currentIdentifier == null)
                    {
                        continue;
                    }

                    if (!string.Equals(currentIdentifier.ObjectGUID, objectGuid, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (identifier != null)
                    {
                        hasDuplicate = true;

                        return true;
                    }

                    identifier = currentIdentifier;
                }
            }

            return identifier != null;
        }

        #endregion

        #region Scene Resolution

        /// <summary>
        /// Resolve a scene path from its Unity asset GUID or last known asset path
        /// </summary>
        /// <param name="sceneAssetGuid"></param>
        /// <param name="scenePath"></param>
        /// <param name="resolvedScenePath"></param>
        /// <returns></returns>
        public bool TryResolveScenePath(string sceneAssetGuid, string scenePath, out string resolvedScenePath)
        {
            resolvedScenePath = string.Empty;

            if (!string.IsNullOrWhiteSpace(sceneAssetGuid))
            {
                string pathFromGuid = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(sceneAssetGuid.Trim()));

                if (IsValidSceneAsset(pathFromGuid))
                {
                    resolvedScenePath = pathFromGuid;

                    return true;
                }
            }

            string normalizedScenePath = NormalizeAssetPath(scenePath);

            if (!IsValidSceneAsset(normalizedScenePath))
            {
                return false;
            }

            resolvedScenePath = normalizedScenePath;

            return true;
        }

        /// <summary>
        /// Check whether an asset path represents an existing Unity scene
        /// </summary>
        /// <param name="scenePath"></param>
        /// <returns></returns>
        private static bool IsValidSceneAsset(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            return sceneAsset != null;
        }

        /// <summary>
        /// Normalize a Unity asset path
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

        #endregion
    }
}