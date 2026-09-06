using Tutorial.Runtime;
using Tutorial.Runtime.Catalogue;
using Tutorial.Runtime.Core;
using Tutorial.Runtime.Flow;
using Tutorial.Runtime.Persistence;
using UnityEditor;
using UnityEngine;

namespace Tutorial.Editor.Flow
{
    /// <summary>
    /// Provide persistent tutorial runtime testing controls directly from the TutorialFlowController Inspector
    /// </summary>
    [CustomEditor(typeof(TutorialFlowController))]
    public sealed class TutorialFlowControllerEditor : UnityEditor.Editor
    {
        #region Private Fields

        /// <summary>
        /// Tutorial FlowController currently inspected by this custom editor
        /// </summary>
        private TutorialFlowController flowController = null;

        #endregion

        #region Unity Editor Callbacks

        private void OnEnable()
        {
            flowController = target as TutorialFlowController;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            DrawRuntimeTesting();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Draw every tutorial runtime testing control
        /// </summary>
        private void DrawRuntimeTesting()
        {
            EditorGUILayout.LabelField("Runtime Testing", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to start and control tutorial graphs.", MessageType.Info);

                return;
            }

            if (flowController == null)
            {
                EditorGUILayout.HelpBox("TutorialFlowController is not available.", MessageType.Error);

                return;
            }

            DrawCurrentRuntime();
            DrawRuntimeControls();
            DrawCatalogue();
            DrawRuntimeGraphDebug(flowController);
        }

        /// <summary>
        /// Draw information about the tutorial runtime currently controlled by the FlowController
        /// </summary>
        private void DrawCurrentRuntime()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Current Runtime", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tutorials Enabled", flowController.TutorialsEnabled.ToString());

            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            if (runtimeInstance == null)
            {
                EditorGUILayout.LabelField("Graph", "<None>");
                EditorGUILayout.LabelField("Status", "Idle");

                return;
            }

            string graphName = runtimeInstance.SourceGraph != null ? runtimeInstance.SourceGraph.name : "<None>";

            EditorGUILayout.LabelField("Graph", graphName);
            EditorGUILayout.LabelField("Runtime Status", runtimeInstance.Status.ToString());

            if (flowController.Runner != null)
            {
                EditorGUILayout.LabelField("Runner Status", flowController.Runner.Status.ToString());
            }

            if (flowController.Progress != null)
            {
                EditorGUILayout.LabelField("Progress", flowController.Progress.Status.ToString());
            }
        }

        /// <summary>
        /// Draw controls affecting the currently active tutorial runtime
        /// </summary>
        private void DrawRuntimeControls()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            string tutorialsToggleLabel = flowController.TutorialsEnabled ? "Disable Tutorials" : "Enable Tutorials";

            if (GUILayout.Button(tutorialsToggleLabel))
            {
                TutoEventBus.Publish<OnTutorialsEnabledChanged>(new OnTutorialsEnabledChanged(!flowController.TutorialsEnabled));
            }

            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            EditorGUI.BeginDisabledGroup(runtimeInstance == null);

            if (GUILayout.Button("Restart Current Graph"))
            {
                TryRestartCurrentGraph();
            }

