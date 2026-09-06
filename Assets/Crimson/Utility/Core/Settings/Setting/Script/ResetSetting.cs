using UnityEngine;

namespace Crimson.Core.Settings
{
    public class ResetSetting : ButtonParent
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
            EventBus.Publish<OnResetSetting>(new OnResetSetting());
        }

        #endregion
    }
}