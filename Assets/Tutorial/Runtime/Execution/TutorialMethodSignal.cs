using System;
using UnityEngine;

namespace Tutorial.Runtime.Execution
{
    /// <summary>
    /// Notify the tutorial runtime when a monitored gameplay method is executed
    /// </summary>
    public static class TutorialMethodSignal
    {
        #region Events

        public static event Action<MonoBehaviour, string> Triggered = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Notify the tutorial runtime that one gameplay method has been executed
        /// </summary>
        /// <param name="source"></param>
        /// <param name="methodName"></param>
        public static void Raise(MonoBehaviour source, string methodName)
        {
            if (source == null || string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            Triggered?.Invoke(source, methodName);
        }

        #endregion
    }
}