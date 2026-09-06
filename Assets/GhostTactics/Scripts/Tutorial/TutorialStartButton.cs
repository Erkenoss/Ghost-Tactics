using Crimson.Core;
using Tutorial.Runtime.Flow;

namespace GhostTactics.Tutorial
{
    public class TutorialStartButton : ButtonParent
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
                gameObject.SetActive(false);
                return;
            }

            StartTutorial();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start the tutorial
        /// </summary>
        public void StartTutorial()
        {
        }

        #endregion

        #region Private Methods
        #endregion
    }
}
