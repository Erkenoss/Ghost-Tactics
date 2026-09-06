using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class PlayButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Indicates whether the game is starting.")]
        [SerializeField]
        private bool isStartingGame = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            base.OnClick();
            EventBus.Publish<StartGameEvent>(new StartGameEvent(isStartingGame));
        }

        #endregion
    }
}