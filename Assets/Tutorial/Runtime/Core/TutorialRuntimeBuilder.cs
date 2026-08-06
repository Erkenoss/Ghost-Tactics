using System;
using System.Collections.Generic;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Persistence;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Runtime.Core
{
    /// <summary>
    /// Build an isolated runtime representation of a tutorial graph
    /// </summary>
    public sealed class TutorialRuntimeBuilder
    {
        #region Public Methods

        /// <summary>
        /// Build the complete runtime representation of a tutorial graph
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="sourceNodes"></param>
        /// <param name="runtimeInstance"></param>
        /// <returns></returns>
        public bool TryBuild(TutorialGraphAsset sourceGraph, IReadOnlyDictionary<string, StepSO> sourceNodes, out TutorialRuntimeInstance runtimeInstance)
        {
            runtimeInstance = null;

            if (!TryValidateSourceGraph(sourceGraph, sourceNodes, out string validationError))
            {
                Debug.LogError(validationError);

                return false;
            }

            TutorialRuntimeInstance createdInstance = new TutorialRuntimeInstance(sourceGraph);
            List<StepSO> collectedSourceSteps = new List<StepSO>();
            HashSet<StepSO> registeredSourceSteps = new HashSet<StepSO>();
            HashSet<StepSequenceSO> sequencePath = new HashSet<StepSequenceSO>();
            Dictionary<string, StepSO> sourceStepsByGuid = new Dictionary<string, StepSO>(StringComparer.Ordinal);
            Dictionary<StepSO, StepSO> runtimeClonesBySource = new Dictionary<StepSO, StepSO>();

            if (!TryCollectSourceSteps(sourceNodes, collectedSourceSteps, registeredSourceSteps, sequencePath, sourceStepsByGuid, out string collectionError))
            {
                return FailBuild(createdInstance, collectionError, out runtimeInstance);
            }

            if (!TryCloneSourceSteps(createdInstance, collectedSourceSteps, runtimeClonesBySource, out string cloneError))
            {
                return FailBuild(createdInstance, cloneError, out runtimeInstance);
            }

            if (!TryRebuildRuntimeSequences(collectedSourceSteps, runtimeClonesBySource, out string sequenceError))
            {
                return FailBuild(createdInstance, sequenceError, out runtimeInstance);
            }

            if (!TryCreateRuntimeNodes(createdInstance, sourceNodes, runtimeClonesBySource, out string nodeError))
            {
                return FailBuild(createdInstance, nodeError, out runtimeInstance);
            }

            if (!TryRebuildTransitions(createdInstance, sourceGraph.SaveData.Sequences, out string transitionError))
            {
                return FailBuild(createdInstance, transitionError, out runtimeInstance);
            }

            if (!TryValidateRuntimeCycles(createdInstance, out string cycleError))
            {
                return FailBuild(createdInstance, cycleError, out runtimeInstance);
            }

            if (!TryConfigureRuntimeEntry(createdInstance, out string entryError))
            {
                return FailBuild(createdInstance, entryError, out runtimeInstance);
            }

            if (!TryValidateRuntimeFlow(createdInstance, out string flowError))
            {
                return FailBuild(createdInstance, flowError, out runtimeInstance);
            }

            createdInstance.SetStatus(ETutorialRuntimeInstanceStatus.Ready);
            runtimeInstance = createdInstance;

            return true;
        }

        #endregion

        #region Source Validation

        /// <summary>
        /// Validate the source graph and the resolved StepSO nodes
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="sourceNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateSourceGraph(TutorialGraphAsset sourceGraph, IReadOnlyDictionary<string, StepSO> sourceNodes, out string error)
        {
            error = string.Empty;

            if (sourceGraph == null)
            {
                error = "The tutorial runtime graph cannot be built because the source graph is null.";

                return false;
            }

            if (!sourceGraph.IsInitialized)
            {
                error = $"The tutorial graph '{sourceGraph.name}' is not initialized.";

                return false;
            }

            if (sourceGraph.SaveData == null)
            {
                error = $"The tutorial graph '{sourceGraph.name}' contains no save data.";

                return false;
            }

            if (sourceGraph.SaveData.Version != TutorialGraphSaveData.CurrentVersion)
            {
                error = $"The tutorial graph '{sourceGraph.name}' uses unsupported save version '{sourceGraph.SaveData.Version}'.";

                return false;
            }

            if (sourceGraph.SaveData.Nodes == null)
            {
                error = $"The tutorial graph '{sourceGraph.name}' contains no node collection.";

                return false;
            }

            if (sourceGraph.SaveData.Sequences == null)
            {
                error = $"The tutorial graph '{sourceGraph.name}' contains no sequence collection.";

                return false;
            }

            if (sourceNodes == null || sourceNodes.Count == 0)
            {
                error = $"The tutorial graph '{sourceGraph.name}' contains no resolved StepSO node.";

                return false;
            }

            return TryValidateResolvedSourceNodes(sourceGraph.SaveData.Nodes, sourceNodes, out error);
        }

        /// <summary>
        /// Validate that every saved Step node has one resolved StepSO reference
        /// </summary>
        /// <param name="savedNodes"></param>
        /// <param name="sourceNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateResolvedSourceNodes(IReadOnlyList<TutorialNodeSaveData> savedNodes, IReadOnlyDictionary<string, StepSO> sourceNodes, out string error)
        {
            error = string.Empty;

            HashSet<string> registeredNodeGuids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> savedStepNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (TutorialNodeSaveData savedNode in savedNodes)
            {
                if (savedNode == null)
                {
                    error = "A null node was found inside the tutorial graph save data.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(savedNode.NodeGuid))
                {
                    error = "A saved tutorial node contains an empty node GUID.";

                    return false;
                }

                if (!registeredNodeGuids.Add(savedNode.NodeGuid))
                {
                    error = $"The saved node GUID '{savedNode.NodeGuid}' is duplicated.";

                    return false;
                }

                if (savedNode.NodeType == ETutorialNodeType.None)
                {
                    error = $"The saved node '{savedNode.NodeGuid}' contains no valid node type.";

                    return false;
                }

                if (savedNode.NodeType != ETutorialNodeType.Step)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(savedNode.AssetGuid))
                {
                    error = $"The saved Step node '{savedNode.NodeGuid}' contains no Unity asset GUID.";

                    return false;
                }

                savedStepNodeGuids.Add(savedNode.NodeGuid);
            }

            foreach (KeyValuePair<string, StepSO> sourceNode in sourceNodes)
            {
                if (string.IsNullOrWhiteSpace(sourceNode.Key))
                {
                    error = "A resolved StepSO node contains an empty node GUID.";

                    return false;
                }

                if (sourceNode.Value == null)
                {
                    error = $"The resolved node '{sourceNode.Key}' contains no StepSO.";

                    return false;
                }

                if (!savedStepNodeGuids.Contains(sourceNode.Key))
                {
                    error = $"The resolved StepSO node '{sourceNode.Key}' does not match a saved Step node.";

                    return false;
                }
            }

            if (sourceNodes.Count != savedStepNodeGuids.Count)
            {
                List<string> missingNodeGuids = new List<string>();

                foreach (string savedStepNodeGuid in savedStepNodeGuids)
                {
                    if (!sourceNodes.ContainsKey(savedStepNodeGuid))
                    {
                        missingNodeGuids.Add(savedStepNodeGuid);
                    }
                }

                missingNodeGuids.Sort(StringComparer.Ordinal);
                error = $"The tutorial graph contains unresolved Step nodes: {string.Join(", ", missingNodeGuids)}.";

                return false;
            }

            return true;
        }

        #endregion

        #region Source Collection

        /// <summary>
        /// Collect every StepSO required by the runtime graph
        /// </summary>
        /// <param name="sourceNodes"></param>
        /// <param name="collectedSourceSteps"></param>
        /// <param name="registeredSourceSteps"></param>
        /// <param name="sequencePath"></param>
        /// <param name="sourceStepsByGuid"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryCollectSourceSteps(IReadOnlyDictionary<string, StepSO> sourceNodes, List<StepSO> collectedSourceSteps, HashSet<StepSO> registeredSourceSteps, HashSet<StepSequenceSO> sequencePath, Dictionary<string, StepSO> sourceStepsByGuid, out string error)
        {
            error = string.Empty;

            foreach (KeyValuePair<string, StepSO> sourceNode in sourceNodes)
            {
                if (!TryCollectSourceStep(sourceNode.Value, collectedSourceSteps, registeredSourceSteps, sequencePath, sourceStepsByGuid, out error))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Collect one StepSO and every Step contained inside its sequence
        /// </summary>
        /// <param name="sourceStep"></param>
        /// <param name="collectedSourceSteps"></param>
        /// <param name="registeredSourceSteps"></param>
        /// <param name="sequencePath"></param>
        /// <param name="sourceStepsByGuid"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryCollectSourceStep(StepSO sourceStep, List<StepSO> collectedSourceSteps, HashSet<StepSO> registeredSourceSteps, HashSet<StepSequenceSO> sequencePath, Dictionary<string, StepSO> sourceStepsByGuid, out string error)
        {
            error = string.Empty;

            if (sourceStep == null)
            {
                error = "A null StepSO was found while collecting the tutorial runtime graph.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(sourceStep.StepGUID))
            {
                error = $"The StepSO '{sourceStep.name}' contains no Step GUID.";

                return false;
            }

            if (sourceStepsByGuid.TryGetValue(sourceStep.StepGUID, out StepSO registeredStep) && registeredStep != sourceStep)
            {
                error = $"The StepSO '{sourceStep.name}' and '{registeredStep.name}' share the same Step GUID: {sourceStep.StepGUID}.";

                return false;
            }

            if (sourceStep is StepSequenceSO cyclicSequence && sequencePath.Contains(cyclicSequence))
            {
                error = $"A nested StepSequenceSO cycle was detected inside '{cyclicSequence.name}'.";

                return false;
            }

            if (!registeredSourceSteps.Add(sourceStep))
            {
                return true;
            }

            sourceStepsByGuid.Add(sourceStep.StepGUID, sourceStep);
            collectedSourceSteps.Add(sourceStep);

            if (sourceStep is not StepSequenceSO sourceSequence)
            {
                return true;
            }

            if (sourceSequence.SequenceSOList == null)
            {
                error = $"The StepSequenceSO '{sourceSequence.name}' contains a null Step collection.";

                return false;
            }

            sequencePath.Add(sourceSequence);

            foreach (StepSO sequenceStep in sourceSequence.SequenceSOList)
            {
                if (!TryCollectSourceStep(sequenceStep, collectedSourceSteps, registeredSourceSteps, sequencePath, sourceStepsByGuid, out error))
                {
                    sequencePath.Remove(sourceSequence);

                    return false;
                }
            }

            sequencePath.Remove(sourceSequence);

            return true;
        }

        #endregion

        #region Step Cloning

        /// <summary>
        /// Clone every collected StepSO and register it inside the runtime instance
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="collectedSourceSteps"></param>
        /// <param name="runtimeClonesBySource"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryCloneSourceSteps(TutorialRuntimeInstance runtimeInstance, IReadOnlyList<StepSO> collectedSourceSteps, Dictionary<StepSO, StepSO> runtimeClonesBySource, out string error)
        {
            error = string.Empty;

            foreach (StepSO sourceStep in collectedSourceSteps)
            {
                StepSO runtimeStep = UnityObject.Instantiate(sourceStep);

                if (runtimeStep == null)
                {
                    error = $"The StepSO '{sourceStep.name}' could not be cloned.";

                    return false;
                }

                runtimeStep.name = $"{sourceStep.name} Runtime";
                runtimeStep.hideFlags = HideFlags.DontSave;

                if (!runtimeInstance.RegisterRuntimeStep(sourceStep.StepGUID, runtimeStep))
                {
                    DestroyRuntimeStep(runtimeStep);
                    error = $"The runtime clone of '{sourceStep.name}' could not be registered with GUID '{sourceStep.StepGUID}'.";

                    return false;
                }

                runtimeClonesBySource.Add(sourceStep, runtimeStep);
            }

            return true;
        }

        #endregion

        #region Sequence Reconstruction

        /// <summary>
        /// Replace every source StepSequenceSO reference with its runtime clone
        /// </summary>
        /// <param name="collectedSourceSteps"></param>
        /// <param name="runtimeClonesBySource"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryRebuildRuntimeSequences(IReadOnlyList<StepSO> collectedSourceSteps, IReadOnlyDictionary<StepSO, StepSO> runtimeClonesBySource, out string error)
        {
            error = string.Empty;

            foreach (StepSO sourceStep in collectedSourceSteps)
            {
                if (sourceStep is not StepSequenceSO sourceSequence)
                {
                    continue;
                }

                if (!runtimeClonesBySource.TryGetValue(sourceSequence, out StepSO runtimeSequenceStep) || runtimeSequenceStep is not StepSequenceSO runtimeSequence)
                {
                    error = $"The runtime clone of sequence '{sourceSequence.name}' could not be found.";

                    return false;
                }

                List<StepSO> runtimeSequenceSteps = new List<StepSO>();

                foreach (StepSO sourceSequenceStep in sourceSequence.SequenceSOList)
                {
                    if (sourceSequenceStep == null || !runtimeClonesBySource.TryGetValue(sourceSequenceStep, out StepSO runtimeSequenceChild))
                    {
                        error = $"A runtime Step reference is missing inside sequence '{sourceSequence.name}'.";

                        return false;
                    }

                    runtimeSequenceSteps.Add(runtimeSequenceChild);
                }

                runtimeSequence.SetSequence(runtimeSequenceSteps);
            }

            return true;
        }

        #endregion

        #region Runtime Nodes

        /// <summary>
        /// Create every runtime node from the resolved source Step nodes
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="sourceNodes"></param>
        /// <param name="runtimeClonesBySource"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryCreateRuntimeNodes(TutorialRuntimeInstance runtimeInstance, IReadOnlyDictionary<string, StepSO> sourceNodes, IReadOnlyDictionary<StepSO, StepSO> runtimeClonesBySource, out string error)
        {
            error = string.Empty;

            foreach (KeyValuePair<string, StepSO> sourceNode in sourceNodes)
            {
                if (!runtimeClonesBySource.TryGetValue(sourceNode.Value, out StepSO runtimeStep))
                {
                    error = $"The runtime clone associated with node '{sourceNode.Key}' could not be found.";

                    return false;
                }

                TutorialRuntimeNode runtimeNode = new TutorialRuntimeNode(sourceNode.Key, sourceNode.Value.StepGUID, runtimeStep);

                if (!runtimeInstance.RegisterRuntimeNode(runtimeNode))
                {
                    error = $"The runtime node '{sourceNode.Key}' could not be registered.";

                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Transition Reconstruction

        /// <summary>
        /// Reconstruct every runtime transition from the persistent sequence connections
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="savedSequences"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryRebuildTransitions(TutorialRuntimeInstance runtimeInstance, IReadOnlyList<TutorialSequenceSaveData> savedSequences, out string error)
        {
            error = string.Empty;

            HashSet<string> registeredTransitions = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sourceNodeGuids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> targetNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (TutorialSequenceSaveData savedSequence in savedSequences)
            {
                if (savedSequence == null)
                {
                    error = "A null sequence connection was found inside the tutorial graph.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(savedSequence.SourceNodeGuid))
                {
                    error = "A tutorial sequence contains an empty source node GUID.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(savedSequence.TargetNodeGuid))
                {
                    error = $"The sequence starting from node '{savedSequence.SourceNodeGuid}' contains an empty target node GUID.";

                    return false;
                }

                if (string.Equals(savedSequence.SourceNodeGuid, savedSequence.TargetNodeGuid, StringComparison.Ordinal))
                {
                    error = $"The runtime node '{savedSequence.SourceNodeGuid}' cannot transition toward itself.";

                    return false;
                }

                if (!runtimeInstance.TryGetRuntimeNode(savedSequence.SourceNodeGuid, out TutorialRuntimeNode sourceNode))
                {
                    error = $"The sequence source node '{savedSequence.SourceNodeGuid}' does not reference a valid runtime Step node.";

                    return false;
                }

                if (!runtimeInstance.TryGetRuntimeNode(savedSequence.TargetNodeGuid, out TutorialRuntimeNode targetNode))
                {
                    error = $"The sequence target node '{savedSequence.TargetNodeGuid}' does not reference a valid runtime Step node.";

                    return false;
                }

                string transitionKey = BuildTransitionKey(sourceNode.NodeGuid, targetNode.NodeGuid);

                if (!registeredTransitions.Add(transitionKey))
                {
                    error = $"The transition '{sourceNode.NodeGuid}' to '{targetNode.NodeGuid}' is duplicated.";

                    return false;
                }

                if (!sourceNodeGuids.Add(sourceNode.NodeGuid))
                {
                    error = $"The runtime node '{sourceNode.NodeGuid}' contains more than one outgoing sequence transition.";

                    return false;
                }

                if (!targetNodeGuids.Add(targetNode.NodeGuid))
                {
                    error = $"The runtime node '{targetNode.NodeGuid}' contains more than one incoming sequence transition.";

                    return false;
                }

                if (!sourceNode.AddTransition(targetNode.NodeGuid))
                {
                    error = $"The transition '{sourceNode.NodeGuid}' to '{targetNode.NodeGuid}' could not be registered.";

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Build the unique identifier of one runtime transition
        /// </summary>
        /// <param name="sourceNodeGuid"></param>
        /// <param name="targetNodeGuid"></param>
        /// <returns></returns>
        private static string BuildTransitionKey(string sourceNodeGuid, string targetNodeGuid)
        {
            return $"{sourceNodeGuid}>{targetNodeGuid}";
        }

        #endregion

        #region Cycle Validation

        /// <summary>
        /// Validate that the reconstructed runtime graph contains no cycle
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateRuntimeCycles(TutorialRuntimeInstance runtimeInstance, out string error)
        {
            error = string.Empty;

            HashSet<string> visitedNodeGuids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> currentPathNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (string nodeGuid in runtimeInstance.RuntimeNodes.Keys)
            {
                if (!TryValidateRuntimeCycles(runtimeInstance, nodeGuid, visitedNodeGuids, currentPathNodeGuids, out error))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Traverse one runtime branch and detect recursive references
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="nodeGuid"></param>
        /// <param name="visitedNodeGuids"></param>
        /// <param name="currentPathNodeGuids"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateRuntimeCycles(TutorialRuntimeInstance runtimeInstance, string nodeGuid, HashSet<string> visitedNodeGuids, HashSet<string> currentPathNodeGuids, out string error)
        {
            error = string.Empty;

            if (currentPathNodeGuids.Contains(nodeGuid))
            {
                error = $"A cycle was detected at runtime node '{nodeGuid}'.";

                return false;
            }

            if (visitedNodeGuids.Contains(nodeGuid))
            {
                return true;
            }

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                error = $"The runtime node '{nodeGuid}' could not be found while validating graph cycles.";

                return false;
            }

            currentPathNodeGuids.Add(nodeGuid);

            foreach (string targetNodeGuid in runtimeNode.NextNodeGuids)
            {
                if (!TryValidateRuntimeCycles(runtimeInstance, targetNodeGuid, visitedNodeGuids, currentPathNodeGuids, out error))
                {
                    return false;
                }
            }

            currentPathNodeGuids.Remove(nodeGuid);
            visitedNodeGuids.Add(nodeGuid);

            return true;
        }

        #endregion

        #region Entry Node

        /// <summary>
        /// Find and configure the unique entry node of the runtime graph
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryConfigureRuntimeEntry(TutorialRuntimeInstance runtimeInstance, out string error)
        {
            error = string.Empty;

            HashSet<string> targetNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (TutorialRuntimeNode runtimeNode in runtimeInstance.RuntimeNodes.Values)
            {
                foreach (string targetNodeGuid in runtimeNode.NextNodeGuids)
                {
                    targetNodeGuids.Add(targetNodeGuid);
                }
            }

            List<string> entryNodeGuids = new List<string>();

            foreach (string nodeGuid in runtimeInstance.RuntimeNodes.Keys)
            {
                if (!targetNodeGuids.Contains(nodeGuid))
                {
                    entryNodeGuids.Add(nodeGuid);
                }
            }

            entryNodeGuids.Sort(StringComparer.Ordinal);

            if (entryNodeGuids.Count == 0)
            {
                error = $"The tutorial graph '{runtimeInstance.SourceGraph.name}' contains no entry node.";

                return false;
            }

            if (entryNodeGuids.Count > 1)
            {
                error = $"The tutorial graph '{runtimeInstance.SourceGraph.name}' contains multiple entry nodes: {string.Join(", ", entryNodeGuids)}.";

                return false;
            }

            if (!runtimeInstance.SetEntryNode(entryNodeGuids[0]))
            {
                error = $"The runtime entry node '{entryNodeGuids[0]}' could not be configured.";

                return false;
            }

            return true;
        }

        #endregion

        #region Flow Validation

        /// <summary>
        /// Validate that every runtime node can be reached from the graph entry
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateRuntimeFlow(TutorialRuntimeInstance runtimeInstance, out string error)
        {
            error = string.Empty;

            HashSet<string> reachableNodeGuids = new HashSet<string>(StringComparer.Ordinal);
            Stack<string> pendingNodeGuids = new Stack<string>();

            pendingNodeGuids.Push(runtimeInstance.EntryNodeGuid);

            while (pendingNodeGuids.Count > 0)
            {
                string nodeGuid = pendingNodeGuids.Pop();

                if (!reachableNodeGuids.Add(nodeGuid))
                {
                    continue;
                }

                if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
                {
                    error = $"The runtime node '{nodeGuid}' could not be found while validating the tutorial flow.";

                    return false;
                }

                foreach (string targetNodeGuid in runtimeNode.NextNodeGuids)
                {
                    pendingNodeGuids.Push(targetNodeGuid);
                }
            }

            if (reachableNodeGuids.Count == runtimeInstance.RuntimeNodes.Count)
            {
                return true;
            }

            List<string> unreachableNodeGuids = new List<string>();

            foreach (string nodeGuid in runtimeInstance.RuntimeNodes.Keys)
            {
                if (!reachableNodeGuids.Contains(nodeGuid))
                {
                    unreachableNodeGuids.Add(nodeGuid);
                }
            }

            unreachableNodeGuids.Sort(StringComparer.Ordinal);
            error = $"The tutorial graph '{runtimeInstance.SourceGraph.name}' contains unreachable Step nodes: {string.Join(", ", unreachableNodeGuids)}.";

            return false;
        }

        #endregion

        #region Failure

        /// <summary>
        /// Abort the current build and release every created runtime object
        /// </summary>
        /// <param name="createdInstance"></param>
        /// <param name="error"></param>
        /// <param name="runtimeInstance"></param>
        /// <returns></returns>
        private static bool FailBuild(TutorialRuntimeInstance createdInstance, string error, out TutorialRuntimeInstance runtimeInstance)
        {
            Debug.LogError(error);

            if (createdInstance != null)
            {
                createdInstance.SetStatus(ETutorialRuntimeInstanceStatus.Failed);
                createdInstance.Dispose();
            }

            runtimeInstance = null;

            return false;
        }

        /// <summary>
        /// Destroy a runtime StepSO that could not be registered
        /// </summary>
        /// <param name="runtimeStep"></param>
        private static void DestroyRuntimeStep(StepSO runtimeStep)
        {
            if (runtimeStep == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityObject.Destroy(runtimeStep);
            }
            else
            {
                UnityObject.DestroyImmediate(runtimeStep);
            }
        }

        #endregion
    }
}
