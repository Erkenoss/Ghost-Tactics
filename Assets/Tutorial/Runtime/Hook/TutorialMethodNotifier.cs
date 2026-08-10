using System;

namespace Tutorial.Runtime.Hooks
{
    /// <summary>
    /// Runtime bridge between injected gameplay methods and the Tutorial system
    /// </summary>
    public static class TutorialMethodNotifier
    {
        #region Events

        public static event Action<string> Triggered = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Notify the Tutorial system that the method linked to a Step has been executed
        /// </summary>
        /// <param name="stepGUID">GUID of the StepSO linked to the executed method</param>
        public static void Notify(string stepGUID)
        {
            if (string.IsNullOrWhiteSpace(stepGUID))
            {
                return;
            }

            Triggered?.Invoke(stepGUID);
        }

        #endregion
    }
}