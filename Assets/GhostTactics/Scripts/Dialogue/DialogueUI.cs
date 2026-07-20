using GhostTactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class DialogueUI : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The image where the player will be display")]
        [SerializeField]
        private Image playerPortrait = null;

        [Tooltip("The other character wwill be display here")]
        [SerializeField]
        private Image otherPortrait = null;

        [Tooltip("Where the dialogue will be set")]
        [SerializeField]
        private TextMeshProUGUI dialogueText = null;

        /// <summary>
        /// Current level actualy display
        /// </summary>
        private LevelData currentData = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Update the view base on the current level
        /// </summary>
        public void UpdateView(LevelData data)
        {
            if (data == null)
            {
                return;
            }

            currentData = data;

            Debug.Log("Here");
        }

        #endregion

        #region Private Methods
        #endregion
    }
}