using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Tutorial.Runtime.Data;
using Tutorial.Editor.Core;
using Tutorial.Editor.Views;
using Tutorial.Editor.Services;

namespace Tutorial.Editor.Controllers
{
    /// <summary>
    /// Manage StepSO sequence connections and their associated StepSequenceSO assets
    /// </summary>
    internal sealed class TutorialSequenceController
    {
        #region Private Fields

        /// <summary>
        /// Temporary state of the tutorial graph
        /// </summary>
        private readonly TutorialGraphState graphState = null;

        /// <summary>
        /// Main tutorial graph canvas
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Renderer responsible for graph connection drawing
        /// </summary>
        private readonly TutorialConnectionRenderer connectionRenderer = null;

        /// <summary>
        /// Service responsible for StepSequenceSO asset creation
        /// </summary>
        private readonly TutorialSequenceAssetService sequenceAssetService = null;

        #endregion

        #region Events

        /// <summary>
        /// Raised after a manual sequence modification
        /// </summary>
        public event Action SequenceChanged = null;

        #endregion

        #region Constructor

        public TutorialSequenceController(TutorialGraphState graphState, VisualElement canvas, TutorialSequenceAssetService sequenceAssetService, TutorialConnectionRenderer connectionRenderer)
        {
            this.graphState = graphState ?? throw new ArgumentNullException(nameof(graphState));
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.sequenceAssetService = sequenceAssetService ?? throw new ArgumentNullException(nameof(sequenceAssetService));
            this.connectionRenderer = connectionRenderer ?? throw new ArgumentNullException(nameof(connectionRenderer));
        }

        #endregion

        #region Connection Creation

        /// <summary>
        /// Start creating a StepSO to StepSO sequence connection
        /// </summary>
        /// <param name="step"></param>
        /// <param name="sourceNode"></param>
        /// <param name="sourcePort"></param>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool BeginConnection(StepSO step, VisualElement sourceNode, VisualElement sourcePort, Vector2 pointerPosition)
        {
            if (step == null || sourceNode == null || sourcePort == null)
            {
                return false;
            }

            Vector2 localPointerPosition = canvas.WorldToLocal(pointerPosition);
            bool hasStarted = graphState.TryBeginConnectionCreation(EConnectionCreationType.Sequence, step, sourceNode, sourcePort, localPointerPosition);

            if (hasStarted)
            {
                connectionRenderer.MarkDirty();
            }

            return hasStarted;
        }

        /// <summary>
        /// Update the temporary sequence connection position
        /// </summary>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool UpdateConnection(Vector2 pointerPosition)
        {
            Vector2 localPointerPosition = canvas.WorldToLocal(pointerPosition);
            bool hasUpdated = graphState.TryUpdateConnectionCreation(EConnectionCreationType.Sequence, localPointerPosition);

            if (hasUpdated)
            {
                connectionRenderer.MarkDirty();
            }

            return hasUpdated;
        }

        /// <summary>
        /// Complete the current StepSO sequence connection
        /// </summary>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool EndConnection(Vector2 pointerPosition)
        {
            if (!graphState.IsCreatingSequence)
            {
                return false;
            }

            try
            {
                StepSO sourceStep = graphState.ConnectionSourceStep;
                VisualElement sourceNode = graphState.ConnectionSourceNode;
                VisualElement sourcePort = graphState.ConnectionSourcePort;

                if (sourceStep == null || sourceNode == null || sourcePort == null)
                {
                    return false;
                }

                VisualElement targetPort = FindSequenceInputPort(pointerPosition);

                if (targetPort == null || targetPort.userData is not VisualElement targetNode)
                {
                    return false;
                }

                if (targetNode.userData is not StepSO targetStep)
                {
                    return false;
                }

                if (!ValidateSequenceConnection(sourceNode, targetNode))
                {
                    return false;
                }

                StepSequenceSO sequence = GetOrCreateSequence(sourceNode, targetNode);

                if (sequence == null)
                {
                    return false;
                }

                SequenceConnection connection = new SequenceConnection(sequence, sourceNode, sourcePort, targetNode, targetPort);

                if (!connection.IsValid)
                {
                    return false;
                }

                if (!graphState.AddSequenceConnection(connection))
                {
                    Debug.LogError($"Unable to register the sequence connection between '{sourceStep.name}' and '{targetStep.name}'.", sequence);

                    return false;
                }

                if (!RebuildSequence(sequence))
                {
                    graphState.RemoveSequenceConnection(connection);

                    Debug.LogError($"Unable to rebuild the sequence '{sequence.name}' after connecting '{sourceStep.name}' to '{targetStep.name}'.", sequence);

                    return false;
                }

                SequenceChanged?.Invoke();

                return true;
            }
            finally
            {
                graphState.TryResetConnectionCreation(EConnectionCreationType.Sequence);
                connectionRenderer.MarkDirty();
            }
        }

