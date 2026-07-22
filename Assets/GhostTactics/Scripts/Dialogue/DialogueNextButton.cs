using Crimson.Core;
using GhostTactics.UI;
using UnityEngine;

namespace GhostTactics.Core.Dialogue
{
    public class DialogueNextButton : ButtonParent
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
            EventBus.Publish<OnNextLine>(new OnNextLine());
        }

        #endregion
    }
}