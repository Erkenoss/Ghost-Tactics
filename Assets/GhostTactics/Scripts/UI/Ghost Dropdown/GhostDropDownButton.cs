using Crimson.Core;
using UnityEngine;
using System.Collections.Generic;
using GhostTactics.Core;
using GhostTactics.Data;

namespace GhostTactics.UI
{
    public class GhostDropDownButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The skillghost of the button to manage the view of the dropdown")]
        [SerializeField]
        private List<SkillGhost> skillGhosts = new List<SkillGhost>();

        [Tooltip("Animator of the dropdown")]
        [SerializeField]
        private Animator animator = null;
        
        /// <summary>
        /// Use to set the dropdown with animator
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// The currentLevel of the game
        /// </summary>
        private LevelData currentLevel = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Update the list base on the Player receive with the next level
        /// </summary>
        /// <param name="level"></param>
        private void UpdateSkillGhostList(NextLevel level)
        {
            if (level == null || level.Player == null || level.Player.PlayerGhost == null || level.Player.PlayerGhost.ActionsGhost == null || level.Player.PlayerGhost.ActionsGhost.Count == 0)
            {
                CleanGhostList();
                return;
            }

            if (skillGhosts == null || skillGhosts.Count == 0)
            {
                return;
            }

            for (int i = 0; i < skillGhosts.Count; i++)
            {
                if (i < level.Player.PlayerGhost.ActionsGhost.Count && level.Player.PlayerGhost.ActionsGhost[i] != null && level.Player.PlayerGhost.ActionsGhost[i].AbilityIcon != null)
                {
                    skillGhosts[i].SetImage(level.Player.PlayerGhost.ActionsGhost[i].AbilityIcon);
                }
            }

            currentLevel = level.Data;
        }

        /// <summary>
        /// Clean the ghost list without clear it
        /// </summary>
        private void CleanGhostList()
        {
            if (skillGhosts == null || skillGhosts.Count == 0)
            {
                return;
            }

            foreach(SkillGhost ghost in skillGhosts)
            {
                ghost.ResetSkill();
            }
        }

        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventBus.Subscribe<NextLevel>(UpdateSkillGhostList);
        }

        protected override void UnsubscribeEvent()
        {
            base.UnsubscribeEvent();

            EventBus.Unsubscribe<NextLevel>(UpdateSkillGhostList);
        }

        protected override void OnClick()
        {
            if (animator == null || skillGhosts == null || skillGhosts.Count == 0)
            {
                return;
            }

            isOpen = !isOpen;
            animator.SetBool("IsOpen", isOpen);
        }
        #endregion
    }
}