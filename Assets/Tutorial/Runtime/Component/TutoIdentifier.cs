using System;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Resolution;
using UnityEngine;

namespace Tutorial.Runtime.Component
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

        /// <summary>
        /// Raised when one TutoIdentifier becomes available at runtime
        /// </summary>
        public static event Action<TutoIdentifier> BecameAvailable = null;

        /// <summary>
        /// Raised when one TutoIdentifier becomes unavailable at runtime
        /// </summary>
        public static event Action<TutoIdentifier> BecameUnavailable = null;

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

        #region Runtime Initialization

        /// <summary>
        /// Reset static runtime events before scene objects become available
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            BecameAvailable = null;
            BecameUnavailable = null;
        }

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            if (!TutorialIdentifierRegistry.Instance.TryRegister(this, out string error) && !string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning(error, this);
            }

            BecameAvailable?.Invoke(this);
        }

        private void OnDisable()
        {
            TutorialIdentifierRegistry.Instance.TryUnregister(this);
            BecameUnavailable?.Invoke(this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(objectGUID))
            {
                GenerateGUID();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Raise the current tutorial step or sequence
        /// </summary>
        public void Raise()
        {
            OnRaised();
        }

        /// <summary>
        /// Skip the current tutorial step or sequence
        /// </summary>
        public void Skip()
        {
            OnSkipped();
        }

        /// <summary>
        /// Trigger the current tutorial step or sequence
        /// </summary>
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