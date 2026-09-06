using System;

namespace Tutorial.Runtime.Completion
{
    /// <summary>
    /// Base runtime condition used to determine when a tutorial Step is completed
    /// </summary>
    public abstract class TutorialCompletionCondition : IDisposable
    {
        #region Events

        /// <summary>
        /// Raised when the completion condition is satisfied
        /// </summary>
        public event Action<TutorialCompletionCondition> Completed = null;

        #endregion

        #region Private Fields

        /// <summary>
        /// Indicate whether the completion condition is currently listening for completion
        /// </summary>
        private bool isArmed = false;

        #endregion

        #region Properties

        public bool IsArmed { get { return isArmed; } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start listening for the completion condition
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool Arm(out string error)
        {
            error = string.Empty;

            if (isArmed)
            {
                return true;
            }

            isArmed = true;

            if (OnArm(out error))
            {
                return true;
            }

            Disarm();

            return false;
        }

        /// <summary>
        /// Stop listening for the completion condition
        /// </summary>
        public void Disarm()
        {
            if (!isArmed)
            {
                return;
            }

            isArmed = false;

            OnDisarm();
        }

        /// <summary>
        /// Release every resource used by the completion condition
        /// </summary>
        public void Dispose()
        {
            Disarm();

            Completed = null;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Notify that the completion condition has been satisfied
        /// </summary>
        protected void Complete()
        {
            if (!isArmed)
            {
                return;
            }

            Disarm();

            Completed?.Invoke(this);
        }

        /// <summary>
        /// Start listening for the concrete completion condition
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        protected abstract bool OnArm(out string error);

        /// <summary>
        /// Stop listening for the concrete completion condition
        /// </summary>
        protected abstract void OnDisarm();

        #endregion
    }
}