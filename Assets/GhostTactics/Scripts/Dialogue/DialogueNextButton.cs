using Crimson.Core;
using GhostTactics.UI;
using Tutorial.Runtime.Flow;

namespace GhostTactics.Core.Dialogue
{
    public class DialogueNextButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        protected override void Start()
        {
            base.Start();
            
            if (TutorialFlowController.Instance == null || TutorialFlowController.Instance.Runner != null && TutorialFlowController.Instance.Runner.IsCompleted)
            {
                return;
            }

            StartTuto();
        }

        #endregion

        #region Public Methods

        public void StartTuto()
        {

        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            EventBus.Publish<OnNextLine>(new OnNextLine());
        }

        #endregion
    }
}