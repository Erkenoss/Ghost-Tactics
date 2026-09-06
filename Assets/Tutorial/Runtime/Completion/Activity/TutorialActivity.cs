using Tutorial.Runtime.Components;
using UnityEngine;

namespace Tutorial.Runtime.Activity
{
    public abstract class TutorialActivity : MonoBehaviour, ITutorialActivity
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Identifier of this activity to manage tutorial step accomplishment")]
        [SerializeField]
        private TutoIdentifier targetIdentifier = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void OnEnable()
        {
            if (targetIdentifier == null)
            {
                return;
            }

            targetIdentifier.ActivityRegister(this);
        }

        protected virtual void OnDisable()
        {
            if (targetIdentifier == null)
            {
                return;
            }

            targetIdentifier.ActivityUnregister(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Use when the step is triggered
        /// </summary>
        public virtual void Trigger()
        {

        }

        /// <summary>
        /// Use when the Skipped is triggered
        /// </summary>
        public virtual void Skipped()
        { 
        
        }

        /// <summary>
        /// Use when the Raised is triggered at the end of the tutorial
        /// </summary>
        public virtual void Raised()
        {

        }

        #endregion

        #region Private Methods
        #endregion
    }
}
