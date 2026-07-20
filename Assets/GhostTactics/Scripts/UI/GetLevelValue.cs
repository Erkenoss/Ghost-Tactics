using TMPro;
using UnityEngine;
using GhostTactics.Core;
using Crimson.Core;

namespace GhostTactics.UI
{
    public class GetLevelValue : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Text reference to display the level value")]
        [SerializeField]
        private TextMeshProUGUI text = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscride();
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
        /// Updates the level value displayed in the text component based on the provided NextLevel object.
        /// </summary>
        /// <param name="lvl"></param>
        private void UpdateLevelValue(NextLevel lvl)
        {
            if (text == null || lvl == null)
            {
                return;
            }

            text.text = lvl.Data.LevelNumber.ToString();
        }

        /// <summary>
        /// Subscribes to the NextLevel event to update the level value when the event is published.
        /// </summary>
        private void Subscride()
        {
            EventBus.Subscribe<NextLevel>(UpdateLevelValue);
        }

        /// <summary>
        /// Unsubscribes from the NextLevel event to stop updating the level value when the event is published.
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(UpdateLevelValue);
        }

        #endregion
    }
}