        /// <summary>
        /// Cancel the current sequence connection creation
        /// </summary>
        public void CancelConnection()
        {
            if (!graphState.TryResetConnectionCreation(EConnectionCreationType.Sequence))
            {
                return;
            }

            connectionRenderer.MarkDirty();
        }

        #endregion

        #region Connection Restoration

        /// <summary>
        /// Register one visual connection belonging to an existing StepSequenceSO without modifying the sequence asset
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <param name="connection"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryRegisterExistingSequenceConnection(StepSequenceSO sequence, VisualElement sourceNode, VisualElement targetNode, out SequenceConnection connection, out string failureReason)
        {
            connection = null;
            failureReason = string.Empty;

            if (sequence == null || sourceNode == null || targetNode == null)
            {
                failureReason = "The existing sequence connection contains missing data.";
                return false;
            }

            if (sourceNode.userData is not StepSO sourceStep || targetNode.userData is not StepSO targetStep)
            {
                failureReason = "The existing sequence connection does not contain valid StepSO nodes.";
                return false;
            }

            if (!AreConsecutiveSequenceSteps(sequence, sourceStep, targetStep))
            {
                failureReason = $"The Steps '{sourceStep.name}' and '{targetStep.name}' are not consecutive inside sequence '{sequence.name}'.";
                return false;
            }

            connection = FindExistingSequenceConnection(sequence, sourceNode, targetNode);

            if (connection != null)
            {
                return true;
            }

            if (!TryValidateRestoredSequence(sourceNode, targetNode, sequence, out failureReason))
            {
                return false;
            }

            VisualElement sourcePort = sourceNode.Q<VisualElement>(className: TutorialNodeFactory.SequenceOutputPortClass);
            VisualElement targetPort = targetNode.Q<VisualElement>(className: TutorialNodeFactory.SequenceInputPortClass);

            if (sourcePort == null)
            {
                failureReason = $"The sequence output port of StepSO '{sourceStep.name}' could not be found.";
                return false;
            }

            if (targetPort == null)
            {
                failureReason = $"The sequence input port of StepSO '{targetStep.name}' could not be found.";
                return false;
            }

            connection = new SequenceConnection(sequence, sourceNode, sourcePort, targetNode, targetPort);

            if (!connection.IsValid)
            {
                connection = null;
                failureReason = $"The sequence connection '{sourceStep.name} → {targetStep.name}' is invalid.";
                return false;
            }

            if (!graphState.AddSequenceConnection(connection))
            {
                connection = null;
                failureReason = $"Unable to register sequence connection '{sourceStep.name} → {targetStep.name}'.";
                return false;
            }

            connectionRenderer.MarkDirty();

            return true;
        }

