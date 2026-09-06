using System;
using System.Collections.Generic;
using Tutorial.Runtime.Data;
using UnityEngine;

namespace Tutorial.Runtime.Persistence
{
    /// <summary>
    /// Persistent runtime reference between one visual graph node and its StepSO asset
    /// </summary>
    [Serializable]
    public sealed class TutorialGraphRuntimeNodeReference
    {
        #region Private Fields

        /// <summary>
        /// Unique identifier of the visual graph node using this StepSO
        /// </summary>
        [Tooltip("Unique identifier of the visual graph node using this StepSO")]
        [SerializeField]
        private string nodeGuid = string.Empty;

        /// <summary>
        /// StepSO asset referenced by this runtime graph node
        /// </summary>
        [Tooltip("StepSO asset referenced by this runtime graph node")]
        [SerializeField]
        private StepSO step = null;

        #endregion

        #region Properties

        public string NodeGuid => nodeGuid;
        public StepSO Step => step;

        #endregion

        #region Constructors

        /// <summary>
        /// Create one runtime node reference
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="step"></param>
        public TutorialGraphRuntimeNodeReference(string nodeGuid, StepSO step)
        {
            this.nodeGuid = nodeGuid;
            this.step = step;
        }

        #endregion
    }

    /// <summary>
    /// Persistent asset containing the complete saved state of a tutorial graph
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialGraph", menuName = "Tutorial/Graph")]
    public sealed class TutorialGraphAsset : ScriptableObject
    {
        #region Private Fields

        /// <summary>
        /// Unique identifier of this tutorial graph asset
        /// </summary>
        [Tooltip("Unique identifier of this tutorial graph")]
        [SerializeField]
        private string graphGuid = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Persistent data contained inside the tutorial graph
        /// </summary>
        [Tooltip("Persistent topology and editor data contained inside this tutorial graph")]
        [SerializeField]
        private TutorialGraphSaveData saveData = new TutorialGraphSaveData();

        /// <summary>
        /// Use to know if we can replay the tutorial or not
        /// </summary>
        [Tooltip("Determine whether this tutorial graph can be replayed after completion")]
        [SerializeField]
        private ETutorialReplayPolicy replayPolicy = ETutorialReplayPolicy.Disabled;

        /// <summary>
        /// Runtime StepSO references associated with persistent visual graph nodes
        /// </summary>
        [Tooltip("Runtime StepSO references automatically generated from the tutorial graph")]
        [SerializeField]
        private List<TutorialGraphRuntimeNodeReference> runtimeNodeReferences = new List<TutorialGraphRuntimeNodeReference>();

        #endregion

        #region Properties

        public string GraphGuid => graphGuid;
        public TutorialGraphSaveData SaveData => saveData;
        public int Version => saveData != null ? saveData.Version : 0;
        public bool IsInitialized => !string.IsNullOrWhiteSpace(graphGuid) && saveData != null;
        public ETutorialReplayPolicy ReplayPolicy => replayPolicy;
        public IReadOnlyList<TutorialGraphRuntimeNodeReference> RuntimeNodeReferences => runtimeNodeReferences;

        #endregion

        #region ScriptableObject Callbacks

