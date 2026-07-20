using Crimson.Core;
using GhostTactics.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class ChooseActionButton : ButtonParent
    {
        #region Public Fields

        public AbilityData Data { get { return data; } }

        #endregion

        #region Private Fields

        [Tooltip("Image of the button set by the action we want to choose")]
        [SerializeField]
        private Image buttonImage = null;

        /// <summary>
        /// Data of the ability choosen
        /// </summary>
        private AbilityData data = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Set the value of the button
        /// </summary>
        /// <param name="abilities"></param>
        /// <param name="image"></param>
        public void Set(AbilityData data)
        {
            if (buttonImage == null || btn == null)
            {
                return;
            }

            this.data = data;
            buttonImage.sprite = data.AbilityIcon;
            btn.enabled = true;
        }

        public void RemoveGhostChoice()
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = true;
        }

        /// <summary>
        /// Reset the different value of the button, like the image and the data.
        /// </summary>
        public void Disable()
        {
            if (buttonImage == null)
            {
                return;
            }

            data = null;
            buttonImage.sprite = null;
        }

        /// <summary>
        /// Disable the button
        /// </summary>
        public void DisableButton()
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = false;
        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            EventBus.Publish<OnChooseForGhost>(new OnChooseForGhost(this));
        }

        #endregion
    }
}