        /// <summary>
        /// Restore a visual sequence connection without creating or modifying a StepSequenceSO asset
        /// </summary>
        /// <param name="sequenceData"></param>
        /// <param name="runtimeRegistry"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryRestoreConnection(TutorialResolvedSequenceData sequenceData, TutorialGraphRuntimeRegistry runtimeRegistry, out string failureReason)
        {
            failureReason = string.Empty;

            if (sequenceData == null)
            {
                failureReason = "The resolved sequence data is missing.";

                return false;
            }

            if (sequenceData.Sequence == null)
            {
                failureReason = "The restored sequence connection contains no StepSequenceSO.";

                return false;
            }

            if (runtimeRegistry == null)
            {
                failureReason = "The tutorial graph runtime registry is missing.";

                return false;
            }

            if (!runtimeRegistry.TryGetElement(sequenceData.SourceNodeGuid, out VisualElement sourceNode))
            {
                failureReason = $"Unable to find sequence source node '{sequenceData.SourceNodeGuid}'.";

                return false;
            }

            if (!runtimeRegistry.TryGetElement(sequenceData.TargetNodeGuid, out VisualElement targetNode))
            {
                failureReason = $"Unable to find sequence target node '{sequenceData.TargetNodeGuid}'.";

                return false;
            }

            if (sourceNode.userData is not StepSO sourceStep)
            {
                failureReason = $"Sequence source node '{sequenceData.SourceNodeGuid}' does not contain a StepSO.";

                return false;
            }

            if (targetNode.userData is not StepSO targetStep)
            {
                failureReason = $"Sequence target node '{sequenceData.TargetNodeGuid}' does not contain a StepSO.";

                return false;
            }

            if (!TryValidateRestoredSequence(sourceNode, targetNode, sequenceData.Sequence, out failureReason))
            {
                return false;
            }

            VisualElement sourcePort = sourceNode.Q<VisualElement>(className: TutorialNodeFactory.SequenceOutputPortClass);
            VisualElement targetPort = targetNode.Q<VisualElement>(className: TutorialNodeFactory.SequenceInputPortClass);

            if (sourcePort == null)
            {
                failureReason = $"The sequence output port of StepSO '{sourceStep.name}' could not be found.";

                return false;
            }

            if (targetPort == null)
            {
                failureReason = $"The sequence input port of StepSO '{targetStep.name}' could not be found.";

                return false;
            }

            SequenceConnection connection = new SequenceConnection(sequenceData.Sequence, sourceNode, sourcePort, targetNode, targetPort);

            if (!connection.IsValid)
            {
                failureReason = $"The restored sequence connection '{sourceStep.name} → {targetStep.name}' is invalid.";

                return false;
            }

            if (!graphState.AddSequenceConnection(connection))
            {
                failureReason = $"Unable to register restored sequence connection '{sourceStep.name} → {targetStep.name}'.";

                return false;
            }

            connectionRenderer.MarkDirty();

            return true;
        }

