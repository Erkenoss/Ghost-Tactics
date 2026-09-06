using Crimson.Core;
using I2.Loc;
using Tutorial.Runtime.Activity;
using UnityEngine;

namespace GhostTactics.Tutorial
{
    public class TutorialButton : TutorialActivity
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Message we want to display in the pop up")]
        [SerializeField]
        protected LocalizedString popUpMessage = null;

        [Tooltip("Parent use to disable the button during the tutorial when OnDIsableButton is published")]
        [SerializeField]
        protected ButtonParent btn = null;

        [Tooltip("Animator of the button")]
        [SerializeField]
        protected Animator animator = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public override void Trigger()
        {

            EventBus.Publish<OnDisableButton>(new OnDisableButton(btn));
            EventBus.Publish<OnTutorialStepRequired>(new OnTutorialStepRequired(popUpMessage));

            if (animator == null)
            {
                return;
            }

            animator.SetBool("Pulse", true);
        }

        public override void Skipped()
        {
            EventBus.Publish<OnTutorialNotrequired>(new OnTutorialNotrequired());
            EventBus.Publish<OnEnableButton>(new OnEnableButton());

            if (animator == null)
            {
                return;
            }

            animator.SetBool("Pulse", false);
        }

        public override void Raised()
        {
            EventBus.Publish<OnEnableButton>(new OnEnableButton());
            EventBus.Publish<OnTutorialNotrequired>(new OnTutorialNotrequired());

            if (animator == null)
            {
                return;
            }

            animator.SetBool("Pulse", false);
        }

        #endregion

        #region Private Methods
        #endregion
    }
}