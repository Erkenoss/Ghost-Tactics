using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.Tutorial
{
    public class TutorialMessageBubble : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields  

        [Tooltip("The bubble gameObject")]
        [SerializeField]
        private GameObject toManage = null;

        [Tooltip("Text where the information will be display")]
        [SerializeField]
        private TextMeshProUGUI text = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Close and reset the current bubble
        /// </summary>
        public void CloseBubble()
        {
            if (text == null || toManage == null)
            {
                return;
            }

            text.text = string.Empty;
            toManage.SetActive(false);
        }

        /// <summary>
        /// When the tutorial want to open a bubble
        /// </summary>
        /// <param name="message"></param>
        public void OpenCloseBubble(string message)
        {
            if (string.IsNullOrEmpty(message) || text == null || toManage == null || message == null)
            {
                CloseBubble();
                return;
            }

            text.text = message;
            toManage.SetActive(true);
        }

        #endregion

        #region Private Methods
        #endregion
    }
}