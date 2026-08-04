using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tutorial
{
    public class TutoIdentifier : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Events

        public event Action Raised = null;
        public event Action Skipped = null;
        public event Action Trigger = null;

        #endregion

        #region Private Fields

        /// <summary>
        /// Guid to link with the StepSO
        /// </summary>
        [Tooltip("Guid to link with the StepSO")]
        [SerializeField]
        private string objectGUID = string.Empty;

        /// <summary>
        /// Step we want to use with this object
        /// </summary>
        [Tooltip("Step we want to use with the object")]
        [SerializeField]
        private StepSO step = null;

        [Tooltip("Sequence we want to use with this object")]
        [SerializeField]
        private StepSequenceSO stepSequence = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Raised the step
        /// </summary>
        private void OnRaised()
        {
            if (step == null && stepSequence == null)
            {
                return;
            }

            if (step == null)
            {
                TutoEventBus.Publish<OnRaised>(new OnRaised(stepSequence));
            }
            else
            {
                TutoEventBus.Publish<OnRaised>(new OnRaised(step));
            }

        }

        /// <summary>
        /// Skipped the step
        /// </summary>
        private void OnSkipped()
        {
            if (step == null && stepSequence == null)
            {
                return;
            }

            if (step == null)
            {
                TutoEventBus.Publish<OnSkipped>(new OnSkipped(stepSequence));
            }
            else
            {
                TutoEventBus.Publish<OnSkipped>(new OnSkipped(step));
            }
        }

        /// <summary>
        /// Trigger the step
        /// </summary>
        private void OnTrigger()
        {
            if (step == null && stepSequence == null)
            {
                return;
            }

            if (step == null)
            {
                TutoEventBus.Publish<OnTrigger>(new OnTrigger(stepSequence));
            }
            else
            {
                TutoEventBus.Publish<OnTrigger>(new OnTrigger(step));
            }
        }

        #endregion
    }
}