            if (GUILayout.Button("Reset Current Graph Progress"))
            {
                TryResetCurrentGraphProgress();
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Reset All Tutorial Progress"))
            {
                TryResetAllTutorialProgress();
            }

            bool canResume =
                flowController.Runner != null &&
                flowController.Runner.Status == ETutorialRunnerStatus.WaitingForDependencies;

            EditorGUI.BeginDisabledGroup(!canResume);

            if (GUILayout.Button("Resume Waiting Tutorial"))
            {
                if (!flowController.TryResumeWaitingTutorial())
                {
                    Debug.LogWarning("Tutorial runtime could not resume.", flowController);
                }
            }

            EditorGUI.EndDisabledGroup();

            bool canSkipCurrentStep = flowController.Runner != null && !flowController.Runner.IsTerminal;

            EditorGUI.BeginDisabledGroup(!canSkipCurrentStep);

            if (GUILayout.Button("Skip Current Step"))
            {
                if (!flowController.Runner.SkipCurrentStep())
                {
                    Debug.LogWarning("The current tutorial Step could not be skipped.", flowController);
                }
            }

            EditorGUI.EndDisabledGroup();

            bool canReplay =
                runtimeInstance != null &&
                runtimeInstance.SourceGraph != null &&
                flowController.Progress != null &&
                flowController.Progress.IsCompleted &&
                runtimeInstance.SourceGraph.ReplayPolicy == ETutorialReplayPolicy.Allowed &&
                !flowController.IsReplaying;

            EditorGUI.BeginDisabledGroup(!canReplay);

            if (GUILayout.Button("Replay Current Graph"))
            {
                TryReplayCurrentGraph();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!flowController.IsReplaying);

            if (GUILayout.Button("Cancel Replay"))
            {
                flowController.CancelReplay();
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Draw one runtime start control for every graph contained inside the TutorialRuntimeCatalogue
        /// </summary>
        private void DrawCatalogue()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Runtime Catalogue", EditorStyles.boldLabel);

            TutorialRuntimeCatalogue catalogue = flowController.RuntimeCatalogue;

            if (catalogue == null)
            {
                EditorGUILayout.HelpBox("No TutorialRuntimeCatalogue is assigned to the TutorialFlowController.", MessageType.Warning);

                return;
            }

            if (catalogue.Graphs == null || catalogue.Graphs.Count == 0)
            {
                EditorGUILayout.HelpBox("The runtime catalogue contains no tutorial graph.", MessageType.Info);

                return;
            }

            foreach (TutorialRuntimeGraphEntry graphEntry in catalogue.Graphs)
            {
                if (graphEntry == null || graphEntry.Graph == null)
                {
                    continue;
                }

                DrawGraphEntry(graphEntry);
            }
        }

        /// <summary>
        /// Draw one tutorial graph catalogue entry and its runtime start button
        /// </summary>
        /// <param name="graphEntry"></param>
        private void DrawGraphEntry(TutorialRuntimeGraphEntry graphEntry)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(graphEntry.Graph, typeof(TutorialGraphAsset), false);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Start", GUILayout.Width(80f)))
            {
                TryStartGraph(graphEntry.Graph);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Start one tutorial graph from a fresh progress state
        /// </summary>
        /// <param name="graph"></param>
        private void TryStartGraph(TutorialGraphAsset graph)
        {
            if (graph == null)
            {
                Debug.LogError("Cannot start a null TutorialGraphAsset.", flowController);

                return;
            }

            if (!flowController.TryStartTutorial(graph, null, out string error))
            {
                Debug.LogError(error, flowController);

                return;
            }

            Debug.Log($"Tutorial graph '{graph.name}' started.", flowController);
        }

        /// <summary>
        /// Reset persistent progress and explicitly restart the currently controlled tutorial graph from zero
        /// </summary>
        private void TryRestartCurrentGraph()
        {
            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            if (runtimeInstance == null || runtimeInstance.SourceGraph == null)
            {
                Debug.LogError("No tutorial graph is currently available to restart.", flowController);

                return;
            }

            TutorialGraphAsset sourceGraph = runtimeInstance.SourceGraph;

            if (!flowController.ResetTutorialProgress(sourceGraph, out string error))
            {
                Debug.LogError(error, flowController);

                return;
            }

            if (!flowController.TryStartTutorial(sourceGraph, null, out error))
            {
                Debug.LogError(error, flowController);

                return;
            }

            Debug.Log($"Tutorial graph '{sourceGraph.name}' persistent progress reset and graph restarted.", flowController);
        }

        /// <summary>
        /// Reset persistent progress of the currently controlled tutorial graph without restarting it
        /// </summary>
        private void TryResetCurrentGraphProgress()
        {
            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            if (runtimeInstance == null || runtimeInstance.SourceGraph == null)
            {
                Debug.LogError("No tutorial graph is currently available to reset.", flowController);

                return;
            }

            TutorialGraphAsset sourceGraph = runtimeInstance.SourceGraph;

            if (!flowController.ResetTutorialProgress(sourceGraph, out string error))
            {
                Debug.LogError(error, flowController);

                return;
            }

            Debug.Log($"Tutorial graph '{sourceGraph.name}' persistent progress reset.", flowController);
        }

        /// <summary>
        /// Reset every persistent tutorial progress without starting a tutorial
        /// </summary>
        private void TryResetAllTutorialProgress()
        {
            if (!flowController.ResetAllTutorialProgress(out string error))
            {
                Debug.LogError(error, flowController);

                return;
            }

            Debug.Log("Every tutorial persistent progress has been reset.", flowController);
        }

        /// <summary>
        /// Start a temporary replay of the currently completed tutorial graph
        /// </summary>
        private void TryReplayCurrentGraph()
        {
            TutorialRuntimeInstance runtimeInstance = flowController.RuntimeInstance;

            if (runtimeInstance == null || runtimeInstance.SourceGraph == null || flowController.Progress == null)
            {
                Debug.LogError("No completed tutorial graph is currently available for replay.", flowController);

                return;
            }

            TutorialProgressSaveData persistentProgress = flowController.Progress.CreateSaveData();

            if (!flowController.TryStartReplay(runtimeInstance.SourceGraph, persistentProgress, out string error))
            {
                Debug.LogError(error, flowController);

                return;
            }

            Debug.Log($"Tutorial graph '{runtimeInstance.SourceGraph.name}' replay started.", flowController);
        }

        /// <summary>
        /// Draw the button used to print the complete reconstructed runtime graph
        /// </summary>
        /// <param name="flowController"></param>
        private static void DrawRuntimeGraphDebug(TutorialFlowController flowController)
        {
            bool canLogGraph = Application.isPlaying && flowController != null && flowController.RuntimeInstance != null && !flowController.RuntimeInstance.IsDisposed;

            EditorGUI.BeginDisabledGroup(!canLogGraph);

            if (GUILayout.Button("Log Runtime Graph"))
            {
                flowController.RuntimeInstance.DebugLogRuntimeGraph();
            }

            EditorGUI.EndDisabledGroup();
        }

        #endregion
    }
}