        /// <summary>
        /// Ensure that the graph contains every required persistent value
        /// </summary>
        private void OnEnable()
        {
            EnsureInitialized();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate the serialized graph data inside the Unity Editor
        /// </summary>
        private void OnValidate()
        {
            EnsureInitialized();
        }
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize this asset as a completely new tutorial graph
        /// </summary>
        public void InitializeNewGraph()
        {
            graphGuid = GenerateGraphGuid();
            saveData = new TutorialGraphSaveData();
            runtimeNodeReferences = new List<TutorialGraphRuntimeNodeReference>();

            saveData.EnsureInitialized();
        }

        /// <summary>
        /// Ensure that the graph identity and its data containers are available
        /// </summary>
        public void EnsureInitialized()
        {
            if (string.IsNullOrWhiteSpace(graphGuid))
            {
                graphGuid = GenerateGraphGuid();
            }

            if (saveData == null)
            {
                saveData = new TutorialGraphSaveData();
            }

            if (runtimeNodeReferences == null)
            {
                runtimeNodeReferences = new List<TutorialGraphRuntimeNodeReference>();
            }

            saveData.EnsureInitialized();
        }

        /// <summary>
        /// Replace every runtime StepSO reference stored inside this graph
        /// </summary>
        /// <param name="sourceNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TrySetRuntimeNodeReferences(IReadOnlyDictionary<string, StepSO> sourceNodes, out string error)
        {
            error = string.Empty;

            if (!TryValidateSourceNodes(sourceNodes, out error))
            {
                return false;
            }

            List<string> nodeGuids = new List<string>(sourceNodes.Keys);
            nodeGuids.Sort(StringComparer.Ordinal);

            runtimeNodeReferences.Clear();

            foreach (string nodeGuid in nodeGuids)
            {
                runtimeNodeReferences.Add(new TutorialGraphRuntimeNodeReference(nodeGuid, sourceNodes[nodeGuid]));
            }

            return true;
        }

        /// <summary>
        /// Resolve every serialized runtime StepSO reference by its visual graph node identifier
        /// </summary>
        /// <param name="sourceNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryResolveSourceNodes(out Dictionary<string, StepSO> sourceNodes, out string error)
        {
            sourceNodes = new Dictionary<string, StepSO>(StringComparer.Ordinal);
            error = string.Empty;

            if (runtimeNodeReferences == null)
            {
                error = $"Tutorial graph '{name}' contains no runtime node reference container.";

                return false;
            }

            foreach (TutorialGraphRuntimeNodeReference runtimeReference in runtimeNodeReferences)
            {
                if (runtimeReference == null)
                {
                    error = $"Tutorial graph '{name}' contains a null runtime node reference.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(runtimeReference.NodeGuid))
                {
                    error = $"Tutorial graph '{name}' contains a runtime Step reference with no NodeGuid.";

                    return false;
                }

                if (runtimeReference.Step == null)
                {
                    error = $"Tutorial graph '{name}' runtime node '{runtimeReference.NodeGuid}' contains no StepSO reference.";

                    return false;
                }

                if (!sourceNodes.TryAdd(runtimeReference.NodeGuid, runtimeReference.Step))
                {
                    error = $"Tutorial graph '{name}' contains duplicate runtime NodeGuid '{runtimeReference.NodeGuid}'.";

                    return false;
                }
            }

            return TryValidateSourceNodes(sourceNodes, out error);
        }

        /// <summary>
        /// Remove every saved graph element while preserving the graph identity
        /// </summary>
        public void ClearGraph()
        {
            EnsureInitialized();

            saveData.Clear();
            runtimeNodeReferences.Clear();
        }

        /// <summary>
        /// Remove every runtime StepSO reference stored inside this graph
        /// </summary>
        public void ClearRuntimeNodeReferences()
        {
            if (runtimeNodeReferences == null)
            {
                runtimeNodeReferences = new List<TutorialGraphRuntimeNodeReference>();

                return;
            }

            runtimeNodeReferences.Clear();
        }

        /// <summary>
        /// Generate a new identity for this graph
        /// </summary>
        public void RegenerateGraphGuid()
        {
            graphGuid = GenerateGraphGuid();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate that runtime StepSO references match every persistent Step node contained inside this graph
        /// </summary>
        /// <param name="sourceNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryValidateSourceNodes(IReadOnlyDictionary<string, StepSO> sourceNodes, out string error)
        {
            error = string.Empty;

            if (sourceNodes == null)
            {
                error = $"Tutorial graph '{name}' received no runtime Step node references.";

                return false;
            }

            if (saveData == null || saveData.Nodes == null)
            {
                error = $"Tutorial graph '{name}' contains no persistent graph node data.";

                return false;
            }

            HashSet<string> expectedNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (TutorialNodeSaveData nodeData in saveData.Nodes)
            {
                if (nodeData == null || nodeData.NodeType != ETutorialNodeType.Step)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(nodeData.NodeGuid))
                {
                    error = $"Tutorial graph '{name}' contains a persistent Step node with no NodeGuid.";

                    return false;
                }

                expectedNodeGuids.Add(nodeData.NodeGuid);

                if (!sourceNodes.TryGetValue(nodeData.NodeGuid, out StepSO sourceStep) || sourceStep == null)
                {
                    error = $"Tutorial graph '{name}' contains no runtime StepSO reference for node '{nodeData.NodeGuid}'.";

                    return false;
                }
            }

            foreach (KeyValuePair<string, StepSO> sourceNode in sourceNodes)
            {
                if (string.IsNullOrWhiteSpace(sourceNode.Key))
                {
                    error = $"Tutorial graph '{name}' contains a runtime StepSO reference with no NodeGuid.";

                    return false;
                }

                if (sourceNode.Value == null)
                {
                    error = $"Tutorial graph '{name}' runtime node '{sourceNode.Key}' contains a null StepSO reference.";

                    return false;
                }

                if (!expectedNodeGuids.Contains(sourceNode.Key))
                {
                    error = $"Tutorial graph '{name}' runtime node '{sourceNode.Key}' does not match any persistent Step node.";

                    return false;
                }
            }

            if (sourceNodes.Count != expectedNodeGuids.Count)
            {
                error = $"Tutorial graph '{name}' runtime StepSO reference count does not match its persistent Step node count.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Generate a unique tutorial graph identifier
        /// </summary>
        /// <returns></returns>
        private static string GenerateGraphGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        #endregion
    }
}