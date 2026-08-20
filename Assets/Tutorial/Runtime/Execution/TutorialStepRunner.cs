using System;
using Tutorial.Runtime.Completion;
using Tutorial.Runtime.Components;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Resolution;
using UnityEngine;

namespace Tutorial.Runtime.Execution
{
    /// <summary>
    /// Manage the runtime execution lifecycle of one tutorial StepSO
    /// </summary>
    public sealed class TutorialStepRunner : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Runtime StepSO handled by a completion condition
        /// </summary>
        private readonly StepSO runtimeStep = null;

        /// <summary>
        /// Runtime method binding associated with the StepSO handled by this runner
        /// </summary>
        private readonly TutorialResolvedMethod resolvedMethod = null;

        /// <summary>
        /// Tutorial scene identifier associated with this runner
        /// </summary>
        private readonly TutoIdentifier identifier = null;

        /// <summary>
        /// Runtime completion condition associated with this runner
        /// </summary>
        private readonly TutorialCompletionCondition completionCondition = null;

        /// <summary>
        /// Current execution status of this Step runner
        /// </summary>
        private ETutorialStepRunnerStatus status = ETutorialStepRunnerStatus.Created;

        /// <summary>
        /// Whether this runner is currently subscribed to its runtime signals
        /// </summary>
        private bool isSubscribed = false;

        /// <summary>
        /// Last error produced by this Step runner
        /// </summary>
        private string lastError = string.Empty;

        #endregion

        #region Properties

        public StepSO RuntimeStep => runtimeStep;
        public TutorialResolvedMethod ResolvedMethod => resolvedMethod;
        public TutoIdentifier Identifier => identifier;
        public TutorialCompletionCondition CompletionCondition => completionCondition;
        public ETutorialStepRunnerStatus Status => status;
        public string LastError => lastError;
        public bool IsWaiting => status == ETutorialStepRunnerStatus.WaitingForTrigger;
        public bool IsRunning => status == ETutorialStepRunnerStatus.Running;
        public bool IsCompleted => status == ETutorialStepRunnerStatus.Completed;
        public bool IsSkipped => status == ETutorialStepRunnerStatus.Skipped;
        public bool IsDisposed => status == ETutorialStepRunnerStatus.Disposed;
        public bool IsTerminal => IsCompleted || IsSkipped || IsDisposed;

        #endregion

        #region Events

        public event Action<TutorialStepRunner> Triggered = null;
        public event Action<TutorialStepRunner> Completed = null;
        public event Action<TutorialStepRunner> Skipped = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a Step runner from an already resolved runtime method binding
        /// </summary>
        /// <param name="resolvedMethod"></param>
        public TutorialStepRunner(TutorialResolvedMethod resolvedMethod)
        {
            this.resolvedMethod = resolvedMethod ?? throw new ArgumentNullException(nameof(resolvedMethod));
            identifier = resolvedMethod.Identifier ?? throw new ArgumentNullException(nameof(resolvedMethod.Identifier));
        }

        /// <summary>
        /// Create a Step runner from a runtime completion condition
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="identifier"></param>
        /// <param name="completionCondition"></param>
        public TutorialStepRunner(StepSO runtimeStep, TutoIdentifier identifier, TutorialCompletionCondition completionCondition)
        {
            this.runtimeStep = runtimeStep ?? throw new ArgumentNullException(nameof(runtimeStep));
            this.identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            this.completionCondition = completionCondition ?? throw new ArgumentNullException(nameof(completionCondition));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Prepare this Step runner and wait for its activation
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (status != ETutorialStepRunnerStatus.Created)
            {
                return false;
            }

            lastError = string.Empty;

            Subscribe();

            status = ETutorialStepRunnerStatus.WaitingForTrigger;

            return true;
        }

        /// <summary>
        /// Activate this Step runner and begin observing its completion mechanism
        /// </summary>
        /// <returns></returns>
        public bool Activate()
        {
            if (status != ETutorialStepRunnerStatus.WaitingForTrigger)
            {
                return false;
            }

            lastError = string.Empty;

            status = ETutorialStepRunnerStatus.Running;

            if (completionCondition != null && !completionCondition.Arm(out lastError))
            {
                status = ETutorialStepRunnerStatus.WaitingForTrigger;
                return false;
            }

            Triggered?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Release every signal subscription owned by this Step runner
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Unsubscribe();

            completionCondition?.Dispose();

            Triggered = null;
            Completed = null;
            Skipped = null;

            status = ETutorialStepRunnerStatus.Disposed;
        }

        #endregion

        #region Signal Handling

        /// <summary>
        /// Process one gameplay method execution signal and activate this Step when the binding matches
        /// </summary>
        /// <param name="source"></param>
        /// <param name="methodName"></param>
        private void OnMethodTriggered(MonoBehaviour source, string methodName)
        {
            if (status != ETutorialStepRunnerStatus.WaitingForTrigger || resolvedMethod == null)
            {
                return;
            }

            if (source != resolvedMethod.Script)
            {
                return;
            }

            if (!string.Equals(methodName, resolvedMethod.Method.Name, StringComparison.Ordinal))
            {
                return;
            }

            Activate();
        }

        /// <summary>
        /// Complete this Step after its associated TutoIdentifier has been raised
        /// </summary>
        private void OnRaised()
        {
            if (status != ETutorialStepRunnerStatus.Running || resolvedMethod == null)
            {
                return;
            }

            status = ETutorialStepRunnerStatus.Completed;

            Unsubscribe();

            Completed?.Invoke(this);
        }

        /// <summary>
        /// Complete this Step when its runtime completion condition has been satisfied
        /// </summary>
        /// <param name="condition"></param>
        private void OnCompletionConditionCompleted(TutorialCompletionCondition condition)
        {
            if (status != ETutorialStepRunnerStatus.Running || condition != completionCondition)
            {
                return;
            }

            status = ETutorialStepRunnerStatus.Completed;

            Unsubscribe();

            runtimeStep.OnRaised();

            Completed?.Invoke(this);
        }

        /// <summary>
        /// Skip this Step while it is waiting or currently running
        /// </summary>
        private void OnSkipped()
        {
            if (status != ETutorialStepRunnerStatus.WaitingForTrigger && status != ETutorialStepRunnerStatus.Running)
            {
                return;
            }

            status = ETutorialStepRunnerStatus.Skipped;

            Unsubscribe();

            Skipped?.Invoke(this);
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Subscribe this Step runner to the runtime signals required by its execution mode
        /// </summary>
        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            if (resolvedMethod != null)
            {
                TutorialMethodSignal.Triggered += OnMethodTriggered;
                identifier.Raised += OnRaised;
            }

            if (completionCondition != null)
            {
                completionCondition.Completed += OnCompletionConditionCompleted;
            }

            identifier.Skipped += OnSkipped;

            isSubscribed = true;
        }

        /// <summary>
        /// Remove every runtime signal subscription owned by this runner
        /// </summary>
        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (resolvedMethod != null)
            {
                TutorialMethodSignal.Triggered -= OnMethodTriggered;
                identifier.Raised -= OnRaised;
            }

            if (completionCondition != null)
            {
                completionCondition.Completed -= OnCompletionConditionCompleted;
                completionCondition.Disarm();
            }

            identifier.Skipped -= OnSkipped;

            isSubscribed = false;
        }

        #endregion
    }
}