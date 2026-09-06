using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class ConfirmButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("ANimator to manage the pulse action animation")]
        [SerializeField]
        private Animator animator = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Check if the button can be used
        /// </summary>
        /// <param name=""></param>
        private void EnableConfirmButton(OnTimelineChecked check)
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = check.Check;
            btn.interactable = check.Check;

            if (animator == null)
            {
                return;
            }

            animator.SetBool("Pulse", check.Check);
        }

        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventBus.Subscribe<OnTimelineChecked>(EnableConfirmButton);
        }

        protected override void UnsubscribeEvent()
        {
            base.UnsubscribeEvent();

            EventBus.Unsubscribe<OnTimelineChecked>(EnableConfirmButton);
        }

        protected override void OnClick()
        {
            EventBus.Publish<ConfirmTry>(new ConfirmTry());
        }

        #endregion
    }
}