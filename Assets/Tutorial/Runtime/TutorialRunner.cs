using System;
using System.Collections.Generic;
using Tutorial.Runtime.Core;
using Tutorial.Runtime.Data;

namespace Tutorial.Runtime.Execution
{
    /// <summary>
    /// Orchestrate the execution of every root, Step and sequence contained by one tutorial runtime instance
    /// </summary>
    public sealed class TutorialRunner : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Runtime tutorial graph executed by this runner
        /// </summary>
        private readonly TutorialRuntimeInstance runtimeInstance = null;

        /// <summary>
        /// Factory used to create TutorialStepRunner instances when their runtime dependencies are available
        /// </summary>
        private readonly Func<StepSO, TutorialStepRunner> stepRunnerFactory = null;

        /// <summary>
        /// Active standalone Step runners indexed by their runtime node GUID
        /// </summary>
        private readonly Dictionary<string, TutorialStepRunner> activeStepRunners = new Dictionary<string, TutorialStepRunner>(StringComparer.Ordinal);

        /// <summary>
        /// Runtime node GUID associated with each active standalone Step runner
        /// </summary>
        private readonly Dictionary<TutorialStepRunner, string> stepRunnerNodeGuids = new Dictionary<TutorialStepRunner, string>();

        /// <summary>
        /// Active sequence runners indexed by their runtime node GUID
        /// </summary>
        private readonly Dictionary<string, TutorialSequenceRunner> activeSequenceRunners = new Dictionary<string, TutorialSequenceRunner>(StringComparer.Ordinal);

        /// <summary>
        /// Runtime node GUID associated with each active sequence runner
        /// </summary>
        private readonly Dictionary<TutorialSequenceRunner, string> sequenceRunnerNodeGuids = new Dictionary<TutorialSequenceRunner, string>();

        /// <summary>
        /// Runtime nodes waiting for dependencies and whether they must activate immediately once available
        /// </summary>
        private readonly Dictionary<string, bool> pendingNodeActivationModes = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Runtime nodes whose execution has already terminated successfully or by skip
        /// </summary>
        private readonly HashSet<string> finishedNodeGuids = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Current lifecycle status of this tutorial runner
        /// </summary>
        private ETutorialRunnerStatus status = ETutorialRunnerStatus.Created;

        /// <summary>
        /// Last fatal error encountered while executing the tutorial
        /// </summary>
        private string lastError = string.Empty;

        #endregion

        #region Properties

        public TutorialRuntimeInstance RuntimeInstance => runtimeInstance;
        public ETutorialRunnerStatus Status => status;
        public string LastError => lastError;
        public int ActiveStepRunnerCount => activeStepRunners.Count;
        public int ActiveSequenceRunnerCount => activeSequenceRunners.Count;
        public int PendingNodeCount => pendingNodeActivationModes.Count;
        public int FinishedNodeCount => finishedNodeGuids.Count;
        public bool IsRunning => status == ETutorialRunnerStatus.Running;
        public bool IsWaiting => status == ETutorialRunnerStatus.WaitingForDependencies;
        public bool IsCompleted => status == ETutorialRunnerStatus.Completed;
        public bool IsFailed => status == ETutorialRunnerStatus.Failed;
        public bool IsDisposed => status == ETutorialRunnerStatus.Disposed;
        public bool IsTerminal => IsCompleted || IsFailed || IsDisposed;

        #endregion

        #region Events

        public event Action<TutorialRunner> Started = null;
        public event Action<TutorialRunner, TutorialRuntimeNode> NodeStarted = null;
        public event Action<TutorialRunner, TutorialRuntimeNode> NodeCompleted = null;
        public event Action<TutorialRunner, TutorialRuntimeNode> NodeSkipped = null;
        public event Action<TutorialRunner> WaitingForDependencies = null;
        public event Action<TutorialRunner> Completed = null;
        public event Action<TutorialRunner, string> Failed = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a tutorial runner from a reconstructed runtime instance and Step runner factory
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="stepRunnerFactory"></param>
        public TutorialRunner(TutorialRuntimeInstance runtimeInstance, Func<StepSO, TutorialStepRunner> stepRunnerFactory)
        {
            this.runtimeInstance = runtimeInstance != null ? runtimeInstance : throw new ArgumentNullException(nameof(runtimeInstance));
            this.stepRunnerFactory = stepRunnerFactory ?? throw new ArgumentNullException(nameof(stepRunnerFactory));
        }

