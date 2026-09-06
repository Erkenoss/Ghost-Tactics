using Crimson.Core;
using Crimson.Core.Scenes;
using Tutorial.Runtime.Flow;
using UnityEngine;

namespace GhostTactics.Tutorial
{
    public class BackMainMenu : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The main menu group to load the scene")]
        [SerializeField]
        private SceneGroupSO mainMenuGroup = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        
        public void PlayTuto()
        {
        }
        
        #endregion

        #region Private Methods

        protected override void SubscribeEvent()
        {
            
        }

        protected override void UnsubscribeEvent()
        {
            
        }

        /// <summary>
        /// Check if the tutorial is enable or not
        /// </summary>
        /// <returns></returns>
        private bool IsTutorialEnable()
        {
            if (TutorialFlowController.Instance == null || TutorialFlowController.Instance.Runner == null)
            {
                Debug.Log("here");
                return false;
            }

            return !TutorialFlowController.Instance.Runner.IsCompleted;
        }

        protected override void OnClick()
        {
            base.OnClick();
            PlayTuto();
            EventBus.Publish<OnSceneToLoad>(new OnSceneToLoad(mainMenuGroup));
        }

        #endregion
    }
}