        /// <summary>
        /// Validate a sequence connection restored from persistent graph data
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <param name="sequence"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private bool TryValidateRestoredSequence(VisualElement sourceNode, VisualElement targetNode, StepSequenceSO sequence, out string failureReason)
        {
            failureReason = string.Empty;

            if (sourceNode == null || targetNode == null || sequence == null)
            {
                failureReason = "The restored sequence connection contains missing data.";

                return false;
            }

            if (sourceNode.userData is not StepSO sourceStep || targetNode.userData is not StepSO targetStep)
            {
                failureReason = "The restored sequence nodes do not contain valid StepSO assets.";

                return false;
            }

            if (sourceNode == targetNode)
            {
                failureReason = $"The StepSO node '{sourceStep.name}' cannot be connected to itself.";

                return false;
            }

            if (HasSequenceOutput(sourceNode))
            {
                failureReason = $"The StepSO node '{sourceStep.name}' already has a restored sequence output.";

                return false;
            }

            if (HasSequenceInput(targetNode))
            {
                failureReason = $"The StepSO node '{targetStep.name}' already has a restored sequence input.";

                return false;
            }

            if (WouldCreateSequenceCycle(sourceNode, targetNode))
            {
                failureReason = $"The restored connection '{sourceStep.name} → {targetStep.name}' would create a cycle.";

                return false;
            }

            StepSequenceSO sourceSequence = FindSequenceForNode(sourceNode);
            StepSequenceSO targetSequence = FindSequenceForNode(targetNode);

            if (sourceSequence != null && sourceSequence != sequence)
            {
                failureReason = $"The source node '{sourceStep.name}' already belongs to another StepSequenceSO.";

                return false;
            }

            if (targetSequence != null && targetSequence != sequence)
            {
                failureReason = $"The target node '{targetStep.name}' already belongs to another StepSequenceSO.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove every visual sequence connection without modifying StepSequenceSO assets
        /// </summary>
        /// <returns></returns>
        public int ClearVisualConnections()
        {
            int removedCount = graphState.SequenceConnections.Count;

            graphState.RemoveSequenceConnections(connection => true);

            if (removedCount > 0)
            {
                connectionRenderer.MarkDirty();
            }

            return removedCount;
        }

        #endregion


        #region Validation

        /// <summary>
        /// Check whether a sequence connection can be created
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private bool ValidateSequenceConnection(VisualElement sourceNode, VisualElement targetNode)
        {
            if (sourceNode == null || targetNode == null)
            {
                return false;
            }

            if (sourceNode.userData is not StepSO sourceStep || targetNode.userData is not StepSO targetStep)
            {
                return false;
            }

            if (sourceNode == targetNode)
            {
                Debug.LogWarning($"The StepSO node '{sourceStep.name}' cannot be connected to itself.", sourceStep);

                return false;
            }

            if (HasSequenceOutput(sourceNode))
            {
                Debug.LogWarning($"The StepSO node '{sourceStep.name}' already has a sequence output.", sourceStep);

                return false;
            }

            if (HasSequenceInput(targetNode))
            {
                Debug.LogWarning($"The StepSO node '{targetStep.name}' already has a sequence input.", targetStep);

                return false;
            }

            if (WouldCreateSequenceCycle(sourceNode, targetNode))
            {
                Debug.LogWarning($"The connection '{sourceStep.name} → {targetStep.name}' would create a sequence cycle.", sourceStep);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Check whether a visual node already has a sequence output
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool HasSequenceOutput(VisualElement node)
        {
            if (node == null)
            {
                return false;
            }

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.SourceNode == node)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether a visual node already has a sequence input
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool HasSequenceInput(VisualElement node)
        {
            if (node == null)
            {
                return false;
            }

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.TargetNode == node)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether a new connection would create a cycle
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private bool WouldCreateSequenceCycle(VisualElement sourceNode, VisualElement targetNode)
        {
            if (sourceNode == null || targetNode == null)
            {
                return false;
            }

            HashSet<VisualElement> visitedNodes = new HashSet<VisualElement>();
            VisualElement currentNode = targetNode;

            while (currentNode != null)
            {
                if (currentNode == sourceNode)
                {
                    return true;
                }

                if (!visitedNodes.Add(currentNode))
                {
                    return true;
                }

                currentNode = FindNextSequenceNode(currentNode, graphState.SequenceConnections);
            }

            return false;
        }

        #endregion

        #region Target Detection

        /// <summary>
        /// Find a sequence input port below the pointer
        /// </summary>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        private VisualElement FindSequenceInputPort(Vector2 pointerPosition)
        {
            if (canvas.panel == null || !canvas.worldBound.Contains(pointerPosition))
            {
                return null;
            }

            VisualElement pickedElement = canvas.panel.Pick(pointerPosition);

            while (pickedElement != null)
            {
                if (pickedElement.ClassListContains(TutorialNodeFactory.SequenceInputPortClass))
                {
                    return pickedElement;
                }

                pickedElement = pickedElement.parent;
            }

            return null;
        }

        #endregion

        #region Sequence Resolution

        /// <summary>
        /// Find or create the StepSequenceSO associated with two nodes
        /// </summary>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private StepSequenceSO GetOrCreateSequence(VisualElement sourceNode, VisualElement targetNode)
        {
            StepSO sourceStep = sourceNode?.userData as StepSO;
            StepSO targetStep = targetNode?.userData as StepSO;

            if (sourceStep == null || targetStep == null)
            {
                return null;
            }

            StepSequenceSO sourceSequence = FindSequenceForNode(sourceNode);
            StepSequenceSO targetSequence = FindSequenceForNode(targetNode);

            if (sourceSequence != null && targetSequence != null)
            {
                if (sourceSequence == targetSequence)
                {
                    return sourceSequence;
                }

                Debug.LogError("Two different StepSequenceSO assets cannot be merged automatically.");

                return null;
            }

            if (sourceSequence != null)
            {
                return sourceSequence;
            }

            if (targetSequence != null)
            {
                return targetSequence;
            }

            return CreateSequenceAsset(sourceStep, targetStep);
        }

        /// <summary>
        /// Find the StepSequenceSO associated with a visual node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private StepSequenceSO FindSequenceForNode(VisualElement node)
        {
            if (node == null)
            {
                return null;
            }

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.SourceNode == node || connection.TargetNode == node)
                {
                    return connection.Sequence;
                }
            }

            return null;
        }

        /// <summary>
        /// Create a new StepSequenceSO through the configured asset service
        /// </summary>
        /// <param name="sourceStep"></param>
        /// <param name="targetStep"></param>
        /// <returns></returns>
        private StepSequenceSO CreateSequenceAsset(StepSO sourceStep, StepSO targetStep)
        {
            if (!sequenceAssetService.TryCreateSequenceAsset(sourceStep, targetStep, out StepSequenceSO sequence))
            {
                return null;
            }

            return sequence;
        }

        #endregion

        #region Sequence Rebuild

        /// <summary>
        /// Rebuild the ordered StepSO list of a StepSequenceSO
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private bool RebuildSequence(StepSequenceSO sequence)
        {
            if (sequence == null)
            {
                return false;
            }

            List<SequenceConnection> sequenceConnections = GetConnectionsForSequence(sequence);

            if (sequenceConnections.Count == 0)
            {
                SetSequenceSteps(sequence, new List<StepSO>());

                return true;
            }

            if (!IsSingleLinearChain(sequenceConnections))
            {
                Debug.LogError($"The sequence '{sequence.name}' contains several disconnected chains or an invalid cycle.", sequence);

                return false;
            }

            VisualElement firstNode = FindSequenceFirstNode(sequenceConnections);

            if (firstNode == null)
            {
                Debug.LogError($"Unable to find the first StepSO of sequence '{sequence.name}'.", sequence);

                return false;
            }

            List<StepSO> orderedSteps = new List<StepSO>();
            HashSet<VisualElement> visitedNodes = new HashSet<VisualElement>();
            VisualElement currentNode = firstNode;

            while (currentNode != null)
            {
                if (!visitedNodes.Add(currentNode))
                {
                    Debug.LogError($"A cycle was detected inside sequence '{sequence.name}'.", sequence);

                    return false;
                }

                if (currentNode.userData is not StepSO step)
                {
                    Debug.LogError($"A visual node inside sequence '{sequence.name}' does not contain a StepSO.", sequence);

                    return false;
                }

                orderedSteps.Add(step);
                currentNode = FindNextSequenceNode(currentNode, sequenceConnections);
            }

            if (orderedSteps.Count != sequenceConnections.Count + 1)
            {
                Debug.LogError($"The sequence '{sequence.name}' could not be completely traversed.", sequence);

                return false;
            }

            SetSequenceSteps(sequence, orderedSteps);

            return true;
        }

        /// <summary>
        /// Get every connection associated with a StepSequenceSO
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private List<SequenceConnection> GetConnectionsForSequence(StepSequenceSO sequence)
        {
            List<SequenceConnection> connections = new List<SequenceConnection>();

            if (sequence == null)
            {
                return connections;
            }

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid || connection.Sequence != sequence)
                {
                    continue;
                }

                connections.Add(connection);
            }

            return connections;
        }

