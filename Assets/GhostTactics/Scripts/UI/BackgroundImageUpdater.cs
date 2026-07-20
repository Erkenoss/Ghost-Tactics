using GhostTactics.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class BackgroundImageUpdater : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Background image of the game scene, use to change the background of the game scene when the player is in a level")]
        [SerializeField]
        private Image backgroundImage = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            if (backgroundImage == null || UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.UpdateImage(backgroundImage);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}