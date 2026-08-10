using System;
using Tutorial.Runtime.Resolution;
using UnityEngine;

namespace Tutorial.Runtime.Execution
{
    /// <summary>
    /// Manage the runtime execution lifecycle of one resolved tutorial StepSO
    /// </summary>
    public sealed class TutorialStepRunner : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Runtime method binding associated with the StepSO handled by this runner
        /// </summary>
        private readonly TutorialResolvedMethod resolvedMethod = null;

        /// <summary>
        /// Current execution status of this Step runner
        /// </summary>
        private ETutorialStepRunnerStatus status = ETutorialStepRunnerStatus.Created;

        /// <summary>
        /// Whether this runner is currently subscribed to its runtime signals
        /// </summary>
        private bool isSubscribed = false;

        #endregion

        #region Properties

        public TutorialResolvedMethod ResolvedMethod => resolvedMethod;
        public ETutorialStepRunnerStatus Status => status;
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
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Activate this Step runner and begin waiting for its gameplay method trigger
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (status != ETutorialStepRunnerStatus.Created)
            {
                return false;
            }

            Subscribe();

            status = ETutorialStepRunnerStatus.WaitingForTrigger;

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

            Triggered = null;
            Completed = null;
            Skipped = null;

            status = ETutorialStepRunnerStatus.Disposed;
        }

        #endregion

        #region Signal Handling

        /// <summary>
        /// Process one gameplay method execution signal and trigger this Step when the binding matches
        /// </summary>
        /// <param name="source"></param>
        /// <param name="methodName"></param>
        private void OnMethodTriggered(MonoBehaviour source, string methodName)
        {
            if (status != ETutorialStepRunnerStatus.WaitingForTrigger)
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

            status = ETutorialStepRunnerStatus.Running;

            Triggered?.Invoke(this);
        }

        /// <summary>
        /// Complete this Step after its associated TutoIdentifier has been raised
        /// </summary>
        private void OnRaised()
        {
            if (status != ETutorialStepRunnerStatus.Running)
            {
                return;
            }

            status = ETutorialStepRunnerStatus.Completed;

            Unsubscribe();

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
        /// Subscribe this Step runner to the gameplay and tutorial object signals it requires
        /// </summary>
        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            TutorialMethodSignal.Triggered += OnMethodTriggered;

            resolvedMethod.Identifier.Raised += OnRaised;
            resolvedMethod.Identifier.Skipped += OnSkipped;

            isSubscribed = true;
        }

        /// <summary>
        /// Remove every gameplay and tutorial object signal subscription owned by this runner
        /// </summary>
        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            TutorialMethodSignal.Triggered -= OnMethodTriggered;

            if (resolvedMethod.Identifier != null)
            {
                resolvedMethod.Identifier.Raised -= OnRaised;
                resolvedMethod.Identifier.Skipped -= OnSkipped;
            }

            isSubscribed = false;
        }

        #endregion
    }
}