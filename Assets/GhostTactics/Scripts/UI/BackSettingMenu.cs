using Crimson.Core;
using Crimson.Core.Settings;
using Crimson.Utilities;

namespace GhostTactics.UI
{
    public class BackSettingMenu : OpenOrClosePanel
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

            EventBus.Publish<SaveSetting>(new SaveSetting());
        }

        #endregion
    }
}