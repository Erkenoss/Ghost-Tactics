using Crimson.Core;
using GhostTactics.Core;

namespace GhostTactics.UI
{
    public class StartButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            base.OnClick();
            EventBus.Publish<StartGameEvent>(new StartGameEvent());
        }

        #endregion
    }
}