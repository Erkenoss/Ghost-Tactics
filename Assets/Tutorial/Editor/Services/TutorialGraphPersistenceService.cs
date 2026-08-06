using System;
using System.Collections.Generic;
using Tutorial.Editor.Core;
using Tutorial.Runtime.Persistence;
using Tutorial.Runtime.Component;
using Tutorial.Runtime.Data;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Editor.Services
{
    #region Load Plan

    /// <summary>
    /// Resolved representation of a graph node ready to be recreated
    /// </summary>
    public sealed class TutorialResolvedNodeData
    {
        #region Properties

        /// <summary>
        /// Persistent identifier of the graph node
        /// </summary>
        public string NodeGuid { get; }

        /// <summary>
        /// Type of object represented by the node
        /// </summary>
        public ETutorialNodeType NodeType { get; }

        /// <summary>
        /// Resolved Unity object represented by the node
        /// </summary>
        public UnityObject Target { get; }

        /// <summary>
        /// Saved position of the node inside the canvas
        /// </summary>
        public Vector2 Position { get; }

        #endregion

        #region Constructor

        public TutorialResolvedNodeData(string nodeGuid, ETutorialNodeType nodeType, UnityObject target, Vector2 position)
        {
            NodeGuid = nodeGuid;
            NodeType = nodeType;
            Target = target;
            Position = position;
        }

        #endregion
    }

    /// <summary>
    /// Resolved representation of a binding ready to be recreated
    /// </summary>
    public sealed class TutorialResolvedBindingData
    {
        #region Properties

        /// <summary>
        /// Persistent identifier of the source StepSO node
        /// </summary>
        public string SourceNodeGuid { get; }

        /// <summary>
        /// Persistent identifier of the target GameObject node
        /// </summary>
        public string TargetNodeGuid { get; }

        #endregion

        #region Constructor

        public TutorialResolvedBindingData(string sourceNodeGuid, string targetNodeGuid)
        {
            SourceNodeGuid = sourceNodeGuid;
            TargetNodeGuid = targetNodeGuid;
        }

        #endregion
    }

    /// <summary>
    /// Resolved representation of a sequence connection ready to be recreated
    /// </summary>
    public sealed class TutorialResolvedSequenceData
    {
        #region Properties

        /// <summary>
        /// Persistent identifier of the source StepSO node
        /// </summary>
        public string SourceNodeGuid { get; }

        /// <summary>
        /// Persistent identifier of the target StepSO node
        /// </summary>
        public string TargetNodeGuid { get; }

        /// <summary>
        /// Existing StepSequenceSO containing the connection
        /// </summary>
        public StepSequenceSO Sequence { get; }

        #endregion

        #region Constructor

        public TutorialResolvedSequenceData(string sourceNodeGuid, string targetNodeGuid, StepSequenceSO sequence)
        {
            SourceNodeGuid = sourceNodeGuid;
            TargetNodeGuid = targetNodeGuid;
            Sequence = sequence;
        }

        #endregion
    }

    /// <summary>
    /// Resolved tutorial graph data ready for visual reconstruction
    /// </summary>
    public sealed class TutorialGraphLoadPlan
    {
        #region Properties

        /// <summary>
        /// Tutorial graph asset represented by this plan
        /// </summary>
        public TutorialGraphAsset Graph { get; }

        /// <summary>
        /// Resolved nodes ready to be recreated
        /// </summary>
        public IReadOnlyList<TutorialResolvedNodeData> Nodes { get; }

        /// <summary>
        /// Resolved bindings ready to be recreated
        /// </summary>
        public IReadOnlyList<TutorialResolvedBindingData> Bindings { get; }

        /// <summary>
        /// Resolved sequence connections ready to be recreated
        /// </summary>
        public IReadOnlyList<TutorialResolvedSequenceData> Sequences { get; }

        /// <summary>
        /// Saved visual state of the graph canvas
        /// </summary>
        public TutorialGraphViewSaveData View { get; }

        /// <summary>
        /// Non-fatal problems encountered while resolving the graph
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        #endregion

        #region Constructor

        public TutorialGraphLoadPlan(TutorialGraphAsset graph, IReadOnlyList<TutorialResolvedNodeData> nodes, IReadOnlyList<TutorialResolvedBindingData> bindings, IReadOnlyList<TutorialResolvedSequenceData> sequences, TutorialGraphViewSaveData view, IReadOnlyList<string> warnings)
        {
            Graph = graph;
            Nodes = nodes;
            Bindings = bindings;
            Sequences = sequences;
            View = view;
            Warnings = warnings;
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Capture, save and resolve the persistent state of tutorial graphs
    /// </summary>
    public sealed class TutorialGraphPersistenceService
    {
        #region Constants

        private const float MinimumZoom = 0.01f;

        #endregion

        #region Private Fields

        /// <summary>
        /// Repository responsible for TutorialGraphAsset files
        /// </summary>
        private readonly TutorialGraphRepository graphRepository = null;

        /// <summary>
        /// Service responsible for resolving saved Unity references
        /// </summary>
        private readonly TutorialGraphReferenceResolver referenceResolver = null;

        /// <summary>
        /// Registry containing the nodes currently displayed
        /// </summary>
        private readonly TutorialGraphRuntimeRegistry runtimeRegistry = null;

        /// <summary>
        /// Temporary state containing the current graph connections
        /// </summary>
        private readonly TutorialGraphState graphState = null;

        /// <summary>
        /// Working state of the current editor window
        /// </summary>
        private readonly TutorialGraphSession session = null;

        #endregion

        #region Constructor

        public TutorialGraphPersistenceService(TutorialGraphRepository graphRepository, TutorialGraphReferenceResolver referenceResolver, TutorialGraphRuntimeRegistry runtimeRegistry, TutorialGraphState graphState, TutorialGraphSession session)
        {
            this.graphRepository = graphRepository ?? throw new ArgumentNullException(nameof(graphRepository));
            this.referenceResolver = referenceResolver ?? throw new ArgumentNullException(nameof(referenceResolver));
            this.runtimeRegistry = runtimeRegistry ?? throw new ArgumentNullException(nameof(runtimeRegistry));
            this.graphState = graphState ?? throw new ArgumentNullException(nameof(graphState));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        #endregion

        #region Save

        /// <summary>
        /// Save the active tutorial graph using the default canvas view
        /// </summary>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TrySaveActiveGraph(out string failureReason)
        {
            return TrySaveActiveGraph(Vector2.zero, 1f, out failureReason);
        }

        /// <summary>
        /// Save the active tutorial graph and its current canvas view
        /// </summary>
        /// <param name="panPosition"></param>
        /// <param name="zoom"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TrySaveActiveGraph(Vector2 panPosition, float zoom, out string failureReason)
        {
            failureReason = string.Empty;

            if (!session.HasActiveGraph)
            {
                failureReason = "No active TutorialGraphAsset is assigned to the current session.";

                return false;
            }

            return TrySaveGraph(session.ActiveGraph, panPosition, zoom, out failureReason);
        }

        /// <summary>
        /// Capture and save a TutorialGraphAsset
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="panPosition"></param>
        /// <param name="zoom"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TrySaveGraph(TutorialGraphAsset graph, Vector2 panPosition, float zoom, out string failureReason)
        {
            failureReason = string.Empty;

            if (graph == null)
            {
                failureReason = "The TutorialGraphAsset to save is missing.";

                return false;
            }

            if (session.IsLoading)
            {
                failureReason = "The tutorial graph cannot be saved while a loading operation is running.";

                return false;
            }

            if (!TryCaptureNodes(out List<TutorialNodeSaveData> savedNodes, out HashSet<string> registeredNodeGuids, out failureReason))
            {
                return false;
            }

            if (!TryCaptureBindings(registeredNodeGuids, out List<TutorialBindingSaveData> savedBindings, out failureReason))
            {
                return false;
            }

            if (!TryCaptureSequences(registeredNodeGuids, out List<TutorialSequenceSaveData> savedSequences, out failureReason))
            {
                return false;
            }

            TutorialGraphViewSaveData savedView = CreateViewSaveData(panPosition, zoom);

            Undo.RecordObject(graph, "Save tutorial graph");

            graph.EnsureInitialized();

            TutorialGraphSaveData saveData = graph.SaveData;

            saveData.Version = TutorialGraphSaveData.CurrentVersion;
            saveData.Nodes = savedNodes;
            saveData.Bindings = savedBindings;
            saveData.Sequences = savedSequences;
            saveData.View = savedView;

            if (!graphRepository.TrySaveGraph(graph))
            {
                failureReason = $"Unable to save the TutorialGraphAsset '{graph.name}'.";

                return false;
            }

            if (session.ActiveGraph == graph)
            {
                session.MarkSaved();
            }

            return true;
        }

        #endregion

        #region Node Capture

        /// <summary>
        /// Capture every runtime node currently displayed inside the graph
        /// </summary>
        /// <param name="savedNodes"></param>
        /// <param name="registeredNodeGuids"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private bool TryCaptureNodes(out List<TutorialNodeSaveData> savedNodes, out HashSet<string> registeredNodeGuids, out string failureReason)
        {
            savedNodes = new List<TutorialNodeSaveData>();
            registeredNodeGuids = new HashSet<string>(StringComparer.Ordinal);
            failureReason = string.Empty;

            foreach (TutorialRuntimeNode runtimeNode in runtimeRegistry.Nodes)
            {
                if (!TryCreateNodeSaveData(runtimeNode, out TutorialNodeSaveData nodeData, out failureReason))
                {
                    savedNodes.Clear();
                    registeredNodeGuids.Clear();

                    return false;
                }

                if (!registeredNodeGuids.Add(nodeData.NodeGuid))
                {
                    failureReason = $"The NodeGuid '{nodeData.NodeGuid}' is registered more than once.";

                    savedNodes.Clear();
                    registeredNodeGuids.Clear();

                    return false;
                }

                savedNodes.Add(nodeData);
            }

            return true;
        }

        /// <summary>
        /// Create persistent data from a runtime node
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <param name="nodeData"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private static bool TryCreateNodeSaveData(TutorialRuntimeNode runtimeNode, out TutorialNodeSaveData nodeData, out string failureReason)
        {
            nodeData = null;
            failureReason = string.Empty;

            if (runtimeNode == null || !runtimeNode.IsValid)
            {
                failureReason = "The runtime registry contains an invalid tutorial node.";

                return false;
            }

            Vector2 nodePosition = GetNodePosition(runtimeNode);

            if (runtimeNode.Target is StepSO step)
            {
                return TryCreateStepNodeSaveData(runtimeNode.NodeGuid, step, nodePosition, out nodeData, out failureReason);
            }

            if (runtimeNode.Target is GameObject gameObject)
            {
                return TryCreateGameObjectNodeSaveData(runtimeNode.NodeGuid, gameObject, nodePosition, out nodeData, out failureReason);
            }

            failureReason = $"The node '{runtimeNode.NodeGuid}' represents an unsupported object type: {runtimeNode.Target.GetType().FullName}.";

            return false;
        }

        /// <summary>
        /// Create persistent data for a StepSO node
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="step"></param>
        /// <param name="position"></param>
        /// <param name="nodeData"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private static bool TryCreateStepNodeSaveData(string nodeGuid, StepSO step, Vector2 position, out TutorialNodeSaveData nodeData, out string failureReason)
        {
            nodeData = null;
            failureReason = string.Empty;

            string assetPath = AssetDatabase.GetAssetPath(step);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                failureReason = $"The StepSO '{step.name}' is not stored as a Unity asset.";

                return false;
            }

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrWhiteSpace(assetGuid))
            {
                failureReason = $"Unable to retrieve the Unity asset GUID of StepSO '{step.name}'.";

                return false;
            }

            nodeData = new TutorialNodeSaveData
            {
                NodeGuid = nodeGuid,
                NodeType = ETutorialNodeType.Step,
                AssetGuid = assetGuid,
                Position = position
            };

            return true;
        }

        /// <summary>
        /// Create persistent data for a scene GameObject node
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="gameObject"></param>
        /// <param name="position"></param>
        /// <param name="nodeData"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private static bool TryCreateGameObjectNodeSaveData(string nodeGuid, GameObject gameObject, Vector2 position, out TutorialNodeSaveData nodeData, out string failureReason)
        {
            nodeData = null;
            failureReason = string.Empty;

            if (!gameObject.TryGetComponent(out TutoIdentifier identifier))
            {
                failureReason = $"The GameObject '{gameObject.name}' does not contain a TutoIdentifier.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(identifier.ObjectGUID))
            {
                failureReason = $"The GameObject '{gameObject.name}' has no tutorial object GUID.";

                return false;
            }

            if (!gameObject.scene.IsValid() || string.IsNullOrWhiteSpace(gameObject.scene.path))
            {
                failureReason = $"The GameObject '{gameObject.name}' does not belong to a saved Unity scene.";

                return false;
            }

            string scenePath = NormalizeAssetPath(gameObject.scene.path);
            string sceneAssetGuid = AssetDatabase.AssetPathToGUID(scenePath);

            if (string.IsNullOrWhiteSpace(sceneAssetGuid))
            {
                failureReason = $"Unable to retrieve the Unity asset GUID of scene '{scenePath}'.";

                return false;
            }

            nodeData = new TutorialNodeSaveData
            {
                NodeGuid = nodeGuid,
                NodeType = ETutorialNodeType.GameObject,
                ObjectGuid = identifier.ObjectGUID,
                SceneAssetGuid = sceneAssetGuid,
                ScenePath = scenePath,
                Position = position
            };

            return true;
        }

        /// <summary>
        /// Get the current position of a visual graph node
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        private static Vector2 GetNodePosition(TutorialRuntimeNode runtimeNode)
        {
            float positionX = runtimeNode.Element.resolvedStyle.left;
            float positionY = runtimeNode.Element.resolvedStyle.top;

            if (float.IsNaN(positionX) || float.IsInfinity(positionX))
            {
                positionX = runtimeNode.Element.layout.x;
            }

            if (float.IsNaN(positionY) || float.IsInfinity(positionY))
            {
                positionY = runtimeNode.Element.layout.y;
            }

            if (float.IsNaN(positionX) || float.IsInfinity(positionX))
            {
                positionX = 0f;
            }

            if (float.IsNaN(positionY) || float.IsInfinity(positionY))
            {
                positionY = 0f;
            }

            return new Vector2(positionX, positionY);
        }

        #endregion

        #region Binding Capture

        /// <summary>
        /// Capture every current StepSO to GameObject binding
        /// </summary>
        /// <param name="registeredNodeGuids"></param>
        /// <param name="savedBindings"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private bool TryCaptureBindings(HashSet<string> registeredNodeGuids, out List<TutorialBindingSaveData> savedBindings, out string failureReason)
        {
            savedBindings = new List<TutorialBindingSaveData>();
            failureReason = string.Empty;

            HashSet<string> registeredBindings = new HashSet<string>(StringComparer.Ordinal);

            foreach (BindingConnection connection in graphState.BindingConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    failureReason = "The tutorial graph contains an invalid binding connection.";

                    savedBindings.Clear();

                    return false;
                }

                if (!runtimeRegistry.TryGetNodeGuid(connection.SourceNode, out string sourceNodeGuid))
                {
                    failureReason = $"Unable to find the NodeGuid of binding source '{connection.Step.name}'.";

                    savedBindings.Clear();

                    return false;
                }

                if (!runtimeRegistry.TryGetNodeGuid(connection.TargetNode, out string targetNodeGuid))
                {
                    failureReason = $"Unable to find the NodeGuid of binding target '{connection.TargetGameObject.name}'.";

                    savedBindings.Clear();

                    return false;
                }

                if (!registeredNodeGuids.Contains(sourceNodeGuid) || !registeredNodeGuids.Contains(targetNodeGuid))
                {
                    failureReason = "A binding connection references a node absent from the runtime registry.";

                    savedBindings.Clear();

                    return false;
                }

                string bindingKey = $"{sourceNodeGuid}|{targetNodeGuid}";

                if (!registeredBindings.Add(bindingKey))
                {
                    continue;
                }

                savedBindings.Add(new TutorialBindingSaveData
                {
                    SourceNodeGuid = sourceNodeGuid,
                    TargetNodeGuid = targetNodeGuid
                });
            }

            return true;
        }

        #endregion

        #region Sequence Capture

        /// <summary>
        /// Capture every current StepSO sequence connection
        /// </summary>
        /// <param name="registeredNodeGuids"></param>
        /// <param name="savedSequences"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private bool TryCaptureSequences(HashSet<string> registeredNodeGuids, out List<TutorialSequenceSaveData> savedSequences, out string failureReason)
        {
            savedSequences = new List<TutorialSequenceSaveData>();
            failureReason = string.Empty;

            HashSet<string> registeredSequences = new HashSet<string>(StringComparer.Ordinal);

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    failureReason = "The tutorial graph contains an invalid sequence connection.";

                    savedSequences.Clear();

                    return false;
                }

                if (!runtimeRegistry.TryGetNodeGuid(connection.SourceNode, out string sourceNodeGuid))
                {
                    failureReason = $"Unable to find the NodeGuid of sequence source '{connection.SourceStep.name}'.";

                    savedSequences.Clear();

                    return false;
                }

                if (!runtimeRegistry.TryGetNodeGuid(connection.TargetNode, out string targetNodeGuid))
                {
                    failureReason = $"Unable to find the NodeGuid of sequence target '{connection.TargetStep.name}'.";

                    savedSequences.Clear();

                    return false;
                }

                if (!registeredNodeGuids.Contains(sourceNodeGuid) || !registeredNodeGuids.Contains(targetNodeGuid))
                {
                    failureReason = "A sequence connection references a node absent from the runtime registry.";

                    savedSequences.Clear();

                    return false;
                }

                string sequenceAssetPath = AssetDatabase.GetAssetPath(connection.Sequence);
                string sequenceAssetGuid = AssetDatabase.AssetPathToGUID(sequenceAssetPath);

                if (string.IsNullOrWhiteSpace(sequenceAssetGuid))
                {
                    failureReason = $"Unable to retrieve the Unity asset GUID of sequence '{connection.Sequence.name}'.";

                    savedSequences.Clear();

                    return false;
                }

                string sequenceKey = $"{sourceNodeGuid}|{targetNodeGuid}|{sequenceAssetGuid}";

                if (!registeredSequences.Add(sequenceKey))
                {
                    continue;
                }

                savedSequences.Add(new TutorialSequenceSaveData
                {
                    SourceNodeGuid = sourceNodeGuid,
                    TargetNodeGuid = targetNodeGuid,
                    SequenceAssetGuid = sequenceAssetGuid
                });
            }

            return true;
        }

        #endregion

        #region Load Plan

        /// <summary>
        /// Resolve a TutorialGraphAsset into a plan ready for visual reconstruction
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="loadPlan"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryCreateLoadPlan(TutorialGraphAsset graph, out TutorialGraphLoadPlan loadPlan, out string failureReason)
        {
            loadPlan = null;
            failureReason = string.Empty;

            if (graph == null)
            {
                failureReason = "The TutorialGraphAsset to load is missing.";

                return false;
            }

            graph.EnsureInitialized();

            TutorialGraphSaveData saveData = graph.SaveData;

            if (!ValidateSaveVersion(saveData.Version, out failureReason))
            {
                return false;
            }

            List<string> warnings = new List<string>();
            Dictionary<string, TutorialResolvedNodeData> resolvedNodesByGuid = ResolveNodes(saveData.Nodes, warnings);
            List<TutorialResolvedBindingData> resolvedBindings = ResolveBindings(saveData.Bindings, resolvedNodesByGuid, warnings);
            List<TutorialResolvedSequenceData> resolvedSequences = ResolveSequences(saveData.Sequences, resolvedNodesByGuid, warnings);
            TutorialGraphViewSaveData resolvedView = CopyViewData(saveData.View);

            loadPlan = new TutorialGraphLoadPlan(
                graph,
                new List<TutorialResolvedNodeData>(resolvedNodesByGuid.Values),
                resolvedBindings,
                resolvedSequences,
                resolvedView,
                warnings
            );

            return true;
        }

        /// <summary>
        /// Resolve every valid saved node
        /// </summary>
        /// <param name="savedNodes"></param>
        /// <param name="warnings"></param>
        /// <returns></returns>
        private Dictionary<string, TutorialResolvedNodeData> ResolveNodes(IReadOnlyList<TutorialNodeSaveData> savedNodes, List<string> warnings)
        {
            Dictionary<string, TutorialResolvedNodeData> resolvedNodes = new Dictionary<string, TutorialResolvedNodeData>(StringComparer.Ordinal);

            if (savedNodes == null)
            {
                return resolvedNodes;
            }

            foreach (TutorialNodeSaveData nodeData in savedNodes)
            {
                if (nodeData == null)
                {
                    warnings.Add("A null node entry was ignored.");

                    continue;
                }

                string nodeGuid = NormalizeGuid(nodeData.NodeGuid);

                if (string.IsNullOrWhiteSpace(nodeGuid))
                {
                    warnings.Add("A saved node without NodeGuid was ignored.");

                    continue;
                }

                if (resolvedNodes.ContainsKey(nodeGuid))
                {
                    warnings.Add($"The duplicated NodeGuid '{nodeGuid}' was ignored.");

                    continue;
                }

                if (!referenceResolver.TryResolveNodeTarget(nodeData, out UnityObject target, out string failureReason))
                {
                    warnings.Add(failureReason);

                    continue;
                }

                Vector2 position = SanitizePosition(nodeData.Position);
                TutorialResolvedNodeData resolvedNode = new TutorialResolvedNodeData(nodeGuid, nodeData.NodeType, target, position);

                resolvedNodes.Add(nodeGuid, resolvedNode);
            }

            return resolvedNodes;
        }

        /// <summary>
        /// Resolve every valid saved binding
        /// </summary>
        /// <param name="savedBindings"></param>
        /// <param name="resolvedNodes"></param>
        /// <param name="warnings"></param>
        /// <returns></returns>
        private static List<TutorialResolvedBindingData> ResolveBindings(IReadOnlyList<TutorialBindingSaveData> savedBindings, IReadOnlyDictionary<string, TutorialResolvedNodeData> resolvedNodes, List<string> warnings)
        {
            List<TutorialResolvedBindingData> resolvedBindings = new List<TutorialResolvedBindingData>();
            HashSet<string> registeredBindings = new HashSet<string>(StringComparer.Ordinal);

            if (savedBindings == null)
            {
                return resolvedBindings;
            }

            foreach (TutorialBindingSaveData bindingData in savedBindings)
            {
                if (bindingData == null)
                {
                    warnings.Add("A null binding entry was ignored.");

                    continue;
                }

                string sourceNodeGuid = NormalizeGuid(bindingData.SourceNodeGuid);
                string targetNodeGuid = NormalizeGuid(bindingData.TargetNodeGuid);

                if (!resolvedNodes.TryGetValue(sourceNodeGuid, out TutorialResolvedNodeData sourceNode))
                {
                    warnings.Add($"Binding source node '{sourceNodeGuid}' could not be resolved.");

                    continue;
                }

                if (!resolvedNodes.TryGetValue(targetNodeGuid, out TutorialResolvedNodeData targetNode))
                {
                    warnings.Add($"Binding target node '{targetNodeGuid}' could not be resolved.");

                    continue;
                }

                if (sourceNode.NodeType != ETutorialNodeType.Step || targetNode.NodeType != ETutorialNodeType.GameObject)
                {
                    warnings.Add($"Binding '{sourceNodeGuid}' to '{targetNodeGuid}' uses incompatible node types.");

                    continue;
                }

                string bindingKey = $"{sourceNodeGuid}|{targetNodeGuid}";

                if (!registeredBindings.Add(bindingKey))
                {
                    continue;
                }

                resolvedBindings.Add(new TutorialResolvedBindingData(sourceNodeGuid, targetNodeGuid));
            }

            return resolvedBindings;
        }

        /// <summary>
        /// Resolve every valid saved sequence connection
        /// </summary>
        /// <param name="savedSequences"></param>
        /// <param name="resolvedNodes"></param>
        /// <param name="warnings"></param>
        /// <returns></returns>
        private List<TutorialResolvedSequenceData> ResolveSequences(IReadOnlyList<TutorialSequenceSaveData> savedSequences, IReadOnlyDictionary<string, TutorialResolvedNodeData> resolvedNodes, List<string> warnings)
        {
            List<TutorialResolvedSequenceData> resolvedSequences = new List<TutorialResolvedSequenceData>();
            HashSet<string> registeredSequences = new HashSet<string>(StringComparer.Ordinal);

            if (savedSequences == null)
            {
                return resolvedSequences;
            }

            foreach (TutorialSequenceSaveData sequenceData in savedSequences)
            {
                if (sequenceData == null)
                {
                    warnings.Add("A null sequence entry was ignored.");

                    continue;
                }

                string sourceNodeGuid = NormalizeGuid(sequenceData.SourceNodeGuid);
                string targetNodeGuid = NormalizeGuid(sequenceData.TargetNodeGuid);

                if (!resolvedNodes.TryGetValue(sourceNodeGuid, out TutorialResolvedNodeData sourceNode))
                {
                    warnings.Add($"Sequence source node '{sourceNodeGuid}' could not be resolved.");

                    continue;
                }

                if (!resolvedNodes.TryGetValue(targetNodeGuid, out TutorialResolvedNodeData targetNode))
                {
                    warnings.Add($"Sequence target node '{targetNodeGuid}' could not be resolved.");

                    continue;
                }

                if (sourceNode.NodeType != ETutorialNodeType.Step || targetNode.NodeType != ETutorialNodeType.Step)
                {
                    warnings.Add($"Sequence '{sourceNodeGuid}' to '{targetNodeGuid}' uses incompatible node types.");

                    continue;
                }

                if (string.Equals(sourceNodeGuid, targetNodeGuid, StringComparison.Ordinal))
                {
                    warnings.Add($"The sequence connection '{sourceNodeGuid}' targets itself and was ignored.");

                    continue;
                }

                if (!referenceResolver.TryResolveSequence(sequenceData, out StepSequenceSO sequence))
                {
                    warnings.Add($"The StepSequenceSO associated with '{sourceNodeGuid}' to '{targetNodeGuid}' could not be resolved.");

                    continue;
                }

                string sequenceKey = $"{sourceNodeGuid}|{targetNodeGuid}|{sequenceData.SequenceAssetGuid}";

                if (!registeredSequences.Add(sequenceKey))
                {
                    continue;
                }

                resolvedSequences.Add(new TutorialResolvedSequenceData(sourceNodeGuid, targetNodeGuid, sequence));
            }

            return resolvedSequences;
        }

        #endregion

        #region Save Version

        /// <summary>
        /// Validate the version of persistent graph data
        /// </summary>
        /// <param name="version"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private static bool ValidateSaveVersion(int version, out string failureReason)
        {
            failureReason = string.Empty;

            if (version == TutorialGraphSaveData.CurrentVersion)
            {
                return true;
            }

            if (version > TutorialGraphSaveData.CurrentVersion)
            {
                failureReason = $"The graph uses save format version {version}, but this tool only supports version {TutorialGraphSaveData.CurrentVersion}.";

                return false;
            }

            failureReason = $"The graph uses obsolete save format version {version}. No migration is currently available.";

            return false;
        }

        #endregion

        #region View

        /// <summary>
        /// Create persistent canvas view data
        /// </summary>
        /// <param name="panPosition"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private static TutorialGraphViewSaveData CreateViewSaveData(Vector2 panPosition, float zoom)
        {
            return new TutorialGraphViewSaveData
            {
                PanPosition = SanitizePosition(panPosition),
                Zoom = SanitizeZoom(zoom)
            };
        }

        /// <summary>
        /// Copy persistent canvas view data
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static TutorialGraphViewSaveData CopyViewData(TutorialGraphViewSaveData source)
        {
            if (source == null)
            {
                return new TutorialGraphViewSaveData();
            }

            return new TutorialGraphViewSaveData
            {
                PanPosition = SanitizePosition(source.PanPosition),
                Zoom = SanitizeZoom(source.Zoom)
            };
        }

        /// <summary>
        /// Ensure that a saved graph position contains finite values
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private static Vector2 SanitizePosition(Vector2 position)
        {
            if (float.IsNaN(position.x) || float.IsInfinity(position.x))
            {
                position.x = 0f;
            }

            if (float.IsNaN(position.y) || float.IsInfinity(position.y))
            {
                position.y = 0f;
            }

            return position;
        }

        /// <summary>
        /// Ensure that the saved canvas zoom is valid
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        private static float SanitizeZoom(float zoom)
        {
            if (float.IsNaN(zoom) || float.IsInfinity(zoom))
            {
                return 1f;
            }

            return Mathf.Max(MinimumZoom, zoom);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Normalize a persistent identifier
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        private static string NormalizeGuid(string guid)
        {
            return string.IsNullOrWhiteSpace(guid) ? string.Empty : guid.Trim();
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