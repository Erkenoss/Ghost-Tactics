using Tutorial.Runtime.Flow;
using UnityEngine;

namespace Crimson.Core.Settings
{
    public class ResetTutorialButton : ButtonParent
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
            if (TutorialFlowController.Instance == null)
            {
                return;
            }

            TutorialFlowController.Instance.ResetAllTutorialProgress();
        }

        #endregion
    }
}