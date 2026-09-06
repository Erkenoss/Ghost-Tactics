using Tutorial.Runtime.Flow;
using UnityEngine;

namespace GhostTactics.Tutorial
{
    public class GhostButtonTutorial : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        
        private void Start()
        {
            if (TutorialFlowController.Instance == null || TutorialFlowController.Instance.Runner != null && TutorialFlowController.Instance.Runner.IsCompleted)
            {
                return;
            }

            PlayTuto();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Play the tutorial for the ghost button
        /// </summary>
        public void PlayTuto()
        {
        }

        #endregion

        #region Private Methods
        #endregion
    }
}