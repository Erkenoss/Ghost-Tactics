using Crimson.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

using Tutorial.Runtime.Catalogue;
using Tutorial.Runtime.Components;
using Tutorial.Runtime.Core;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Execution;
using Tutorial.Runtime.Hooks;
using Tutorial.Runtime.Persistence;
using Tutorial.Runtime.Progress;
using Tutorial.Runtime.Replay;
using Tutorial.Runtime.Resolution;
using Tutorial.Runtime.Completion.UI;
using Tutorial.Runtime.Data.Completion.UI;

namespace Tutorial.Runtime.Flow
{
    /// <summary>
    /// Event used to globally enable or disable tutorial execution
    /// </summary>
    public sealed class OnTutorialsEnabledChanged
    {
        #region Private Fields

        /// <summary>
        /// Determine whether tutorials are globally allowed to execute
        /// </summary>
        private readonly bool enabled = true;

        #endregion

        #region Properties

        public bool Enabled => enabled;

        #endregion

        #region Constructor

        /// <summary>
        /// Create one tutorial enabled-state event
        /// </summary>
        /// <param name="enabled"></param>
        public OnTutorialsEnabledChanged(bool enabled)
        {
            this.enabled = enabled;
        }

        #endregion
    }

    /// <summary>
    /// Coordinate the complete runtime lifecycle of one tutorial graph
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialFlowController : Singleton<TutorialFlowController>
    {
        #region Serialized Fields

        /// <summary>
        /// Runtime catalogue containing every tutorial graph and StepSO reference available at runtime
        /// </summary>
        [Tooltip("Runtime catalogue containing every tutorial graph available to the FlowController")]
        [SerializeField]
        private TutorialRuntimeCatalogue runtimeCatalogue = null;

        #endregion

        #region Private Fields

        /// <summary>
        /// Repository responsible for physical tutorial progress persistence
        /// </summary>
        private TutorialProgressRepository progressRepository = null;

        /// <summary>
        /// Service responsible for reconstructing tutorial runtime graphs
        /// </summary>
        private readonly TutorialRuntimeBuilder runtimeBuilder = new TutorialRuntimeBuilder();

        /// <summary>
        /// Registry owning every successfully created tutorial runtime instance
        /// </summary>
        private readonly TutorialRuntimeRegistry runtimeRegistry = new TutorialRuntimeRegistry();

        /// <summary>
        /// Registry containing tutorial identifiers currently available at runtime
        /// </summary>
        private readonly TutorialIdentifierRegistry identifierRegistry = new TutorialIdentifierRegistry();

        /// <summary>
        /// Service responsible for resolving StepSO method bindings
        /// </summary>
        private TutorialMethodResolver methodResolver = null;

        /// <summary>
        /// Service responsible for managing temporary replay sessions
        /// </summary>
        private TutorialReplayService replayService = null;

        /// <summary>
        /// Runtime tutorial instance currently controlled
        /// </summary>
        private TutorialRuntimeInstance runtimeInstance = null;

        /// <summary>
        /// Runtime FSM currently executing the tutorial
        /// </summary>
        private TutorialRunner tutorialRunner = null;

        /// <summary>
        /// Progress service associated with the current normal or replay execution
        /// </summary>
        private TutorialProgressService progressService = null;

        /// <summary>
        /// Determine whether normal tutorial execution is globally allowed
        /// </summary>
        private bool tutorialsEnabled = true;


        #endregion

        #region Properties

        public TutorialRuntimeCatalogue RuntimeCatalogue => runtimeCatalogue;
        public TutorialRuntimeInstance RuntimeInstance => runtimeInstance;
        public TutorialRunner Runner => tutorialRunner;
        public TutorialProgressService Progress => progressService;
        public TutorialIdentifierRegistry IdentifierRegistry => identifierRegistry;
        public bool TutorialsEnabled => tutorialsEnabled;
        public bool IsRunning => tutorialRunner != null && !tutorialRunner.IsTerminal;
        public bool IsReplaying => replayService != null && replayService.IsReplaying;

        #endregion

        #region Events

        public event Action<TutorialFlowController> TutorialStarted = null;
        public event Action<TutorialFlowController> TutorialCompleted = null;
        public event Action<TutorialFlowController, string> TutorialFailed = null;
        public event Action<TutorialFlowController, TutorialProgressSaveData> PersistentProgressChanged = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            methodResolver = new TutorialMethodResolver(identifierRegistry);
            replayService = new TutorialReplayService();
            progressRepository = new TutorialProgressRepository(Application.persistentDataPath);

            SubscribeIdentifierLifecycle();
            RegisterLoadedIdentifiers();
        }