        /// <summary>
        /// Create a tutorial runner from a reconstructed runtime instance, Step runner factory and previously finished runtime nodes
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="stepRunnerFactory"></param>
        /// <param name="initialFinishedNodeGuids"></param>
        public TutorialRunner(TutorialRuntimeInstance runtimeInstance, Func<StepSO, TutorialStepRunner> stepRunnerFactory, IReadOnlyCollection<string> initialFinishedNodeGuids)
        {
            this.runtimeInstance = runtimeInstance != null ? runtimeInstance : throw new ArgumentNullException(nameof(runtimeInstance));
            this.stepRunnerFactory = stepRunnerFactory ?? throw new ArgumentNullException(nameof(stepRunnerFactory));

            if (initialFinishedNodeGuids == null)
            {
                return;
            }

            foreach (string nodeGuid in initialFinishedNodeGuids)
            {
                if (string.IsNullOrWhiteSpace(nodeGuid))
                {
                    throw new ArgumentException("Initial finished runtime nodes contain an empty GUID.", nameof(initialFinishedNodeGuids));
                }

                if (!runtimeInstance.RuntimeNodes.ContainsKey(nodeGuid))
                {
                    throw new ArgumentException($"Initial finished runtime node '{nodeGuid}' does not exist inside tutorial '{runtimeInstance.TutorialGuid}'.", nameof(initialFinishedNodeGuids));
                }

                finishedNodeGuids.Add(nodeGuid);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start every root node currently eligible for execution
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (status != ETutorialRunnerStatus.Created)
            {
                return false;
            }

            if (runtimeInstance.IsDisposed)
            {
                return FailRunner("The tutorial runtime instance has already been disposed.");
            }

            if (runtimeInstance.Status != ETutorialRuntimeInstanceStatus.Ready)
            {
                return FailRunner(
                    $"Tutorial runtime instance '{runtimeInstance.TutorialGuid}' cannot start from status '{runtimeInstance.Status}'."
                );
            }

            if (runtimeInstance.RootNodeGuids == null || runtimeInstance.RootNodeGuids.Count == 0)
            {
                return FailRunner(
                    $"Tutorial runtime instance '{runtimeInstance.TutorialGuid}' contains no root node."
                );
            }

            runtimeInstance.SetStatus(ETutorialRuntimeInstanceStatus.Running);
            status = ETutorialRunnerStatus.Running;

            HashSet<string> visitedFinishedNodeGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (string rootNodeGuid in runtimeInstance.RootNodeGuids)
            {
                if (!TryActivateExecutionFrontier(rootNodeGuid, false, visitedFinishedNodeGuids))
                {
                    return false;
                }
            }

            Started?.Invoke(this);

            EvaluateStatus();

            return !IsFailed;
        }

        /// <summary>
        /// Skip the single Step currently executed by this tutorial
        /// </summary>
        /// <returns></returns>
        public bool SkipCurrentStep()
        {
            if (IsTerminal || status == ETutorialRunnerStatus.Created)
            {
                return false;
            }

            TutorialStepRunner standaloneStepRunner = null;
            TutorialSequenceRunner sequenceRunnerTarget = null;
            int targetCount = 0;

            foreach (TutorialStepRunner stepRunner in activeStepRunners.Values)
            {
                if (stepRunner == null || !stepRunner.IsRunning)
                {
                    continue;
                }

                standaloneStepRunner = stepRunner;
                targetCount++;
            }

            foreach (TutorialSequenceRunner sequenceRunner in activeSequenceRunners.Values)
            {
                if (sequenceRunner == null || sequenceRunner.IsTerminal || sequenceRunner.CurrentStepRunner == null || !sequenceRunner.CurrentStepRunner.IsRunning)
                {
                    continue;
                }

                sequenceRunnerTarget = sequenceRunner;
                targetCount++;
            }

            if (targetCount == 1)
            {
                if (standaloneStepRunner != null)
                {
                    return standaloneStepRunner.Skip();
                }

                return sequenceRunnerTarget.SkipCurrentStep();
            }

            if (targetCount > 1)
            {
                return false;
            }

            return SkipSingleWaitingStep();
        }

        /// <summary>
        /// Skip one Step waiting for activation when no Step is currently running
        /// </summary>
        /// <returns></returns>
        private bool SkipSingleWaitingStep()
        {
            TutorialStepRunner standaloneStepRunner = null;
            TutorialSequenceRunner sequenceRunnerTarget = null;
            int targetCount = 0;

            foreach (TutorialStepRunner stepRunner in activeStepRunners.Values)
            {
                if (stepRunner == null || !stepRunner.IsWaiting)
                {
                    continue;
                }

                standaloneStepRunner = stepRunner;
                targetCount++;
            }

            foreach (TutorialSequenceRunner sequenceRunner in activeSequenceRunners.Values)
            {
                if (sequenceRunner == null || sequenceRunner.IsTerminal || sequenceRunner.CurrentStepRunner == null || !sequenceRunner.CurrentStepRunner.IsWaiting)
                {
                    continue;
                }

                sequenceRunnerTarget = sequenceRunner;
                targetCount++;
            }

            if (targetCount != 1)
            {
                return false;
            }

            if (standaloneStepRunner != null)
            {
                return standaloneStepRunner.Skip();
            }

            return sequenceRunnerTarget.SkipCurrentStep();
        }


        /// <summary>
        /// Process an external trigger for one currently active tutorial Step
        /// </summary>
        /// <param name="stepGuid"></param>
        /// <returns></returns>
        public bool TryTriggerStep(string stepGuid)
        {
            if (IsTerminal || status == ETutorialRunnerStatus.Created || string.IsNullOrWhiteSpace(stepGuid))
            {
                return false;
            }

            foreach (KeyValuePair<string, TutorialStepRunner> pair in activeStepRunners)
            {
                TutorialStepRunner stepRunner = pair.Value;

                if (stepRunner == null)
                {
                    continue;
                }

                if (!runtimeInstance.TryGetRuntimeNode(pair.Key, out TutorialRuntimeNode runtimeNode))
                {
                    continue;
                }

                if (!string.Equals(runtimeNode.StepGuid, stepGuid, StringComparison.Ordinal))
                {
                    continue;
                }

                if (stepRunner.IsRunning)
                {
                    return true;
                }

                if (!stepRunner.IsWaiting)
                {
                    return false;
                }

                if (!stepRunner.Activate())
                {
                    string error = string.IsNullOrWhiteSpace(stepRunner.LastError) ? $"Tutorial Step '{stepGuid}' could not be activated." : stepRunner.LastError;

                    return FailRunner(error);
                }

                EvaluateStatus();

                return true;
            }

            foreach (TutorialSequenceRunner sequenceRunner in activeSequenceRunners.Values)
            {
                if (sequenceRunner == null || sequenceRunner.IsTerminal || sequenceRunner.CurrentStepRunner == null)
                {
                    continue;
                }

                TutorialStepRunner stepRunner = sequenceRunner.CurrentStepRunner;
                StepSO runtimeStep = stepRunner.RuntimeStep;

                if (runtimeStep == null || !string.Equals(runtimeStep.StepGUID, stepGuid, StringComparison.Ordinal))
                {
                    continue;
                }

                if (stepRunner.IsRunning)
                {
                    return true;
                }

                if (!stepRunner.IsWaiting)
                {
                    return false;
                }

                if (!stepRunner.Activate())
                {
                    string error = string.IsNullOrWhiteSpace(stepRunner.LastError) ? $"Tutorial Step '{stepGuid}' contained by sequence '{sequenceRunner.RuntimeSequence.name}' could not be activated." : stepRunner.LastError;

                    return FailRunner(error);
                }

                EvaluateStatus();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Retry every runtime node currently waiting for unavailable dependencies
        /// </summary>
        /// <returns></returns>
        public bool TryResume()
        {
            if (IsTerminal || status == ETutorialRunnerStatus.Created)
            {
                return false;
            }

            bool resumed = false;

            List<TutorialSequenceRunner> sequences = new List<TutorialSequenceRunner>(activeSequenceRunners.Values);

            foreach (TutorialSequenceRunner sequenceRunner in sequences)
            {
                if (sequenceRunner == null || !sequenceRunner.IsWaiting)
                {
                    continue;
                }

                if (sequenceRunner.TryResume())
                {
                    resumed = true;
                }
            }

            List<string> pendingNodes = new List<string>(pendingNodeActivationModes.Keys);

            foreach (string nodeGuid in pendingNodes)
            {
                if (!pendingNodeActivationModes.TryGetValue(nodeGuid, out bool activateImmediately))
                {
                    continue;
                }

                if (!TryActivateNode(nodeGuid, activateImmediately))
                {
                    if (IsFailed)
                    {
                        return false;
                    }

                    continue;
                }

                if (!pendingNodeActivationModes.ContainsKey(nodeGuid))
                {
                    resumed = true;
                }
            }

            EvaluateStatus();

            return resumed;
        }

        /// <summary>
        /// Release every active runner and stop this tutorial runtime execution
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            ReleaseAllRunners();

            pendingNodeActivationModes.Clear();
            finishedNodeGuids.Clear();

            Started = null;
            NodeStarted = null;
            NodeCompleted = null;
            NodeSkipped = null;
            WaitingForDependencies = null;
            Completed = null;
            Failed = null;

            status = ETutorialRunnerStatus.Disposed;
        }

        #endregion

        #region Node Activation

        /// <summary>
        /// Prepare one runtime node and optionally activate it immediately
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="activateImmediately"></param>
        /// <returns></returns>
        private bool TryActivateNode(string nodeGuid, bool activateImmediately)
        {
            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                return FailRunner("A runtime node with an empty GUID cannot be activated.");
            }

            if (finishedNodeGuids.Contains(nodeGuid))
            {
                return true;
            }

            if (activeStepRunners.ContainsKey(nodeGuid) || activeSequenceRunners.ContainsKey(nodeGuid))
            {
                return true;
            }

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                return FailRunner($"Runtime node '{nodeGuid}' could not be found.");
            }

            pendingNodeActivationModes.Remove(nodeGuid);

            if (runtimeNode.RuntimeStep is StepSequenceSO runtimeSequence)
            {
                return TryActivateSequenceNode(runtimeNode, runtimeSequence);
            }

            return TryActivateStepNode(runtimeNode, activateImmediately);
        }

