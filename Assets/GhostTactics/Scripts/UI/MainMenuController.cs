using Crimson.Core;
using Crimson.Core.Audio;
using UnityEngine;

namespace GhostTactics.UI
{
    public class MainMenuController : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Context for the music")]
        [SerializeField]
        private EMusicContext musicContext = EMusicContext.None;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            EventBus.Publish<OnNewMusicContainer>(new OnNewMusicContainer(musicContext));
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}