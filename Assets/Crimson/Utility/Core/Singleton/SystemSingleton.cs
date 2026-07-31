using UnityEngine;


namespace Crimson.Core
{
    /// <summary>
    /// Class for Event in the EventBus to manage the focus of the application
    /// </summary>
    public class OnApplicationSuspensionChanged
    {
        public bool IsSuspended = false;

        public OnApplicationSuspensionChanged(bool isSuspended)
        {
            IsSuspended = isSuspended;
        }
    }

    public abstract class SystemSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Use to manage the focus of the application
        /// </summary>
        private bool hasFocus = true;

        /// <summary>
        /// Use to manage the pause of the application
        /// </summary>
        private bool isSystemPaused = false;

        /// <summary>
        /// Get the value of hasFocus of isSystemPause to manage the application with the exactly same result
        /// </summary>
        private bool isapplicationSuspended => !hasFocus || isSystemPaused;

        /// <summary>
        /// Use to know the preivous state of the pause or focus application
        /// </summary>
        private bool previousSuspensionState = false;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        protected virtual void OnApplicationFocus(bool _hasFocus)
        {
            hasFocus = _hasFocus;
            RefreshSuspensionState();
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            isSystemPaused = pauseStatus;
            RefreshSuspensionState();
        }

        protected virtual void OnDestroy()
        {
            UnSubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// USe to manage the application pause or focus
        /// </summary>
        private void RefreshSuspensionState()
        {
            bool isSuspended = !hasFocus || isSystemPaused;

            if (isSuspended == previousSuspensionState)
            {
                return;
            }

            previousSuspensionState = isSuspended;

            EventBus.Publish(new OnApplicationSuspensionChanged(isSuspended));
        }

        /// <summary>
        /// Subscribe with the EventBus
        /// </summary>
        protected virtual void Subscribe()
        {

        }

        /// <summary>
        /// unsubscfribe with the EventBus
        /// </summary>
        protected virtual void UnSubscribe()
        {

        }

        #endregion
    }
}