using System;
using System.Collections.Generic;
using Tutorial.Runtime.Activity;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Resolution;
using UnityEngine;

namespace Tutorial.Runtime.Components
{
    public class TutoIdentifier : MonoBehaviour
    {
        #region Public Fields

        public UnityEngine.Component TargetComponent { get { return targetComponent; } set { targetComponent = value; } }
        public string ObjectGUID => objectGUID;

        #endregion

        #region Events

        /// <summary>
        /// Component targeted by the tutorial Step
        /// </summary>
        [Tooltip("Component targeted by the tutorial Step")]
        [SerializeField]
        private UnityEngine.Component targetComponent = null;

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

        /// <summary>
        /// List of every activity manage with this tutorial step
        /// </summary>
        private List<ITutorialActivity> activities = new List<ITutorialActivity>();

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
        /// Register activity in the list activities
        /// </summary>
        /// <param name="activity"></param>
        public void ActivityRegister(ITutorialActivity activity)
        {
            if (activity == null || activities.Contains(activity))
            {
                return;
            }

            activities.Add(activity);
        }

        /// <summary>
        /// Unregsiter activity in activities
        /// </summary>
        /// <param name="activity"></param>
        public void ActivityUnregister(ITutorialActivity activity)
        {
            if (activity == null || !activities.Contains(activity))
            {
                return;
            }

            activities.Remove(activity);
        }

        /// <summary>
        /// Trigger every tutorial Activity registered on this identifier
        /// </summary>
        public void TriggerActivities()
        {
            if (activities == null || activities.Count == 0)
            {
                return;
            }

            ITutorialActivity[] registeredActivities = activities.ToArray();

            foreach (ITutorialActivity activity in registeredActivities)
            {
                activity?.Trigger();
            }
        }

        /// <summary>
        /// Raise every tutorial Activity registered on this identifier
        /// </summary>
        public void RaiseActivities()
        {
            if (activities == null || activities.Count == 0)
            {
                return;
            }

            ITutorialActivity[] registeredActivities = activities.ToArray();

            foreach (ITutorialActivity activity in registeredActivities)
            {
                activity?.Raised();
            }
        }

        /// <summary>
        /// Skip every tutorial Activity registered on this identifier
        /// </summary>
        public void SkipActivities()
        {
            if (activities == null || activities.Count == 0)
            {
                return;
            }

            ITutorialActivity[] registeredActivities = activities.ToArray();

            foreach (ITutorialActivity activity in registeredActivities)
            {
                activity?.Skipped();
            }
        }


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