        /// <summary>
        /// Traverse previously finished runtime nodes and activate the first unfinished execution nodes reachable from them
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="activateImmediately"></param>
        /// <param name="visitedFinishedNodeGuids"></param>
        /// <returns></returns>
        private bool TryActivateExecutionFrontier(string nodeGuid, bool activateImmediately, HashSet<string> visitedFinishedNodeGuids)
        {
            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                return FailRunner("A runtime node with an empty GUID cannot be traversed.");
            }

            if (!finishedNodeGuids.Contains(nodeGuid))
            {
                return TryActivateNode(nodeGuid, activateImmediately);
            }

            if (!visitedFinishedNodeGuids.Add(nodeGuid))
            {
                return true;
            }

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                return FailRunner($"Finished runtime node '{nodeGuid}' could not be found.");
            }

            foreach (string targetNodeGuid in runtimeNode.NextNodeGuids)
            {
                if (!TryActivateExecutionFrontier(targetNodeGuid, true, visitedFinishedNodeGuids))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Create and prepare the TutorialStepRunner associated with one standalone runtime Step node
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <param name="activateImmediately"></param>
        /// <returns></returns>
        private bool TryActivateStepNode(TutorialRuntimeNode runtimeNode, bool activateImmediately)
        {
            TutorialStepRunner stepRunner = stepRunnerFactory.Invoke(runtimeNode.RuntimeStep);

            if (stepRunner == null)
            {
                pendingNodeActivationModes[runtimeNode.NodeGuid] = activateImmediately;
                return true;
            }

            activeStepRunners.Add(runtimeNode.NodeGuid, stepRunner);
            stepRunnerNodeGuids.Add(stepRunner, runtimeNode.NodeGuid);

            stepRunner.Triggered += OnStepRunnerTriggered;
            stepRunner.Completed += OnStepRunnerCompleted;
            stepRunner.Skipped += OnStepRunnerSkipped;

            if (!stepRunner.Start())
            {
                ReleaseStepRunner(runtimeNode.NodeGuid);
                return FailRunner($"TutorialStepRunner associated with node '{runtimeNode.NodeGuid}' could not be started.");
            }

            if (!activateImmediately)
            {
                return true;
            }

            if (stepRunner.Activate())
            {
                return true;
            }

            string error = string.IsNullOrWhiteSpace(stepRunner.LastError) ? $"TutorialStepRunner associated with node '{runtimeNode.NodeGuid}' could not be activated." : stepRunner.LastError;

            ReleaseStepRunner(runtimeNode.NodeGuid);

            return FailRunner(error);
        }

        /// <summary>
        /// Create and start the TutorialSequenceRunner associated with one runtime sequence node
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <param name="runtimeSequence"></param>
        /// <returns></returns>
        private bool TryActivateSequenceNode(TutorialRuntimeNode runtimeNode, StepSequenceSO runtimeSequence)
        {
            TutorialSequenceRunner sequenceRunner = new TutorialSequenceRunner(runtimeSequence, stepRunnerFactory);

            activeSequenceRunners.Add(runtimeNode.NodeGuid, sequenceRunner);
            sequenceRunnerNodeGuids.Add(sequenceRunner, runtimeNode.NodeGuid);

            sequenceRunner.Completed += OnSequenceRunnerCompleted;
            sequenceRunner.Skipped += OnSequenceRunnerSkipped;
            sequenceRunner.Failed += OnSequenceRunnerFailed;

            if (!sequenceRunner.Start())
            {
                if (!sequenceRunner.IsFailed)
                {
                    ReleaseSequenceRunner(runtimeNode.NodeGuid);

                    return FailRunner(
                        $"TutorialSequenceRunner associated with node '{runtimeNode.NodeGuid}' could not be started."
                    );
                }

                return false;
            }

            NodeStarted?.Invoke(this, runtimeNode);

            return true;
        }

        #endregion

        #region Step Runner Events

        /// <summary>
        /// Process the successful completion of one standalone Step runner
        /// </summary>
        /// <param name="stepRunner"></param>
        private void OnStepRunnerCompleted(TutorialStepRunner stepRunner)
        {
            if (!TryGetStepRunnerNode(stepRunner, out TutorialRuntimeNode runtimeNode))
            {
                FailRunner("A completed TutorialStepRunner could not be associated with its runtime node.");

                return;
            }

            string nodeGuid = runtimeNode.NodeGuid;

            ReleaseStepRunner(nodeGuid);
            FinishNode(runtimeNode, false);
        }

        /// <summary>
        /// Process the skip of one standalone Step runner
        /// </summary>
        /// <param name="stepRunner"></param>
        private void OnStepRunnerSkipped(TutorialStepRunner stepRunner)
        {
            if (!TryGetStepRunnerNode(stepRunner, out TutorialRuntimeNode runtimeNode))
            {
                FailRunner("A skipped TutorialStepRunner could not be associated with its runtime node.");

                return;
            }

            string nodeGuid = runtimeNode.NodeGuid;

            ReleaseStepRunner(nodeGuid);
            FinishNode(runtimeNode, true);
        }

        #endregion

        #region Sequence Runner Events

        /// <summary>
        /// Process the activation of one standalone Step runner
        /// </summary>
        /// <param name="stepRunner"></param>
        private void OnStepRunnerTriggered(TutorialStepRunner stepRunner)
        {
            if (!TryGetStepRunnerNode(stepRunner, out TutorialRuntimeNode runtimeNode))
            {
                FailRunner("A triggered TutorialStepRunner could not be associated with its runtime node.");
                return;
            }

            runtimeNode.RuntimeStep.OnTrigger();

            NodeStarted?.Invoke(this, runtimeNode);
        }

        /// <summary>
        /// Process the successful completion of one runtime sequence
        /// </summary>
        /// <param name="sequenceRunner"></param>
        private void OnSequenceRunnerCompleted(TutorialSequenceRunner sequenceRunner)
        {
            if (!TryGetSequenceRunnerNode(sequenceRunner, out TutorialRuntimeNode runtimeNode))
            {
                FailRunner("A completed TutorialSequenceRunner could not be associated with its runtime node.");

                return;
            }

            string nodeGuid = runtimeNode.NodeGuid;

            ReleaseSequenceRunner(nodeGuid);
            FinishNode(runtimeNode, false);
        }

        /// <summary>
        /// Process the skip of one complete runtime sequence
        /// </summary>
        /// <param name="sequenceRunner"></param>
        private void OnSequenceRunnerSkipped(TutorialSequenceRunner sequenceRunner)
        {
            if (!TryGetSequenceRunnerNode(sequenceRunner, out TutorialRuntimeNode runtimeNode))
            {
                FailRunner("A skipped TutorialSequenceRunner could not be associated with its runtime node.");

                return;
            }

            string nodeGuid = runtimeNode.NodeGuid;

            ReleaseSequenceRunner(nodeGuid);
            FinishNode(runtimeNode, true);
        }

        /// <summary>
        /// Process a fatal error emitted by one runtime sequence
        /// </summary>
        /// <param name="sequenceRunner"></param>
        /// <param name="error"></param>
        private void OnSequenceRunnerFailed(TutorialSequenceRunner sequenceRunner, string error)
        {
            FailRunner(
                string.IsNullOrWhiteSpace(error)
                    ? "A TutorialSequenceRunner failed without an error message."
                    : error
            );
        }

        #endregion

        #region Node Completion

        /// <summary>
        /// Mark one runtime node as finished and activate every node following it
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <param name="wasSkipped"></param>
        private void FinishNode(TutorialRuntimeNode runtimeNode, bool wasSkipped)
        {
            if (runtimeNode == null || IsTerminal)
            {
                return;
            }

            if (!finishedNodeGuids.Add(runtimeNode.NodeGuid))
            {
                return;
            }

            if (wasSkipped)
            {
                NodeSkipped?.Invoke(this, runtimeNode);
            }
            else
            {
                NodeCompleted?.Invoke(this, runtimeNode);
            }

            foreach (string targetNodeGuid in runtimeNode.NextNodeGuids)
            {
                if (!TryActivateNode(targetNodeGuid, true))
                {
                    return;
                }
            }

            EvaluateStatus();
        }

        #endregion

        #region Runner Status

        /// <summary>
        /// Evaluate the current execution state after a runtime node changes
        /// </summary>
        private void EvaluateStatus()
        {
            if (IsTerminal)
            {
                return;
            }

            if (finishedNodeGuids.Count == runtimeInstance.RuntimeNodes.Count)
            {
                CompleteRunner();

                return;
            }

            if (HasExecutableRunner())
            {
                status = ETutorialRunnerStatus.Running;

                return;
            }

            if (pendingNodeActivationModes.Count > 0 || HasWaitingSequenceRunner())
            {
                bool wasWaiting = status == ETutorialRunnerStatus.WaitingForDependencies;

                status = ETutorialRunnerStatus.WaitingForDependencies;

                if (!wasWaiting)
                {
                    WaitingForDependencies?.Invoke(this);
                }

                return;
            }

            FailRunner($"Tutorial runtime '{runtimeInstance.TutorialGuid}' has unfinished nodes but no active or pending runner.");
        }

        /// <summary>
        /// Determine whether at least one runtime runner is actively waiting for gameplay or executing
        /// </summary>
        /// <returns></returns>
        private bool HasExecutableRunner()
        {
            if (activeStepRunners.Count > 0)
            {
                return true;
            }

            foreach (TutorialSequenceRunner sequenceRunner in activeSequenceRunners.Values)
            {
                if (sequenceRunner != null && sequenceRunner.IsRunning)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine whether at least one active sequence is waiting for an unavailable Step dependency
        /// </summary>
        /// <returns></returns>
        private bool HasWaitingSequenceRunner()
        {
            foreach (TutorialSequenceRunner sequenceRunner in activeSequenceRunners.Values)
            {
                if (sequenceRunner != null && sequenceRunner.IsWaiting)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Complete the whole tutorial runtime after every node has terminated
        /// </summary>
        private void CompleteRunner()
        {
            ReleaseAllRunners();

            status = ETutorialRunnerStatus.Completed;
            runtimeInstance.SetStatus(ETutorialRuntimeInstanceStatus.Completed);

            Completed?.Invoke(this);
        }

        /// <summary>
        /// Stop the tutorial runtime after a fatal execution error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool FailRunner(string error)
        {
            if (IsDisposed)
            {
                return false;
            }

            ReleaseAllRunners();

            lastError = error;
            status = ETutorialRunnerStatus.Failed;

            if (!runtimeInstance.IsDisposed)
            {
                runtimeInstance.SetStatus(ETutorialRuntimeInstanceStatus.Failed);
            }

            Failed?.Invoke(this, error);

            return false;
        }

        #endregion

        #region Runner Lookup

        /// <summary>
        /// Retrieve the runtime node associated with one standalone Step runner
        /// </summary>
        /// <param name="stepRunner"></param>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        private bool TryGetStepRunnerNode(TutorialStepRunner stepRunner, out TutorialRuntimeNode runtimeNode)
        {
            runtimeNode = null;

            if (stepRunner == null || !stepRunnerNodeGuids.TryGetValue(stepRunner, out string nodeGuid))
            {
                return false;
            }

            return runtimeInstance.TryGetRuntimeNode(nodeGuid, out runtimeNode);
        }

        /// <summary>
        /// Retrieve the runtime node associated with one sequence runner
        /// </summary>
        /// <param name="sequenceRunner"></param>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        private bool TryGetSequenceRunnerNode(TutorialSequenceRunner sequenceRunner, out TutorialRuntimeNode runtimeNode)
        {
            runtimeNode = null;

            if (sequenceRunner == null || !sequenceRunnerNodeGuids.TryGetValue(sequenceRunner, out string nodeGuid))
            {
                return false;
            }

            return runtimeInstance.TryGetRuntimeNode(nodeGuid, out runtimeNode);
        }

        #endregion

        #region Runner Release

        /// <summary>
        /// Release one standalone TutorialStepRunner
        /// </summary>
        /// <param name="nodeGuid"></param>
        private void ReleaseStepRunner(string nodeGuid)
        {
            if (!activeStepRunners.TryGetValue(nodeGuid, out TutorialStepRunner stepRunner))
            {
                return;
            }

            stepRunner.Triggered -= OnStepRunnerTriggered;
            stepRunner.Completed -= OnStepRunnerCompleted;
            stepRunner.Skipped -= OnStepRunnerSkipped;

            stepRunnerNodeGuids.Remove(stepRunner);
            activeStepRunners.Remove(nodeGuid);

            stepRunner.Dispose();
        }

        /// <summary>
        /// Release one TutorialSequenceRunner
        /// </summary>
        /// <param name="nodeGuid"></param>
        private void ReleaseSequenceRunner(string nodeGuid)
        {
            if (!activeSequenceRunners.TryGetValue(nodeGuid, out TutorialSequenceRunner sequenceRunner))
            {
                return;
            }

            sequenceRunner.Completed -= OnSequenceRunnerCompleted;
            sequenceRunner.Skipped -= OnSequenceRunnerSkipped;
            sequenceRunner.Failed -= OnSequenceRunnerFailed;

            sequenceRunnerNodeGuids.Remove(sequenceRunner);
            activeSequenceRunners.Remove(nodeGuid);

            sequenceRunner.Dispose();
        }

        /// <summary>
        /// Release every currently active Step and sequence runner
        /// </summary>
        private void ReleaseAllRunners()
        {
            List<string> stepNodeGuids = new List<string>(activeStepRunners.Keys);

            foreach (string nodeGuid in stepNodeGuids)
            {
                ReleaseStepRunner(nodeGuid);
            }

            List<string> sequenceNodeGuids = new List<string>(activeSequenceRunners.Keys);

            foreach (string nodeGuid in sequenceNodeGuids)
            {
                ReleaseSequenceRunner(nodeGuid);
            }
        }

        #endregion
    }
}