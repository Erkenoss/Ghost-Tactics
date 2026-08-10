using Tutorial.Runtime.Core;
using Tutorial.Runtime.Flow;
using Tutorial.Runtime.Persistence;
using Tutorial.Runtime.Progress;
using UnityEngine;

namespace Tutorial.Runtime.Debugging
{
    /// <summary>
    /// Display the current tutorial runtime state for diagnostic purposes
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TutorialFlowController))]
    public sealed class TutorialRuntimeOverlay : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>
        /// Whether the runtime diagnostic overlay must be visible when entering Play Mode
        /// </summary>
        [Tooltip("Show the tutorial runtime diagnostic overlay when entering Play Mode")]
        [SerializeField]
        private bool showOnStart = true;

        /// <summary>
        /// Whether every runtime node must be displayed inside the diagnostic overlay
        /// </summary>
        [Tooltip("Display detailed information for every runtime tutorial node")]
        [SerializeField]
        private bool showNodeDetails = true;

        /// <summary>
        /// Maximum number of runtime nodes displayed inside the diagnostic overlay
        /// </summary>
        [Tooltip("Maximum number of runtime nodes displayed inside the diagnostic overlay")]
        [SerializeField]
        private int maxDisplayedNodes = 20;

        /// <summary>
        /// Position of the diagnostic overlay on screen
        /// </summary>
        [Tooltip("Screen position of the tutorial runtime diagnostic overlay")]
        [SerializeField]
        private Vector2 overlayPosition = new Vector2(10f, 10f);

        /// <summary>
        /// Width of the diagnostic overlay
        /// </summary>
        [Tooltip("Width of the tutorial runtime diagnostic overlay")]
        [SerializeField]
        private float overlayWidth = 500f;

        #endregion

        #region Private Fields

        /// <summary>
        /// Tutorial FlowController observed by this diagnostic overlay
        /// </summary>
        private TutorialFlowController flowController = null;

        /// <summary>
        /// Whether the diagnostic overlay is currently visible
        /// </summary>
        private bool isVisible = false;

        /// <summary>
        /// Current scroll position used by the runtime node detail area
        /// </summary>
        private Vector2 scrollPosition = Vector2.zero;

        #endregion

        #region Properties

        public bool IsVisible => isVisible;

        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Resolve the TutorialFlowController attached to this GameObject
        /// </summary>
        private void Awake()
        {
            flowController = GetComponent<TutorialFlowController>();
            isVisible = showOnStart;
        }

        /// <summary>
        /// Validate serialized diagnostic values
        /// </summary>
        private void OnValidate()
        {
            maxDisplayedNodes = Mathf.Max(1, maxDisplayedNodes);
            overlayWidth = Mathf.Max(200f, overlayWidth);
        }

        /// <summary>
        /// Draw the runtime tutorial diagnostic overlay
        /// </summary>
        private void OnGUI()
        {
            if (!isVisible || flowController == null)
            {
                return;
            }

            float availableHeight = Mathf.Max(200f, Screen.height - overlayPosition.y - 10f);
            Rect overlayRect = new Rect(overlayPosition.x, overlayPosition.y, overlayWidth, availableHeight);

            GUILayout.BeginArea(overlayRect, GUI.skin.box);

            DrawHeader();
            DrawRuntimeState();

            if (showNodeDetails)
            {
                DrawRuntimeNodes();
            }

            GUILayout.EndArea();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the runtime tutorial diagnostic overlay
        /// </summary>
        public void Show()
        {
            isVisible = true;
        }

        /// <summary>
        /// Hide the runtime tutorial diagnostic overlay
        /// </summary>
        public void Hide()
        {
            isVisible = false;
        }

        /// <summary>
        /// Toggle the runtime tutorial diagnostic overlay visibility
        /// </summary>
        public void Toggle()
        {
            isVisible = !isVisible;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draw the diagnostic overlay title and general controller state
        /// </summary>
        private void DrawHeader()
        {
            GUILayout.Label("TUTORIAL RUNTIME");
            GUILayout.Label($"Running: {flowController.IsRunning}");
            GUILayout.Label($"Replay: {flowController.IsReplaying}");
            GUILayout.Space(5f);
        }

        /// <summary>
        /// Draw the current runtime instance, runner and progress states
        /// </summary>
        private void DrawRuntimeState()
        {
            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            if (runtimeInstance == null)
            {
                GUILayout.Label("Runtime Instance: <None>");

                return;
            }

            string graphName = runtimeInstance.SourceGraph != null ? runtimeInstance.SourceGraph.name : "<None>";

            GUILayout.Label($"Graph: {graphName}");
            GUILayout.Label($"Tutorial GUID: {runtimeInstance.TutorialGuid}");
            GUILayout.Label($"Runtime Status: {runtimeInstance.Status}");
            GUILayout.Label($"Runtime Nodes: {runtimeInstance.RuntimeNodes.Count}");
            GUILayout.Label($"Runtime Steps: {runtimeInstance.RuntimeSteps.Count}");
            GUILayout.Label($"Root Nodes: {runtimeInstance.RootNodeGuids.Count}");

            if (flowController.Runner != null)
            {
                GUILayout.Space(5f);
                GUILayout.Label($"Runner Status: {flowController.Runner.Status}");

                if (!string.IsNullOrWhiteSpace(flowController.Runner.LastError))
                {
                    GUILayout.Label($"Runner Error: {flowController.Runner.LastError}");
                }
            }

            DrawProgressState();

            GUILayout.Space(10f);
        }

        /// <summary>
        /// Draw the current tutorial progress snapshot
        /// </summary>
        private void DrawProgressState()
        {
            TutorialProgressService progress = flowController.Progress;

            if (progress == null)
            {
                return;
            }

            TutorialProgressSaveData saveData = progress.CreateSaveData();

            GUILayout.Space(5f);
            GUILayout.Label($"Progress: {progress.Status}");

            if (saveData != null)
            {
                int completedCount = saveData.CompletedNodeGuids != null ? saveData.CompletedNodeGuids.Count : 0;
                int skippedCount = saveData.SkippedNodeGuids != null ? saveData.SkippedNodeGuids.Count : 0;

                GUILayout.Label($"Completed Nodes: {completedCount}");
                GUILayout.Label($"Skipped Nodes: {skippedCount}");
            }
        }

        /// <summary>
        /// Draw runtime node information and progression state
        /// </summary>
        private void DrawRuntimeNodes()
        {
            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            if (runtimeInstance == null)
            {
                return;
            }

            GUILayout.Label("NODES");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            int displayedNodeCount = 0;

            foreach (TutorialRuntimeNode runtimeNode in runtimeInstance.RuntimeNodes.Values)
            {
                if (runtimeNode == null)
                {
                    continue;
                }

                if (displayedNodeCount >= maxDisplayedNodes)
                {
                    GUILayout.Label($"... {runtimeInstance.RuntimeNodes.Count - displayedNodeCount} more node(s)");

                    break;
                }

                DrawRuntimeNode(runtimeNode);
                displayedNodeCount++;
            }

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Draw diagnostic information associated with one runtime tutorial node
        /// </summary>
        /// <param name="runtimeNode"></param>
        private void DrawRuntimeNode(TutorialRuntimeNode runtimeNode)
        {
            string stepName = runtimeNode.RuntimeStep != null ? runtimeNode.RuntimeStep.name : "<Null Step>";
            string nodeType = runtimeNode.IsSequence ? "Sequence" : "Step";
            string progressState = GetNodeProgressState(runtimeNode.NodeGuid);

            GUILayout.Label($"[{nodeType}] {stepName}");
            GUILayout.Label($"    Node: {runtimeNode.NodeGuid}");
            GUILayout.Label($"    Progress: {progressState}");
            GUILayout.Label($"    Next: {runtimeNode.NextNodeGuids.Count}");
        }

        /// <summary>
        /// Resolve the current progress state associated with one runtime node
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        private string GetNodeProgressState(string nodeGuid)
        {
            TutorialProgressService progress = flowController.Progress;

            if (progress == null)
            {
                return "Unavailable";
            }

            if (progress.IsNodeCompleted(nodeGuid))
            {
                return "Completed";
            }

            if (progress.IsNodeSkipped(nodeGuid))
            {
                return "Skipped";
            }

            return "Pending / Active";
        }

        #endregion
    }
}