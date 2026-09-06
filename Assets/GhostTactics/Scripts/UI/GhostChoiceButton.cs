using Crimson.Core;
using UnityEngine.UI;
using UnityEngine;
using GhostTactics.Data;

namespace GhostTactics.UI
{
    public class GhostChoiceButton : ButtonParent
    {
        #region Public Fields

        public AbilityData Data { get { return data; } }

        #endregion

        #region Private Fields

        [Tooltip("Image of the button set for the ghost we want to choose")]
        [SerializeField]
        private Image buttonImage = null;

        /// <summary>
        /// Data of the ability choosen
        /// </summary>
        private AbilityData data = null;

        /// <summary>
        /// Button that the player choose to save a ghost action
        /// </summary>
        private ChooseActionButton chooseActionButton = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Set the value of the button
        /// </summary>
        /// <param name="abilities"></param>
        /// <param name="image"></param>
        public void Set(ChooseActionButton buttonAction)
        {
            if (buttonImage == null || btn == null)
            {
                return;
            }

            data = buttonAction.Data;
            buttonImage.sprite = data.AbilityIcon;
            chooseActionButton = buttonAction;
            btn.enabled = true;
        }

        /// <summary>
        /// Reset the different value of the button, like the image and the data.
        /// </summary>
        public void Disable()
        {
            if (buttonImage == null || btn == null)
            {
                return;
            }

            data = null;
            buttonImage.sprite = null;
            btn.enabled = false;
            chooseActionButton = null;
        }

        /// <summary>
        /// Play the tutorial link with this script
        /// </summary>
        public void PlaytTuto()
        {

        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            PlaytTuto();
            EventBus.Publish<OnRemoveGhostChoice>(new OnRemoveGhostChoice(chooseActionButton));
            Disable();
        }

        #endregion
    }
}