        private void OnEnable()
        {
            TutorialMethodNotifier.Triggered += OnTutorialMethodTriggered;
            TutorialMethodNotifier.SkipRequested += OnTutorialSkipRequested;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            TutoEventBus.Subscribe<OnTutorialsEnabledChanged>(OnTutorialsEnabledStateChanged);
        }

        /// <summary>
        /// Start the tutorial graph associated with the initial active scene
        /// </summary>
        private void Start()
        {
            TryStartSceneTutorial(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            TutorialMethodNotifier.Triggered -= OnTutorialMethodTriggered;
            TutorialMethodNotifier.SkipRequested -= OnTutorialSkipRequested;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            TutoEventBus.Unsubscribe<OnTutorialsEnabledChanged>(OnTutorialsEnabledStateChanged);
        }

        /// <summary>
        /// Apply the global tutorial execution state received through the TutoEventBus
        /// </summary>
        /// <param name="tutorialState"></param>
        private void OnTutorialsEnabledStateChanged(OnTutorialsEnabledChanged tutorialState)
        {
            if (tutorialState == null || tutorialsEnabled == tutorialState.Enabled)
            {
                return;
            }

            tutorialsEnabled = tutorialState.Enabled;

            if (tutorialsEnabled)
            {
                return;
            }

            ReleaseCurrentExecution();
        }

        /// <summary>
        /// Forward an injected tutorial Step trigger to the currently executing tutorial runner
        /// </summary>
        /// <param name="stepGUID"></param>
        private void OnTutorialMethodTriggered(string stepGUID)
        {
            if (!tutorialsEnabled || tutorialRunner == null || tutorialRunner.IsTerminal)
            {
                return;
            }

            tutorialRunner.TryTriggerStep(stepGUID);
        }

        /// <summary>
        /// Release every runtime tutorial service owned by this controller
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeIdentifierLifecycle();

            ReleaseCurrentExecution();

            replayService?.Dispose();
            replayService = null;
            progressRepository = null;

            runtimeRegistry.Dispose();
            identifierRegistry.Clear();

            TutorialStarted = null;
            TutorialCompleted = null;
            TutorialFailed = null;
            PersistentProgressChanged = null;
        }

        #endregion

        #region Public Tutorial Methods

        /// <summary>
        /// Unregister one TutoIdentifier that is no longer available at runtime
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool TryUnregisterIdentifier(TutoIdentifier identifier)
        {
            return identifierRegistry.TryUnregister(identifier);
        }

        /// <summary>
        /// Delete the persistent progress of one tutorial graph
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <returns></returns>
        public bool ResetTutorialProgress(TutorialGraphAsset sourceGraph)
        {
            return ResetTutorialProgress(sourceGraph, out _);
        }

        /// <summary>
        /// Delete the persistent progress of one tutorial graph without starting a new execution
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool ResetTutorialProgress(TutorialGraphAsset sourceGraph, out string error)
        {
            error = string.Empty;

            if (sourceGraph == null)
            {
                error = "Cannot reset persistent progress for a null TutorialGraphAsset.";

                return false;
            }

            if (progressRepository == null)
            {
                error = "Tutorial progress repository is not initialized.";

                return false;
            }

            bool isCurrentGraph = runtimeInstance != null && runtimeInstance.SourceGraph == sourceGraph;

            if (isCurrentGraph)
            {
                ReleaseCurrentExecution();
            }

            return progressRepository.TryDelete(sourceGraph.GraphGuid, out error);
        }

