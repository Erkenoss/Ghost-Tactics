using UnityEngine;
using System.Collections.Generic;

namespace Tutorial
{
    [CreateAssetMenu(fileName = "Sequence", menuName = "Tutorial/Sequence")]
    public class StepSequenceSO : StepSO
    {
        #region Public Fields

        public ESequenceType SequenceType { get { return sequenceType; } set { sequenceType = value; } }
        public List<StepSO> SequenceSOList { get { return sequenceSOList; } set { sequenceSOList = value; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Type of the sequence
        /// </summary>
        [Tooltip("Type of the sequence")]
        [SerializeField]
        private ESequenceType sequenceType = ESequenceType.none;

        /// <summary>
        /// List of differents step to manage a list of step
        /// </summary>
        [Tooltip("List of differents step to manage a list of step")]
        [SerializeField]
        private List<StepSO> sequenceSOList = new List<StepSO>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public override void OnRaised()
        {
            TutoEventBus.Publish<OnRaised>(new OnRaised(this));
        }

        public override void OnSkipped()
        {
            TutoEventBus.Publish<OnSkipped>(new OnSkipped(this));
        }

        public override void OnTrigger()
        {
            TutoEventBus.Publish<OnTrigger>(new OnTrigger(this));
        }

        /// <summary>
        /// Raise the step given
        /// </summary>
        /// <param name="index"></param>
        public void RaiseByStep(StepSO step)
        {
            if (sequenceSOList == null || sequenceSOList.Count == 0 || !sequenceSOList.Contains(step))
            {
                return;
            }

            sequenceSOList.Find(s => s == step).OnRaised();
        }

        /// <summary>
        /// Skipped the step given
        /// </summary>
        /// <param name="step"></param>
        public void SkippedByStep(StepSO step)
        {
            if (sequenceSOList == null || sequenceSOList.Count == 0 || !sequenceSOList.Contains(step))
            {
                return;
            }

            sequenceSOList.Find(s => s == step).OnSkipped();
        }

        /// <summary>
        /// Triggered the step given
        /// </summary>
        /// <param name="step"></param>
        public void TriggeredByStep(StepSO step)
        {
            if (sequenceSOList == null || sequenceSOList.Count == 0 || !sequenceSOList.Contains(step))
            {
                return;
            }

            sequenceSOList.Find(s => s == step).OnTrigger();
        }

        /// <summary>
        /// Raised a step by int
        /// </summary>
        /// <param name="index"></param>
        public void RaisedByInt(int index)
        {
            if (sequenceSOList == null || sequenceSOList.Count == 0 || sequenceSOList.Count <= index)
            {
                return;
            }

            StepSO step = sequenceSOList[index];
            
            if (step == null)
            {
                return;
            }

            step.OnRaised();
        }

        /// <summary>
        /// Skipped a step by int
        /// </summary>
        /// <param name="index"></param>
        public void SkippedByInt(int index)
        {
            if (sequenceSOList == null || sequenceSOList.Count == 0 || sequenceSOList.Count <= index)
            {
                return;
            }

            StepSO step = sequenceSOList[index];

            if (step == null)
            {
                return;
            }

            step.OnSkipped();
        }

        /// <summary>
        /// Trigger a step by int
        /// </summary>
        /// <param name="index"></param>
        public void TriggeredByInt(int index)
        {
            if (sequenceSOList == null || sequenceSOList.Count == 0 || sequenceSOList.Count <= index)
            {
                return;
            }


            StepSO step = sequenceSOList[index];

            if (step == null)
            {
                return;
            }

            step.OnTrigger();
        }

        /// <summary>
        /// Replace the current sequence with the given ordered steps
        /// </summary>
        /// <param name="steps"></param>
        public void SetSequence(IEnumerable<StepSO> steps)
        {
            sequenceSOList ??= new List<StepSO>();
            sequenceSOList.Clear();

            if (steps == null)
            {
                return;
            }

            foreach (StepSO step in steps)
            {
                if (step == null ||
                    step == this)
                {
                    continue;
                }

                sequenceSOList.Add(step);
            }
        }

        #endregion

        #region Private Methods
        #endregion
    }
}