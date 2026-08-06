using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Tutorial
{
    public class TutoIdentifier : MonoBehaviour
    {
        #region Public Fields

        public string ObjectGUID => objectGUID;

        #endregion

        #region Events

        /// <summary>
        /// Action raised when the step or the sequence is finished
        /// </summary>
        public event Action Raised = null;

        /// <summary>
        /// Action trigger when the player skipped the tutorial
        /// </summary>
        public event Action Skipped = null;

        /// <summary>
        /// Action use when to start the tutorial
        /// </summary>
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

        /// <summary>
        /// Sequence of Tutorial we want to start here
        /// </summary>
        [Tooltip("Sequence of Tutorial we want to start here")]
        [SerializeField]
        private StepSequenceSO stepSequence = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(objectGUID))
            {
                GenerateGUID();
            }
        }

        #endregion

        #region Public Methods

        public void Raise()
        {
            OnRaised();
        }

        public void Skip()
        {
            OnSkipped();
        }

        public void TriggerStep()
        {
            OnTrigger();
        }

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

            Raised?.Invoke();

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

            Skipped?.Invoke();

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

            Trigger?.Invoke();

            if (step == null)
            {
                TutoEventBus.Publish<OnTrigger>(new OnTrigger(stepSequence));
            }
            else
            {
                TutoEventBus.Publish<OnTrigger>(new OnTrigger(step));
            }
        }

        /// <summary>
        /// Use to generate the GUID of the object
        /// </summary>
        [ContextMenu("Regenerate GUID")]
        private void GenerateGUID()
        {
            objectGUID = Guid.NewGuid().ToString("N");
        }

        #endregion
    }
}