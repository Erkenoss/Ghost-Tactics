using Crimson.Core;
using GhostTactics.Data;
using UnityEngine;
using GhostTactics.Core;
using Tutorial.Runtime.Flow;

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

        protected override void Start()
        {
            base.Start();

            if (TutorialFlowController.Instance == null || TutorialFlowController.Instance.Runner != null && TutorialFlowController.Instance.Runner.IsCompleted)
            {
                return;
            }

            PlayTuto();
        }

        #endregion

        #region Public Methods

        public void PlayTuto()
        {
        }

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            EventBus.Publish(new AbilityChoice(data));
        }

        #endregion
    }
}