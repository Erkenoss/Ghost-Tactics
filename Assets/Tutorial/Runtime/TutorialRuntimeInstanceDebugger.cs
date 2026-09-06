using Tutorial.Runtime.Core;
using UnityEngine;

namespace Tutorial.Runtime.Components
{
    /// <summary>
    /// Exposes the active tutorial runtime instance to its custom Inspector
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialRuntimeInstanceDebugger : MonoBehaviour
    {
        #region Private Fields

        /// <summary>
        /// Runtime tutorial instance currently inspected
        /// </summary>
        private TutorialRuntimeInstance runtimeInstance = null;

        #endregion

        #region Properties

        public bool HasRuntimeInstance => runtimeInstance != null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Display the reconstructed runtime graph inside the Unity Console
        /// </summary>
        public void LogRuntimeGraph()
        {
            if (runtimeInstance == null)
            {
                Debug.LogWarning("No tutorial runtime instance is currently bound to the debugger.", this);

                return;
            }

            runtimeInstance.DebugLogRuntimeGraph();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Bind the runtime tutorial instance inspected by this component
        /// </summary>
        /// <param name="runtimeInstance"></param>
        internal void Bind(TutorialRuntimeInstance runtimeInstance)
        {
            this.runtimeInstance = runtimeInstance;
        }

        /// <summary>
        /// Remove the currently inspected runtime tutorial instance
        /// </summary>
        internal void Unbind()
        {
            runtimeInstance = null;
        }

        #endregion
    }
}