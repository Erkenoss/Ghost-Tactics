using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.Tutorial
{
    public class SkipTutorialButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The bubble parent use to display the informations of the current tutorial")]
        [SerializeField]
        private TutorialMessageBubble bubble = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Use to disable the button
        /// </summary>
        /// <param name="d"></param>
        protected override void DisableButton(OnDisableButton d)
        {

        }

        /// <summary>
        /// Use to Enable or disable the button
        /// </summary>
        /// <param name="e"></param>
        protected override void EnableButton(OnEnableButton e)
        {

        }

        protected override void OnClick()
        {
            if (GameManager.Instance == null || bubble == null)
            {
                return;
            }

            base.OnClick();
            GameManager.Instance.SkipTutorialStep();
        }

        #endregion
    }
}