        /// <summary>
        /// Delete every persistent tutorial progress file
        /// </summary>
        /// <returns></returns>
        public bool ResetAllTutorialProgress()
        {
            return ResetAllTutorialProgress(out _);
        }

        /// <summary>
        /// Delete every persistent tutorial progress file without starting a new execution
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool ResetAllTutorialProgress(out string error)
        {
            error = string.Empty;

            if (progressRepository == null)
            {
                error = "Tutorial progress repository is not initialized.";

                return false;
            }

            ReleaseCurrentExecution();

            return progressRepository.TryDeleteAll(out error);
        }

        /// <summary>
        /// Build and start one tutorial from its TutorialGraphAsset
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="savedProgress"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryStartTutorial(TutorialGraphAsset sourceGraph, TutorialProgressSaveData savedProgress, out string error)
        {
            error = string.Empty;

            if (!tutorialsEnabled)
            {
                error = "Tutorial execution is disabled.";

                return false;
            }

            ReleaseCurrentExecution();

            if (!TryBuildRuntime(sourceGraph, out error))
            {
                return false;
            }

            progressService = new TutorialProgressService(runtimeInstance);

            if (savedProgress == null)
            {
                if (!progressService.Start())
                {
                    error = $"Tutorial '{runtimeInstance.TutorialGuid}' progress could not be started.";

                    ReleaseCurrentExecution();

                    return false;
                }
            }
            else
            {
                if (!progressService.TryRestore(savedProgress, out error))
                {
                    ReleaseCurrentExecution();

                    return false;
                }

                if (progressService.IsCompleted)
                {
                    ReleaseCurrentExecution();

                    return true;
                }

                if (!progressService.IsStarted && !progressService.Start())
                {
                    error = $"Tutorial '{runtimeInstance.TutorialGuid}' restored progress could not be started.";

                    ReleaseCurrentExecution();

                    return false;
                }
            }

            return TryStartRunner(out error);
        }

        /// <summary>
        /// Build and start a temporary replay while preserving completed persistent progress
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="persistentProgress"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryStartReplay(TutorialGraphAsset sourceGraph, TutorialProgressSaveData persistentProgress, out string error)
        {
            error = string.Empty;

            ReleaseCurrentExecution();

            if (!TryBuildRuntime(sourceGraph, out error))
            {
                return false;
            }

            if (!replayService.TryBeginReplay(runtimeInstance, persistentProgress, out error))
            {
                ReleaseCurrentExecution();

                return false;
            }

            progressService = replayService.ReplayProgress;

            return TryStartRunner(out error);
        }

        /// <summary>
        /// Cancel the currently active tutorial replay
        /// </summary>
        /// <returns></returns>
        public bool CancelReplay()
        {
            if (!IsReplaying)
            {
                return false;
            }

            ReleaseRunner();

            bool cancelled = replayService.CancelReplay();

            progressService = null;

            ReleaseRuntimeInstance();

            return cancelled;
        }

        /// <summary>
        /// Retry runtime nodes currently waiting for unavailable scene dependencies
        /// </summary>
        /// <returns></returns>
        public bool TryResumeWaitingTutorial()
        {
            if (tutorialRunner == null || tutorialRunner.IsTerminal)
            {
                return false;
            }

            return tutorialRunner.TryResume();
        }

        #endregion

        #region Identifier Registration

        /// <summary>
        /// Register one currently available TutoIdentifier and retry pending tutorial dependencies
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryRegisterIdentifier(TutoIdentifier identifier, out string error)
        {
            if (!identifierRegistry.TryRegister(identifier, out error))
            {
                return false;
            }

            tutorialRunner?.TryResume();

            return true;
        }

        #endregion

        #region Debug

