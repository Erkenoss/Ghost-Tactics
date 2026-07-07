using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class ConfirmButton : ButtonParent
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
            EventBus.Publish<ConfirmTry>(new ConfirmTry());
        }

        #endregion
    }
}