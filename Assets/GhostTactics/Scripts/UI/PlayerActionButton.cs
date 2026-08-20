using Crimson.Core;
using GhostTactics.Data;
using UnityEngine;
using GhostTactics.Core;

namespace GhostTactics.UI
{
    public class PlayerActionButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Ability of the button")]
        [SerializeField]
        private AbilityData data = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public void PlayTuto()
        {
            Debug.Log("PLAY TUTO => PlayerActionButton");
        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            PlayTuto();
            EventBus.Publish(new AbilityChoice(data));
        }

        #endregion
    }
}