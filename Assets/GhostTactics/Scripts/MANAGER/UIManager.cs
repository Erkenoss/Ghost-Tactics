using Crimson.Core;
using UnityEngine.UI;

namespace GhostTactics.Core
{
    public class UIManager : Singleton<UIManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// background image of the game scene, use to change the background of the game scene when the player is in a level
        /// </summary>
        private Image backgroundGameSceneImage = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the background image of the game scene with the new image
        /// </summary>
        /// <param name="image"></param>
        public void UpdateImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            backgroundGameSceneImage = image;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update the background sprite of the game scene with the new sprite from the level data
        /// </summary>
        /// <param name="lvl"></param>
        private void UpdateBackgroundSprite(NextLevel lvl)
        {
            if (backgroundGameSceneImage == null)
            {
                return;
            }

            backgroundGameSceneImage.sprite = lvl.Data.LevelImage;
        }

        /// <summary>
        /// Subscribe the differents event with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(UpdateBackgroundSprite);
        }

        /// <summary>
        /// Unsubscribe the differents event with the EventBus to avoid memory leaks and unwanted behavior
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(UpdateBackgroundSprite);
        }

        #endregion
    }
}