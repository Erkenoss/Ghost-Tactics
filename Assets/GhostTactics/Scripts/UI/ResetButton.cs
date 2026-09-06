using Crimson.Core;
using GhostTactics.Core;

namespace GhostTactics.UI
{
    public class ResetButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Play the tutorial we need
        /// </summary>
        public void PLayTuto()
        {

        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            PLayTuto();
            EventBus.Publish(new ResetAll());
        }

        #endregion
    }
}