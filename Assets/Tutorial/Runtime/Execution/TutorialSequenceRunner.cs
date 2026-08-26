using System;
using Tutorial.Runtime.Data;

namespace Tutorial.Runtime.Execution
{
    /// <summary>
    /// Execute every StepSO contained by one runtime StepSequenceSO in its configured order
    /// </summary>
    public sealed class TutorialSequenceRunner : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Runtime sequence containing the ordered StepSO list executed by this runner
        /// </summary>
        private readonly StepSequenceSO runtimeSequence = null;

        /// <summary>
        /// Factory used to create one TutorialStepRunner when its Step becomes current
        /// </summary>
        private readonly Func<StepSO, TutorialStepRunner> stepRunnerFactory = null;

        /// <summary>
        /// TutorialStepRunner currently executed by this sequence
        /// </summary>
        private TutorialStepRunner currentStepRunner = null;

        /// <summary>
        /// Index of the StepSO currently processed inside the runtime sequence
        /// </summary>
        private int currentStepIndex = -1;

        /// <summary>
        /// Current lifecycle status of this sequence runner
        /// </summary>
        private ETutorialSequenceRunnerStatus status = ETutorialSequenceRunnerStatus.Created;

        /// <summary>
        /// Last fatal error encountered while executing this sequence
        /// </summary>
        private string lastError = string.Empty;

        #endregion

        #region Properties

        public StepSequenceSO RuntimeSequence => runtimeSequence;
        public TutorialStepRunner CurrentStepRunner => currentStepRunner;
        public int CurrentStepIndex => currentStepIndex;
        public ETutorialSequenceRunnerStatus Status => status;
        public string LastError => lastError;
        public bool IsWaiting => status == ETutorialSequenceRunnerStatus.WaitingForStep;
        public bool IsRunning => status == ETutorialSequenceRunnerStatus.Running;
        public bool IsCompleted => status == ETutorialSequenceRunnerStatus.Completed;
        public bool IsSkipped => status == ETutorialSequenceRunnerStatus.Skipped;
        public bool IsFailed => status == ETutorialSequenceRunnerStatus.Failed;
        public bool IsDisposed => status == ETutorialSequenceRunnerStatus.Disposed;
        public bool IsTerminal => IsCompleted || IsSkipped || IsFailed || IsDisposed;

        public StepSO CurrentStep
        {
            get
            {
                if (runtimeSequence.SequenceSOList == null || currentStepIndex < 0 || currentStepIndex >= runtimeSequence.SequenceSOList.Count)
                {
                    return null;
                }

                return runtimeSequence.SequenceSOList[currentStepIndex];
            }
        }

        #endregion

        #region Events

        public event Action<TutorialSequenceRunner, StepSO> StepStarted = null;
        public event Action<TutorialSequenceRunner, StepSO> StepCompleted = null;
        public event Action<TutorialSequenceRunner, StepSO> StepSkipped = null;
        public event Action<TutorialSequenceRunner> Completed = null;
        public event Action<TutorialSequenceRunner> Skipped = null;
        public event Action<TutorialSequenceRunner, string> Failed = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a sequence runner from a runtime StepSequenceSO and a Step runner factory
        /// </summary>
        /// <param name="runtimeSequence"></param>
        /// <param name="stepRunnerFactory"></param>
        public TutorialSequenceRunner(StepSequenceSO runtimeSequence, Func<StepSO, TutorialStepRunner> stepRunnerFactory)
        {
            this.runtimeSequence = runtimeSequence != null ? runtimeSequence : throw new ArgumentNullException(nameof(runtimeSequence));
            this.stepRunnerFactory = stepRunnerFactory ?? throw new ArgumentNullException(nameof(stepRunnerFactory));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start the sequence from its first configured StepSO
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (status != ETutorialSequenceRunnerStatus.Created)
            {
                return false;
            }

            if (runtimeSequence.SequenceSOList == null || runtimeSequence.SequenceSOList.Count == 0)
            {
                return FailSequence($"StepSequenceSO '{runtimeSequence.name}' contains no StepSO.");
            }

            currentStepIndex = 0;

            return TryPrepareCurrentStep();
        }

        /// <summary>
        /// Retry the current Step after its runtime dependencies become available
        /// </summary>
        /// <returns></returns>
        public bool TryResume()
        {
            if (status != ETutorialSequenceRunnerStatus.WaitingForStep)
            {
                return false;
            }

            if (!TryPrepareCurrentStep())
            {
                return false;
            }

            return status != ETutorialSequenceRunnerStatus.WaitingForStep;
        }

        /// <summary>
        /// Skip only the currently executed Step and continue this sequence
        /// </summary>
        /// <returns></returns>
        public bool SkipCurrentStep()
        {
            if (status != ETutorialSequenceRunnerStatus.Running || currentStepRunner == null)
            {
                return false;
            }

            return currentStepRunner.Skip();
        }

