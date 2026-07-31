using Crimson.Core;
using Crimson.Core.Scenes;
using UnityEngine;

namespace GhostTactics.UI
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
        #endregion

        #region Private Methods

        protected override void SubscribeEvent()
        {
            
        }

        protected override void UnsubscribeEvent()
        {
            
        }

        protected override void OnClick()
        {
            base.OnClick();
            EventBus.Publish<OnSceneToLoad>(new OnSceneToLoad(mainMenuGroup));
        }

        #endregion
    }
}