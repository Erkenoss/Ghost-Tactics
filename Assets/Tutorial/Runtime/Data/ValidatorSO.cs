using UnityEngine;

namespace Tutorial.Runtime.Data
{
    public abstract class ValidatorSO : ScriptableObject
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Raised a StepSO
        /// </summary>
        public virtual void OnRaised()
        {

        }
        
        /// <summary>
        /// Skipped a StepSO
        /// </summary>
        public virtual void OnSkipped()
        {

        }

        /// <summary>
        /// Trigger a StepSO
        /// </summary>
        public virtual void OnTrigger()
        {

        }

        #endregion

        #region Private Methods
        #endregion
    }
}