        /// <summary>
        /// Display the current runtime binding state of every tutorial node
        /// </summary>
        public void DebugLogRuntimeBindings()
        {
            if (runtimeInstance == null || runtimeInstance.IsDisposed)
            {
                Debug.LogWarning("No active tutorial runtime instance is available for binding diagnostics.", this);

                return;
            }

            StringBuilder report = new StringBuilder();

            report.AppendLine("========== TUTORIAL RUNTIME BINDINGS ==========");

            List<TutorialRuntimeNode> runtimeNodes = new List<TutorialRuntimeNode>(runtimeInstance.RuntimeNodes.Values);

            runtimeNodes.Sort((first, second) =>
            {
                string firstGuid = first != null ? first.NodeGuid : string.Empty;
                string secondGuid = second != null ? second.NodeGuid : string.Empty;

                return string.Compare(firstGuid, secondGuid, StringComparison.Ordinal);
            });

            foreach (TutorialRuntimeNode runtimeNode in runtimeNodes)
            {
                AppendRuntimeBindingDebug(report, runtimeNode);
            }

            report.AppendLine("================================================");

            Debug.Log(report.ToString(), this);
        }

        /// <summary>
        /// Append the runtime binding state of one tutorial node
        /// </summary>
        /// <param name="report"></param>
        /// <param name="runtimeNode"></param>
        private void AppendRuntimeBindingDebug(StringBuilder report, TutorialRuntimeNode runtimeNode)
        {
            if (runtimeNode == null)
            {
                report.AppendLine("[NODE] <NULL>");
                report.AppendLine("    Binding Status: <INVALID>");
                report.AppendLine("    Binding Error: The runtime node is null.");
                report.AppendLine();

                return;
            }

            StepSO runtimeStep = runtimeNode.RuntimeStep;
            string nodeType = runtimeNode.IsSequence ? "SEQUENCE" : "STEP";
            string stepName = runtimeStep != null ? runtimeStep.name : "<NULL>";

            report.AppendLine($"[{nodeType}] {stepName}");
            report.AppendLine($"    Node GUID: {runtimeNode.NodeGuid}");
            report.AppendLine($"    Step GUID: {runtimeNode.StepGuid}");

            if (runtimeStep == null)
            {
                report.AppendLine("    Binding Status: <INVALID>");
                report.AppendLine("    Binding Error: The runtime node contains no RuntimeStep.");
                report.AppendLine();

                return;
            }

            bool hasTutoGuid = !string.IsNullOrWhiteSpace(runtimeStep.TutoGUID);
            bool hasScriptName = !string.IsNullOrWhiteSpace(runtimeStep.ScriptName);
            bool hasMethodName = !string.IsNullOrWhiteSpace(runtimeStep.MethodNameToCall);
            bool hasAnyBindingData = hasTutoGuid || hasScriptName || hasMethodName;
            bool hasCompleteBinding = hasTutoGuid && hasScriptName && hasMethodName;

            if (!hasAnyBindingData)
            {
                report.AppendLine("    Binding Status: <NONE>");
                report.AppendLine();

                return;
            }

            report.AppendLine($"    Tuto GUID: {runtimeStep.TutoGUID}");
            report.AppendLine($"    Expected Script: {runtimeStep.ScriptName}");
            report.AppendLine($"    Expected Method: {runtimeStep.MethodNameToCall}()");

            if (!hasCompleteBinding)
            {
                report.AppendLine("    Binding Status: <INVALID>");
                report.AppendLine($"    Binding Error: {BuildMissingBindingFields(runtimeStep)}");
                report.AppendLine();

                return;
            }

            if (methodResolver == null)
            {
                report.AppendLine("    Binding Status: <UNRESOLVED>");
                report.AppendLine("    Resolution Error: TutorialMethodResolver is not initialized.");
                report.AppendLine();

                return;
            }

            if (!methodResolver.TryResolve(runtimeStep, out TutorialResolvedMethod resolvedMethod, out string error))
            {
                report.AppendLine("    Binding Status: <UNRESOLVED>");
                report.AppendLine($"    Resolution Error: {error}");
                report.AppendLine();

                return;
            }

            GameObject targetGameObject = resolvedMethod.Identifier != null ? resolvedMethod.Identifier.gameObject : null;
            string sceneName = targetGameObject != null && targetGameObject.scene.IsValid() ? targetGameObject.scene.name : "<NONE>";
            string scriptName = resolvedMethod.Script != null ? resolvedMethod.Script.GetType().FullName : "<NONE>";
            string methodName = resolvedMethod.Method != null ? resolvedMethod.Method.Name : "<NONE>";

            report.AppendLine("    Binding Status: <RESOLVED>");
            report.AppendLine($"    GameObject: {(targetGameObject != null ? targetGameObject.name : "<NONE>")}");
            report.AppendLine($"    Scene: {sceneName}");
            report.AppendLine($"    Identifier GUID: {resolvedMethod.Identifier?.ObjectGUID ?? "<NONE>"}");
            report.AppendLine($"    Script: {scriptName}");
            report.AppendLine($"    Binding: {scriptName}.{methodName}()");
            report.AppendLine();
        }