        /// <summary>
        /// Skip every remaining Step and terminate this sequence
        /// </summary>
        /// <returns></returns>
        public bool Skip()
        {
            if (IsTerminal)
            {
                return false;
            }

            ReleaseCurrentStepRunner();

            status = ETutorialSequenceRunnerStatus.Skipped;

            Skipped?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Release the current Step runner and every event owned by this sequence
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            ReleaseCurrentStepRunner();

            StepStarted = null;
            StepCompleted = null;
            StepSkipped = null;
            Completed = null;
            Skipped = null;
            Failed = null;

            status = ETutorialSequenceRunnerStatus.Disposed;
        }

        #endregion

        #region Step Execution

        /// <summary>
        /// Create and start the TutorialStepRunner associated with the current sequence StepSO
        /// </summary>
        /// <returns></returns>
        private bool TryPrepareCurrentStep()
        {
            if (currentStepIndex < 0 || currentStepIndex >= runtimeSequence.SequenceSOList.Count)
            {
                CompleteSequence();

                return true;
            }

            StepSO runtimeStep = runtimeSequence.SequenceSOList[currentStepIndex];

            if (runtimeStep == null)
            {
                return FailSequence(
                    $"StepSequenceSO '{runtimeSequence.name}' contains a null StepSO at index {currentStepIndex}."
                );
            }

            TutorialStepRunner createdRunner = stepRunnerFactory.Invoke(runtimeStep);

            if (createdRunner == null)
            {
                status = ETutorialSequenceRunnerStatus.WaitingForStep;

                return true;
            }

            currentStepRunner = createdRunner;

            SubscribeCurrentStepRunner();

            if (!currentStepRunner.Start())
            {
                return FailSequence(
                    $"TutorialStepRunner associated with StepSO '{runtimeStep.name}' could not be started."
                );
            }

            status = ETutorialSequenceRunnerStatus.Running;

            StepStarted?.Invoke(this, runtimeStep);

            return true;
        }

        /// <summary>
        /// Process the completion of the currently executed TutorialStepRunner
        /// </summary>
        /// <param name="stepRunner"></param>
        private void OnCurrentStepCompleted(TutorialStepRunner stepRunner)
        {
            if (stepRunner != currentStepRunner)
            {
                return;
            }

            StepSO completedStep = CurrentStep;

            StepCompleted?.Invoke(this, completedStep);

            MoveToNextStep();
        }

        /// <summary>
        /// Process the skip of the currently executed TutorialStepRunner
        /// </summary>
        /// <param name="stepRunner"></param>
        private void OnCurrentStepSkipped(TutorialStepRunner stepRunner)
        {
            if (stepRunner != currentStepRunner)
            {
                return;
            }

            StepSO skippedStep = CurrentStep;

            StepSkipped?.Invoke(this, skippedStep);

            MoveToNextStep();
        }

        /// <summary>
        /// Release the current Step runner and continue toward the next configured StepSO
        /// </summary>
        private void MoveToNextStep()
        {
            ReleaseCurrentStepRunner();

            currentStepIndex++;

            if (currentStepIndex >= runtimeSequence.SequenceSOList.Count)
            {
                CompleteSequence();

                return;
            }

            TryPrepareCurrentStep();
        }

        /// <summary>
        /// Mark the complete ordered Step sequence as successfully executed
        /// </summary>
        private void CompleteSequence()
        {
            ReleaseCurrentStepRunner();

            status = ETutorialSequenceRunnerStatus.Completed;

            Completed?.Invoke(this);
        }

        /// <summary>
        /// Stop the sequence after a fatal execution error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool FailSequence(string error)
        {
            ReleaseCurrentStepRunner();

            lastError = error;
            status = ETutorialSequenceRunnerStatus.Failed;

            Failed?.Invoke(this, error);

            return false;
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Subscribe to terminal events emitted by the currently executed TutorialStepRunner
        /// </summary>
        private void SubscribeCurrentStepRunner()
        {
            if (currentStepRunner == null)
            {
                return;
            }

            currentStepRunner.Completed += OnCurrentStepCompleted;
            currentStepRunner.Skipped += OnCurrentStepSkipped;
        }

        /// <summary>
        /// Remove subscriptions and release the currently owned TutorialStepRunner
        /// </summary>
        private void ReleaseCurrentStepRunner()
        {
            if (currentStepRunner == null)
            {
                return;
            }

            currentStepRunner.Completed -= OnCurrentStepCompleted;
            currentStepRunner.Skipped -= OnCurrentStepSkipped;

            currentStepRunner.Dispose();
            currentStepRunner = null;
        }

        #endregion
    }
}