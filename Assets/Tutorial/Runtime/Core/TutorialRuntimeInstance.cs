using System;
using System.Collections.Generic;
using System.Text;
using Tutorial.Runtime.Persistence;
using Tutorial.Runtime.Data;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Runtime.Core
{
    /// <summary>
    /// Represents one reconstructed tutorial graph instance during runtime
    /// </summary>
    public sealed class TutorialRuntimeInstance : IDisposable
    {
        #region Constants

        /// <summary>
        /// Maximum recursive depth allowed when displaying the runtime graph
        /// </summary>
        private const int MaximumDebugTraversalDepth = 512;

        #endregion

        #region Private Fields

        /// <summary>
        /// Persistent graph used to create this runtime instance
        /// </summary>
        private readonly TutorialGraphAsset sourceGraph = null;

        /// <summary>
        /// Unique identifier of the source tutorial
        /// </summary>
        private readonly string tutorialGuid = string.Empty;

        /// <summary>
        /// Replay policy configured by the tutorial creator
        /// </summary>
        private readonly ETutorialReplayPolicy replayPolicy = ETutorialReplayPolicy.Disabled;

        /// <summary>
        /// Runtime StepSO clones indexed by their persistent Step GUID
        /// </summary>
        private readonly Dictionary<string, StepSO> runtimeSteps = new Dictionary<string, StepSO>();

        /// <summary>
        /// Runtime graph nodes indexed by their persistent node GUID
        /// </summary>
        private readonly Dictionary<string, TutorialRuntimeNode> runtimeNodes = new Dictionary<string, TutorialRuntimeNode>();

        /// <summary>
        /// Root nodes from which independent runtime tutorial flows can begin
        /// </summary>
        private readonly List<string> rootNodeGuids = new List<string>();

        /// <summary>
        /// Node currently processed by the tutorial runtime
        /// </summary>
        private string currentNodeGuid = string.Empty;

        /// <summary>
        /// Current lifecycle status of this runtime instance
        /// </summary>
        private ETutorialRuntimeInstanceStatus status = ETutorialRuntimeInstanceStatus.Created;

        #endregion

        #region Properties

        public TutorialGraphAsset SourceGraph => sourceGraph;
        public string TutorialGuid => tutorialGuid;
        public ETutorialReplayPolicy ReplayPolicy => replayPolicy;
        public IReadOnlyDictionary<string, StepSO> RuntimeSteps => runtimeSteps;
        public IReadOnlyDictionary<string, TutorialRuntimeNode> RuntimeNodes => runtimeNodes;
        public IReadOnlyList<string> RootNodeGuids => rootNodeGuids;
        public string CurrentNodeGuid => currentNodeGuid;
        public ETutorialRuntimeInstanceStatus Status => status;
        public bool IsDisposed => status == ETutorialRuntimeInstanceStatus.Disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a runtime tutorial instance from its persistent graph asset
        /// </summary>
        /// <param name="sourceGraph"></param>
        public TutorialRuntimeInstance(TutorialGraphAsset sourceGraph)
        {
            this.sourceGraph = sourceGraph != null ? sourceGraph : throw new ArgumentNullException(nameof(sourceGraph));

            tutorialGuid = sourceGraph.GraphGuid;
            replayPolicy = sourceGraph.ReplayPolicy;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Try to retrieve a runtime StepSO clone
        /// </summary>
        /// <param name="stepGuid"></param>
        /// <param name="runtimeStep"></param>
        /// <returns></returns>
        public bool TryGetRuntimeStep(string stepGuid, out StepSO runtimeStep)
        {
            runtimeStep = null;

            if (IsDisposed || string.IsNullOrWhiteSpace(stepGuid))
            {
                return false;
            }

            return runtimeSteps.TryGetValue(stepGuid, out runtimeStep);
        }

        /// <summary>
        /// Try to retrieve a reconstructed runtime node
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        public bool TryGetRuntimeNode(string nodeGuid, out TutorialRuntimeNode runtimeNode)
        {
            runtimeNode = null;

            if (IsDisposed || string.IsNullOrWhiteSpace(nodeGuid))
            {
                return false;
            }

            return runtimeNodes.TryGetValue(nodeGuid, out runtimeNode);
        }

        /// <summary>
        /// Display the complete reconstructed graph inside the Unity Console
        /// This method is reserved for the dedicated runtime debug button
        /// </summary>
        public void DebugLogRuntimeGraph()
        {
            Debug.Log(BuildDebugReport());
        }

        /// <summary>
        /// Release every runtime clone and graph reference owned by this instance
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            foreach (StepSO runtimeStep in runtimeSteps.Values)
            {
                if (runtimeStep == null)
                {
                    continue;
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

            runtimeSteps.Clear();
            runtimeNodes.Clear();
            rootNodeGuids.Clear();

            currentNodeGuid = string.Empty;
            status = ETutorialRuntimeInstanceStatus.Disposed;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Register a StepSO clone owned by this runtime instance
        /// </summary>
        /// <param name="stepGuid"></param>
        /// <param name="runtimeStep"></param>
        /// <returns></returns>
        internal bool RegisterRuntimeStep(string stepGuid, StepSO runtimeStep)
        {
            if (IsDisposed || string.IsNullOrWhiteSpace(stepGuid) || runtimeStep == null || runtimeSteps.ContainsKey(stepGuid))
            {
                return false;
            }

            runtimeSteps.Add(stepGuid, runtimeStep);

            return true;
        }

        /// <summary>
        /// Register a reconstructed runtime graph node
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        internal bool RegisterRuntimeNode(TutorialRuntimeNode runtimeNode)
        {
            if (IsDisposed || runtimeNode == null || runtimeNodes.ContainsKey(runtimeNode.NodeGuid))
            {
                return false;
            }

            runtimeNodes.Add(runtimeNode.NodeGuid, runtimeNode);

            return true;
        }

        /// <summary>
        /// Define every root node from which an independent tutorial flow can begin
        /// </summary>
        /// <param name="nodeGuids"></param>
        /// <returns></returns>
        internal bool SetRootNodes(IEnumerable<string> nodeGuids)
        {
            if (IsDisposed || nodeGuids == null)
            {
                return false;
            }

            List<string> validatedRootNodeGuids = new List<string>();
            HashSet<string> uniqueRootNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (string nodeGuid in nodeGuids)
            {
                if (string.IsNullOrWhiteSpace(nodeGuid) || !runtimeNodes.ContainsKey(nodeGuid))
                {
                    return false;
                }

                if (!uniqueRootNodeGuids.Add(nodeGuid))
                {
                    return false;
                }

                validatedRootNodeGuids.Add(nodeGuid);
            }

            if (validatedRootNodeGuids.Count == 0)
            {
                return false;
            }

            validatedRootNodeGuids.Sort(StringComparer.Ordinal);

            rootNodeGuids.Clear();
            rootNodeGuids.AddRange(validatedRootNodeGuids);

            return true;
        }

        /// <summary>
        /// Define the node currently processed by the runtime
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        internal bool SetCurrentNode(string nodeGuid)
        {
            if (IsDisposed || string.IsNullOrWhiteSpace(nodeGuid) || !runtimeNodes.ContainsKey(nodeGuid))
            {
                return false;
            }

            currentNodeGuid = nodeGuid;

            return true;
        }

        /// <summary>
        /// Update the lifecycle status of this runtime instance
        /// </summary>
        /// <param name="newStatus"></param>
        internal void SetStatus(ETutorialRuntimeInstanceStatus newStatus)
        {
            if (IsDisposed)
            {
                return;
            }

            status = newStatus;
        }

        #endregion

        #region Debug

        /// <summary>
        /// Build a complete and readable runtime graph report
        /// </summary>
        /// <returns></returns>
        private string BuildDebugReport()
        {
            StringBuilder report = new StringBuilder();

            report.AppendLine("========== TUTORIAL RUNTIME GRAPH ==========");
            report.AppendLine($"Graph: {sourceGraph.name}");
            report.AppendLine($"Tutorial GUID: {tutorialGuid}");
            report.AppendLine($"Replay Policy: {replayPolicy}");
            report.AppendLine($"Runtime Status: {status}");
            report.AppendLine($"Runtime Steps: {runtimeSteps.Count}");
            report.AppendLine($"Runtime Nodes: {runtimeNodes.Count}");
            report.AppendLine($"Root Nodes: {rootNodeGuids.Count}");
            report.AppendLine($"Current Node: {GetDisplayGuid(currentNodeGuid)}");
            report.AppendLine();

            AppendRegisteredSteps(report);
            AppendRuntimeFlow(report);

            report.AppendLine("============================================");

            return report.ToString();
        }

        /// <summary>
        /// Append every registered StepSO clone to the debug report
        /// </summary>
        /// <param name="report"></param>
        private void AppendRegisteredSteps(StringBuilder report)
        {
            report.AppendLine("--- REGISTERED RUNTIME STEPS ---");

            if (runtimeSteps.Count == 0)
            {
                report.AppendLine("No runtime StepSO has been registered.");
                report.AppendLine();

                return;
            }

            List<string> stepGuids = new List<string>(runtimeSteps.Keys);
            stepGuids.Sort(StringComparer.Ordinal);

            for (int i = 0; i < stepGuids.Count; i++)
            {
                string stepGuid = stepGuids[i];
                StepSO runtimeStep = runtimeSteps[stepGuid];
                string stepName = runtimeStep != null ? runtimeStep.name : "<Missing Step>";
                string stepType = runtimeStep != null ? runtimeStep.GetType().Name : "<Missing Type>";

                report.AppendLine($"[{i:000}] {stepGuid} | {stepName} | {stepType}");
            }

            report.AppendLine();
        }

        /// <summary>
        /// Append every reconstructed tutorial flow starting from its root nodes
        /// </summary>
        /// <param name="report"></param>
        private void AppendRuntimeFlow(StringBuilder report)
        {
            report.AppendLine("--- RECONSTRUCTED FLOW ---");

            if (runtimeNodes.Count == 0)
            {
                report.AppendLine("No runtime node has been registered.");
                report.AppendLine();

                return;
            }

            if (rootNodeGuids.Count == 0)
            {
                report.AppendLine("No root node has been defined.");
                AppendUnreachableNodes(report, new HashSet<string>(StringComparer.Ordinal));
                report.AppendLine();

                return;
            }

            HashSet<string> visitedNodes = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < rootNodeGuids.Count; i++)
            {
                string rootNodeGuid = rootNodeGuids[i];
                HashSet<string> currentPath = new HashSet<string>(StringComparer.Ordinal);

                report.AppendLine($"[ROOT {i:000}]");
                AppendNodeFlow(report, rootNodeGuid, 1, visitedNodes, currentPath);

                if (i < rootNodeGuids.Count - 1)
                {
                    report.AppendLine();
                }
            }

            AppendUnreachableNodes(report, visitedNodes);

            report.AppendLine();
        }

        /// <summary>
        /// Traverse and append one runtime graph branch
        /// </summary>
        /// <param name="report"></param>
        /// <param name="nodeGuid"></param>
        /// <param name="depth"></param>
        /// <param name="visitedNodes"></param>
        /// <param name="currentPath"></param>
        private void AppendNodeFlow(StringBuilder report, string nodeGuid, int depth, HashSet<string> visitedNodes, HashSet<string> currentPath)
        {
            string indentation = new string(' ', depth * 4);

            if (depth > MaximumDebugTraversalDepth)
            {
                report.AppendLine($"{indentation}[DEPTH LIMIT] {nodeGuid}");

                return;
            }

            if (currentPath.Contains(nodeGuid))
            {
                report.AppendLine($"{indentation}[CYCLE] {nodeGuid}");

                return;
            }

            if (!runtimeNodes.TryGetValue(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                report.AppendLine($"{indentation}[MISSING NODE] {nodeGuid}");

                return;
            }

            if (visitedNodes.Contains(nodeGuid))
            {
                report.AppendLine($"{indentation}[REFERENCE] {GetNodeDisplayName(runtimeNode)}");

                return;
            }

            visitedNodes.Add(nodeGuid);
            currentPath.Add(nodeGuid);

            string nodeType = runtimeNode.IsSequence ? "SEQUENCE" : "STEP";
            string currentMarker = nodeGuid == currentNodeGuid ? " < CURRENT" : string.Empty;

            report.AppendLine($"{indentation}[{nodeType}] {GetNodeDisplayName(runtimeNode)}{currentMarker}");

            if (runtimeNode.NextNodeGuids.Count == 0)
            {
                report.AppendLine($"{indentation}    └── [END]");
            }
            else
            {
                for (int i = 0; i < runtimeNode.NextNodeGuids.Count; i++)
                {
                    string targetNodeGuid = runtimeNode.NextNodeGuids[i];
                    string branchPrefix = i == runtimeNode.NextNodeGuids.Count - 1 ? "└──" : "├──";

                    report.AppendLine($"{indentation}    {branchPrefix} Transition {i}");
                    AppendNodeFlow(report, targetNodeGuid, depth + 2, visitedNodes, currentPath);
                }
            }

            currentPath.Remove(nodeGuid);
        }

        /// <summary>
        /// Append runtime nodes that cannot be reached from any registered root
        /// </summary>
        /// <param name="report"></param>
        /// <param name="visitedNodes"></param>
        private void AppendUnreachableNodes(StringBuilder report, HashSet<string> visitedNodes)
        {
            List<string> unreachableNodeGuids = new List<string>();

            foreach (string nodeGuid in runtimeNodes.Keys)
            {
                if (!visitedNodes.Contains(nodeGuid))
                {
                    unreachableNodeGuids.Add(nodeGuid);
                }
            }

            if (unreachableNodeGuids.Count == 0)
            {
                return;
            }

            unreachableNodeGuids.Sort(StringComparer.Ordinal);

            report.AppendLine();
            report.AppendLine("--- UNREACHABLE NODES ---");

            foreach (string nodeGuid in unreachableNodeGuids)
            {
                TutorialRuntimeNode runtimeNode = runtimeNodes[nodeGuid];

                report.AppendLine($"[UNREACHABLE] {GetNodeDisplayName(runtimeNode)}");
            }
        }

        /// <summary>
        /// Build the display name of a runtime node
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        private static string GetNodeDisplayName(TutorialRuntimeNode runtimeNode)
        {
            string stepName = runtimeNode.RuntimeStep != null ? runtimeNode.RuntimeStep.name : "<Missing Step>";

            return $"{runtimeNode.NodeGuid} | {runtimeNode.StepGuid} | {stepName}";
        }

        /// <summary>
        /// Return a readable GUID value
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        private static string GetDisplayGuid(string guid)
        {
            return string.IsNullOrWhiteSpace(guid) ? "<None>" : guid;
        }

        #endregion
    }
}