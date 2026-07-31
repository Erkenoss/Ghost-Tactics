using UnityEngine;

namespace Crimson.Core
{
    public class QuitApplicationButton : ButtonParent
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

            Application.Quit();
        }

        #endregion
    }
}