        /// <summary>
        /// Check whether a collection of connections represents one linear chain
        /// </summary>
        /// <param name="connections"></param>
        /// <returns></returns>
        private static bool IsSingleLinearChain(IReadOnlyList<SequenceConnection> connections)
        {
            if (connections == null || connections.Count == 0)
            {
                return true;
            }

            VisualElement firstNode = FindSequenceFirstNode(connections);

            if (firstNode == null)
            {
                return false;
            }

            HashSet<VisualElement> visitedNodes = new HashSet<VisualElement>();
            VisualElement currentNode = firstNode;

            while (currentNode != null)
            {
                if (!visitedNodes.Add(currentNode))
                {
                    return false;
                }

                currentNode = FindNextSequenceNode(currentNode, connections);
            }

            return visitedNodes.Count == connections.Count + 1;
        }

        /// <summary>
        /// Find the first visual node of a sequence
        /// </summary>
        /// <param name="connections"></param>
        /// <returns></returns>
        private static VisualElement FindSequenceFirstNode(IReadOnlyList<SequenceConnection> connections)
        {
            if (connections == null)
            {
                return null;
            }

            foreach (SequenceConnection candidate in connections)
            {
                if (candidate == null || !candidate.IsValid)
                {
                    continue;
                }

                bool hasIncomingConnection = false;

                foreach (SequenceConnection other in connections)
                {
                    if (other == null || !other.IsValid)
                    {
                        continue;
                    }

                    if (other.TargetNode == candidate.SourceNode)
                    {
                        hasIncomingConnection = true;

                        break;
                    }
                }

                if (!hasIncomingConnection)
                {
                    return candidate.SourceNode;
                }
            }

            return null;
        }

        /// <summary>
        /// Find the next visual node connected to a sequence node
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        private static VisualElement FindNextSequenceNode(VisualElement currentNode, IReadOnlyList<SequenceConnection> connections)
        {
            if (currentNode == null || connections == null)
            {
                return null;
            }

            foreach (SequenceConnection connection in connections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.SourceNode == currentNode)
                {
                    return connection.TargetNode;
                }
            }

            return null;
        }

        /// <summary>
        /// Save an ordered StepSO list inside a StepSequenceSO
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="orderedSteps"></param>
        private static void SetSequenceSteps(StepSequenceSO sequence, List<StepSO> orderedSteps)
        {
            if (sequence == null)
            {
                return;
            }

            Undo.RecordObject(sequence, "Update tutorial sequence");

            sequence.SetSequence(orderedSteps ?? new List<StepSO>());

            EditorUtility.SetDirty(sequence);
            AssetDatabase.SaveAssetIfDirty(sequence);
        }

