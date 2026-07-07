using UnityEngine;
using UnityEngine.UI;
using Crimson.Core;
using GhostTactics.Data;
using System;
using UnityEditor.EditorTools;

namespace GhostTactics.UI
{
    public class ButtonActionSelected : ButtonParent
    {
        #region Public Fields

        public AbilityData CurrentData { get { return currentData; } }

        #endregion

        #region Private Fields

        [Tooltip("Image of the button")]
        [SerializeField]
        private Image buttonImage = null;

        /// <summary>
        /// Current data of the button.
        /// </summary>
        private AbilityData currentData = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Reset the value of the button
        /// </summary>
        public void ResetButton()
        {
            if(buttonImage == null)
            {
                return;
            }

            buttonImage.sprite = null;
            currentData = null;
        }

        /// <summary>
        /// Update the button base on data, if the data is null or the same as current data, it will do nothing.
        /// </summary>
        /// <param name="data"></param>
        public void UpdateButton(AbilityData data)
        {
            if (data == null || currentData == data)
            {
                return;
            }

            currentData = data;
            UpdateView();
        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            ResetButton();
        }

        /// <summary>
        /// Update the view of the button
        /// </summary>
        private void UpdateView()
        {
            if (buttonImage == null || currentData.AbilityIcon == null)
            {
                return;
            }

            buttonImage.sprite = currentData.AbilityIcon;
        }

        #endregion
    }
}