        /// <summary>
        /// Build the list of missing fields from one partially configured tutorial binding
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <returns></returns>
        private static string BuildMissingBindingFields(StepSO runtimeStep)
        {
            List<string> missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(runtimeStep.TutoGUID))
            {
                missingFields.Add(nameof(runtimeStep.TutoGUID));
            }

            if (string.IsNullOrWhiteSpace(runtimeStep.ScriptName))
            {
                missingFields.Add(nameof(runtimeStep.ScriptName));
            }

            if (string.IsNullOrWhiteSpace(runtimeStep.MethodNameToCall))
            {
                missingFields.Add(nameof(runtimeStep.MethodNameToCall));
            }

            return $"The binding is incomplete. Missing: {string.Join(", ", missingFields)}.";
        }


        #endregion

        #region Runtime Creation

        /// <summary>
        /// Resolve runtime StepSO references from the source graph, build the tutorial runtime instance and register it
        /// </summary>
        /// <param name="sourceGraph"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryBuildRuntime(TutorialGraphAsset sourceGraph, out string error)
        {
            error = string.Empty;

            if (sourceGraph == null)
            {
                error = "The TutorialGraphAsset used to build the tutorial runtime is null.";

                return false;
            }

            if (runtimeCatalogue == null)
            {
                error = $"TutorialFlowController '{name}' has no TutorialRuntimeCatalogue assigned.";

                return false;
            }

            if (!runtimeCatalogue.TryGetGraphEntry(sourceGraph.GraphGuid, out TutorialRuntimeGraphEntry graphEntry))
            {
                error = $"TutorialGraphAsset '{sourceGraph.name}' is not registered inside runtime catalogue '{runtimeCatalogue.name}'.";

                return false;
            }

            if (graphEntry.Graph != sourceGraph)
            {
                error = $"Runtime catalogue graph GUID '{sourceGraph.GraphGuid}' references another TutorialGraphAsset than '{sourceGraph.name}'.";

                return false;
            }

            if (!sourceGraph.TryResolveSourceNodes(out Dictionary<string, StepSO> sourceNodes, out error))
            {
                return false;
            }

            if (!runtimeBuilder.TryBuild(sourceGraph, sourceNodes, out TutorialRuntimeInstance builtInstance))
            {
                error = $"Tutorial runtime graph '{sourceGraph.name}' could not be built.";

                return false;
            }

            if (!runtimeRegistry.TryRegister(builtInstance))
            {
                builtInstance.Dispose();

                error = $"Tutorial runtime '{builtInstance.TutorialGuid}' could not be registered.";

                return false;
            }

            runtimeInstance = builtInstance;

            return true;
        }

        /// <summary>
        /// Try to start the tutorial graph associated with a newly loaded scene
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="loadSceneMode"></param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            TryStartSceneTutorial(scene);
        }

        /// <summary>
        /// Release the current tutorial execution when its owning scene is unloaded
        /// </summary>
        /// <param name="scene"></param>
        private void OnSceneUnloaded(Scene scene)
        {
            if (runtimeInstance == null || runtimeInstance.SourceGraph == null || runtimeCatalogue == null)
            {
                return;
            }

            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                return;
            }

            if (!runtimeCatalogue.TryGetGraphEntryByScenePath(scene.path, out TutorialRuntimeGraphEntry graphEntry))
            {
                return;
            }

            if (graphEntry.Graph != runtimeInstance.SourceGraph)
            {
                return;
            }

            ReleaseCurrentExecution();
        }

        /// <summary>
        /// Resolve persisted progress and start the tutorial graph associated with one scene
        /// </summary>
        /// <param name="scene"></param>
        private void TryStartSceneTutorial(Scene scene)
        {
            if (!tutorialsEnabled)
            {
                return;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (runtimeCatalogue == null)
            {
                Debug.LogWarning($"TutorialFlowController '{name}' has no TutorialRuntimeCatalogue assigned.", this);

                return;
            }

            if (!runtimeCatalogue.TryGetGraphEntryByScenePath(scene.path, out TutorialRuntimeGraphEntry graphEntry))
            {
                return;
            }

            TutorialProgressSaveData savedProgress = null;

            if (progressRepository != null)
            {
                ETutorialProgressLoadResult loadResult = progressRepository.TryLoad(graphEntry.Graph.GraphGuid, out savedProgress, out string loadError);

                if (loadResult == ETutorialProgressLoadResult.InvalidData)
                {
                    Debug.LogWarning($"Invalid tutorial progress found for graph '{graphEntry.Graph.name}'. Progress will be reset. {loadError}", this);

                    if (!progressRepository.TryDelete(graphEntry.Graph.GraphGuid, out string deleteError))
                    {
                        Debug.LogWarning($"Invalid tutorial progress could not be deleted. {deleteError}", this);
                    }

                    savedProgress = null;
                }
                else if (loadResult == ETutorialProgressLoadResult.Failed)
                {
                    Debug.LogWarning($"Tutorial progress could not be loaded for graph '{graphEntry.Graph.name}'. Tutorial will start without restored progress. {loadError}", this);

                    savedProgress = null;
                }
            }

            if (TryStartTutorial(graphEntry.Graph, savedProgress, out string error))
            {
                return;
            }

            if (savedProgress == null)
            {
                Debug.LogWarning($"Unable to start tutorial graph '{graphEntry.Graph.name}' for scene '{scene.path}'. {error}", this);

                return;
            }

            string restoreError = error;

            if (!TryStartTutorial(graphEntry.Graph, null, out string freshStartError))
            {
                Debug.LogWarning($"Unable to restore tutorial graph '{graphEntry.Graph.name}'. Restore error: {restoreError} Fresh start error: {freshStartError}", this);

                return;
            }

            if (progressRepository != null && !progressRepository.TryDelete(graphEntry.Graph.GraphGuid, out string resetError))
            {
                Debug.LogWarning($"Invalid tutorial progress for graph '{graphEntry.Graph.name}' could not be deleted. {resetError}", this);

                return;
            }

            Debug.LogWarning($"Saved tutorial progress for graph '{graphEntry.Graph.name}' was incompatible and has been reset. {restoreError}", this);
        }

        /// <summary>
        /// Create and start the TutorialRunner associated with the current runtime instance
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryStartRunner(out string error)
        {
            error = string.Empty;

            if (runtimeInstance == null || progressService == null)
            {
                error = "The tutorial runtime or progress service is not available.";

                return false;
            }

            SubscribeProgress();

            IReadOnlyCollection<string> finishedNodeGuids = progressService.CreateFinishedNodeGuidSnapshot();
            tutorialRunner = new TutorialRunner(runtimeInstance, CreateStepRunner, finishedNodeGuids);

            tutorialRunner.NodeCompleted += OnNodeCompleted;
            tutorialRunner.NodeSkipped += OnNodeSkipped;
            tutorialRunner.Completed += OnRunnerCompleted;
            tutorialRunner.Failed += OnRunnerFailed;

            if (!tutorialRunner.Start())
            {
                error = string.IsNullOrWhiteSpace(tutorialRunner.LastError) ? $"Tutorial '{runtimeInstance.TutorialGuid}' could not be started." : tutorialRunner.LastError;

                ReleaseCurrentExecution();

                return false;
            }

            TutorialStarted?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Create one TutorialStepRunner when all runtime dependencies are currently available
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <returns></returns>
        private TutorialStepRunner CreateStepRunner(StepSO runtimeStep)
        {
            if (runtimeStep == null)
            {
                return null;
            }

            if (runtimeStep.StepType == EStepType.UI)
            {
                return CreateUIStepRunner(runtimeStep);
            }

            if (!methodResolver.TryResolve(runtimeStep, out TutorialResolvedMethod resolvedMethod, out _))
            {
                return null;
            }

            return new TutorialStepRunner(runtimeStep, resolvedMethod);
        }

        /// <summary>
        /// Create one TutorialStepRunner using a UI completion condition
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <returns></returns>
        private TutorialStepRunner CreateUIStepRunner(StepSO runtimeStep)
        {
            if (runtimeStep == null || !(runtimeStep.CompletionData is TutorialUICompletionData completionData))
            {
                return null;
            }

            if (!identifierRegistry.TryGet(runtimeStep.TutoGUID, out TutoIdentifier identifier))
            {
                return null;
            }

            TutorialUICompletionCondition condition = new TutorialUICompletionCondition(completionData, identifier.TargetComponent);

            return new TutorialStepRunner(runtimeStep, identifier, condition);
        }

        #endregion

        #region Runner Events

        /// <summary>
        /// Store persistent progress after one runtime node completes
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="runtimeNode"></param>
        private void OnNodeCompleted(TutorialRunner runner, TutorialRuntimeNode runtimeNode)
        {
            if (runtimeNode == null || progressService == null)
            {
                return;
            }

            progressService.MarkNodeCompleted(runtimeNode.NodeGuid);
        }

        /// <summary>
        /// Store persistent progress after one runtime node is skipped
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="runtimeNode"></param>
        private void OnNodeSkipped(TutorialRunner runner, TutorialRuntimeNode runtimeNode)
        {
            if (runtimeNode == null || progressService == null)
            {
                return;
            }

            progressService.MarkNodeSkipped(runtimeNode.NodeGuid);
        }

        /// <summary>
        /// Forward an injected Skip Current Step request to the currently executing tutorial
        /// </summary>
        private void OnTutorialSkipRequested()
        {
            if (tutorialRunner == null || tutorialRunner.IsTerminal)
            {
                return;
            }

            tutorialRunner.SkipCurrentStep();
        }

        /// <summary>
        /// Complete the current normal execution or temporary replay session
        /// </summary>
        /// <param name="runner"></param>
        private void OnRunnerCompleted(TutorialRunner runner)
        {
            if (IsReplaying)
            {
                replayService.TryCompleteReplay(out _);
            }

            TutorialCompleted?.Invoke(this);
        }

        /// <summary>
        /// Forward one fatal tutorial execution error outside the FlowController
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="error"></param>
        private void OnRunnerFailed(TutorialRunner runner, string error)
        {
            TutorialFailed?.Invoke(this, error);
        }

        #endregion

        #region Progress Events

        /// <summary>
        /// Subscribe to the progress service associated with the current execution
        /// </summary>
        private void SubscribeProgress()
        {
            if (progressService == null)
            {
                return;
            }

            progressService.Changed += OnProgressChanged;
        }

        /// <summary>
        /// Remove the current progress service subscription
        /// </summary>
        private void UnsubscribeProgress()
        {
            if (progressService == null)
            {
                return;
            }

            progressService.Changed -= OnProgressChanged;
        }

        /// <summary>
        /// Persist and forward tutorial progress snapshots while ignoring temporary replay progress
        /// </summary>
        /// <param name="service"></param>
        private void OnProgressChanged(TutorialProgressService service)
        {
            if (service == null)
            {
                return;
            }

            if (replayService != null && replayService.IsReplayProgress(service))
            {
                return;
            }

            TutorialProgressSaveData saveData = service.CreateSaveData();

            if (saveData.Status != ETutorialProgressStatus.NotStarted && progressRepository != null && !progressRepository.TrySave(saveData, out string saveError))
            {
                Debug.LogWarning($"Tutorial progress could not be persisted. {saveError}", this);
            }

            PersistentProgressChanged?.Invoke(this, saveData);
        }

        #endregion

        #region Release

        /// <summary>
        /// Release every object associated with the current tutorial execution
        /// </summary>
        private void ReleaseCurrentExecution()
        {
            ReleaseRunner();

            UnsubscribeProgress();
            progressService = null;

            if (replayService != null && replayService.IsReplaying)
            {
                replayService.CancelReplay();
            }

            ReleaseRuntimeInstance();
        }

        /// <summary>
        /// Release the currently active TutorialRunner
        /// </summary>
        private void ReleaseRunner()
        {
            if (tutorialRunner == null)
            {
                return;
            }

            tutorialRunner.NodeCompleted -= OnNodeCompleted;
            tutorialRunner.NodeSkipped -= OnNodeSkipped;
            tutorialRunner.Completed -= OnRunnerCompleted;
            tutorialRunner.Failed -= OnRunnerFailed;

            tutorialRunner.Dispose();
            tutorialRunner = null;
        }

        /// <summary>
        /// Remove and dispose the currently controlled tutorial runtime instance
        /// </summary>
        private void ReleaseRuntimeInstance()
        {
            if (runtimeInstance == null)
            {
                return;
            }

            runtimeRegistry.TryRemove(runtimeInstance.TutorialGuid);
            runtimeInstance = null;
        }

        #endregion

        #region Identifier Lifecycle

        /// <summary>
        /// Subscribe to runtime TutoIdentifier availability changes
        /// </summary>
        private void SubscribeIdentifierLifecycle()
        {
            TutoIdentifier.BecameAvailable -= OnIdentifierBecameAvailable;
            TutoIdentifier.BecameUnavailable -= OnIdentifierBecameUnavailable;

            TutoIdentifier.BecameAvailable += OnIdentifierBecameAvailable;
            TutoIdentifier.BecameUnavailable += OnIdentifierBecameUnavailable;
        }

        /// <summary>
        /// Remove runtime TutoIdentifier availability subscriptions
        /// </summary>
        private void UnsubscribeIdentifierLifecycle()
        {
            TutoIdentifier.BecameAvailable -= OnIdentifierBecameAvailable;
            TutoIdentifier.BecameUnavailable -= OnIdentifierBecameUnavailable;
        }

        /// <summary>
        /// Register every active TutoIdentifier already loaded before this FlowController became available
        /// </summary>
        private void RegisterLoadedIdentifiers()
        {
            TutoIdentifier[] loadedIdentifiers = UnityEngine.Object.FindObjectsByType<TutoIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (TutoIdentifier identifier in loadedIdentifiers)
            {
                if (identifier == null || !identifier.isActiveAndEnabled)
                {
                    continue;
                }

                if (!TryRegisterIdentifier(identifier, out string error))
                {
                    Debug.LogWarning(error, identifier);
                }
            }
        }

        /// <summary>
        /// Register one TutoIdentifier when it becomes available
        /// </summary>
        /// <param name="identifier"></param>
        private void OnIdentifierBecameAvailable(TutoIdentifier identifier)
        {
            if (TryRegisterIdentifier(identifier, out string error))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning(error, identifier);
            }
        }

        /// <summary>
        /// Unregister one TutoIdentifier when it becomes unavailable
        /// </summary>
        /// <param name="identifier"></param>
        private void OnIdentifierBecameUnavailable(TutoIdentifier identifier)
        {
            TryUnregisterIdentifier(identifier);
        }

        #endregion
    }
}