        #endregion

        #region Connection Deletion

        /// <summary>
        /// Delete a sequence connection and rebuild its StepSequenceSO
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool DeleteConnection(SequenceConnection connection)
        {
            if (connection == null || !connection.IsValid)
            {
                return false;
            }

            if (!CanRemoveConnectionWithoutSplitting(connection))
            {
                Debug.LogWarning($"The connection '{connection.SourceStep.name} → {connection.TargetStep.name}' cannot be removed because it would split '{connection.Sequence.name}' into two separate chains.", connection.Sequence);

                return false;
            }

            StepSequenceSO sequence = connection.Sequence;

            if (!graphState.RemoveSequenceConnection(connection))
            {
                return false;
            }

            if (!RebuildSequence(sequence))
            {
                graphState.AddSequenceConnection(connection);
                RebuildSequence(sequence);

                return false;
            }

            connectionRenderer.MarkDirty();
            SequenceChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Remove every sequence connection associated with a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool RemoveConnectionsForNode(VisualElement node)
        {
            if (node == null)
            {
                return false;
            }

            HashSet<StepSequenceSO> affectedSequences = new HashSet<StepSequenceSO>();

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.SourceNode != node && connection.TargetNode != node)
                {
                    continue;
                }

                affectedSequences.Add(connection.Sequence);
            }

            foreach (StepSequenceSO sequence in affectedSequences)
            {
                if (!CanRemoveNodeWithoutSplitting(sequence, node))
                {
                    Debug.LogWarning($"The node cannot be removed because it would split the sequence '{sequence.name}' into two separate chains.", sequence);

                    return false;
                }
            }

            graphState.RemoveSequenceConnections(connection => connection.SourceNode == node || connection.TargetNode == node);

            foreach (StepSequenceSO sequence in affectedSequences)
            {
                RebuildSequence(sequence);
            }

            connectionRenderer.MarkDirty();

            return true;
        }

        /// <summary>
        /// Check whether removing a connection preserves a single linear chain
        /// </summary>
        /// <param name="connectionToRemove"></param>
        /// <returns></returns>
        private bool CanRemoveConnectionWithoutSplitting(SequenceConnection connectionToRemove)
        {
            if (connectionToRemove == null || connectionToRemove.Sequence == null)
            {
                return false;
            }

            List<SequenceConnection> remainingConnections = new List<SequenceConnection>();

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid || connection == connectionToRemove)
                {
                    continue;
                }

                if (connection.Sequence == connectionToRemove.Sequence)
                {
                    remainingConnections.Add(connection);
                }
            }

            return IsSingleLinearChain(remainingConnections);
        }

        /// <summary>
        /// Check whether removing a node preserves a single linear chain
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool CanRemoveNodeWithoutSplitting(StepSequenceSO sequence, VisualElement node)
        {
            if (sequence == null || node == null)
            {
                return false;
            }

            List<SequenceConnection> remainingConnections = new List<SequenceConnection>();

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid || connection.Sequence != sequence)
                {
                    continue;
                }

                if (connection.SourceNode == node || connection.TargetNode == node)
                {
                    continue;
                }

                remainingConnections.Add(connection);
            }

            return IsSingleLinearChain(remainingConnections);
        }

        /// <summary>
        /// Find an already registered visual connection between two nodes of the same sequence
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="sourceNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private SequenceConnection FindExistingSequenceConnection(StepSequenceSO sequence, VisualElement sourceNode, VisualElement targetNode)
        {
            if (sequence == null || sourceNode == null || targetNode == null)
            {
                return null;
            }

            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.Sequence == sequence && connection.SourceNode == sourceNode && connection.TargetNode == targetNode)
                {
                    return connection;
                }
            }

            return null;
        }

        /// <summary>
        /// Check whether two Steps are consecutive inside an existing StepSequenceSO
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="sourceStep"></param>
        /// <param name="targetStep"></param>
        /// <returns></returns>
        private static bool AreConsecutiveSequenceSteps(StepSequenceSO sequence, StepSO sourceStep, StepSO targetStep)
        {
            if (sequence == null || sourceStep == null || targetStep == null || sequence.SequenceSOList == null)
            {
                return false;
            }

            int sourceIndex = sequence.SequenceSOList.IndexOf(sourceStep);

            return sourceIndex >= 0 && sourceIndex + 1 < sequence.SequenceSOList.Count && sequence.SequenceSOList[sourceIndex + 1] == targetStep;
        }

        #endregion
    }
}