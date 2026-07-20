using Crimson.Core;
using GhostTactics.Core;
using TMPro;
using UnityEngine;

namespace GhostTactics.UI
{
    public class GetPrevisualizationValue : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Text reference")]
        [SerializeField]
        private TextMeshProUGUI text = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Update the value of the text
        /// </summary>
        /// <param name="lvl"></param>
        private void UpdateTextValue(NextLevel lvl)
        {
            if (text == null || lvl == null)
            {
                return;
            }

            text.text = lvl.Player.VisualizationValue.ToString();
        }

        /// <summary>
        /// Subscribe with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(UpdateTextValue);
        }

        /// <summary>
        /// Unsubsscribe with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(UpdateTextValue);
        }

        #endregion
    }
}