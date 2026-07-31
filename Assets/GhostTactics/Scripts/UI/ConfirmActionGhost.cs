using Crimson.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class ConfirmActionGhost : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Player die UI reference")]
        [SerializeField]
        private PlayerDieUI playerDie = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            if (playerDie == null)
            {
                return;
            }

            base.OnClick();

            playerDie.SaveGhostList();

        }

        #endregion
    }
}