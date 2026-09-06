using Crimson.Core;
using UnityEngine;
using System.Collections.Generic;
using GhostTactics.Core;
using GhostTactics.Data;

namespace GhostTactics.UI
{
    public class OnUpdateDropdown
    {
        public List<AbilityData> GhostList = new List<AbilityData>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ghostList"></param>
        public OnUpdateDropdown(List<AbilityData> ghostList)
        {
            GhostList = ghostList;
        }
    }


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

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public void PlayTuto()
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update the list base on the Player receive with the next level
        /// </summary>
        /// <param name="level"></param>
        private void UpdateSkillGhostList(OnUpdateDropdown level)
        {
            if (level?.GhostList == null || skillGhosts == null || skillGhosts.Count == 0)
            {
                if (btn != null)
                {
                    btn.enabled = false;
                    btn.interactable = false;
                }

                if (animator != null)
                {
                    animator.SetBool("IsOpen", false);
                }

                return;
            }

            if (btn != null)
            {
                btn.enabled = true;
                btn.interactable = true;
            }

            for (int i = 0; i < skillGhosts.Count && i < level.GhostList.Count; i++)
            {
                if (skillGhosts[i] != null && level.GhostList[i]?.AbilityIcon != null)
                {
                    skillGhosts[i].SetImage(level.GhostList[i].AbilityIcon, level.GhostList[i].Ability);
                }
                else
                {
                    skillGhosts[i].ResetSkill();
                }
            }
        }

        /// <summary>
        /// When the player use a ghost action, reset the GhostSkill with this ability
        /// </summary>
        /// <param name="action"></param>
        private void GhostUseAction(OnGhostUseAction action)
        {
            if (skillGhosts == null || skillGhosts.Count == 0)
            {
                return;
            }

            int i = 0;

            foreach (SkillGhost skill in skillGhosts)
            {
                if (skill.GhostAbility == action.Data.Ability)
                {
                    skill.ResetSkill();
                }

                if (skill.GhostAbility == Abilities.none)
                {
                    i++;
                }
            }

            if (i == skillGhosts.Count && btn != null)
            {
                btn.enabled = false;
                btn.interactable = false;

                if (animator != null)
                {
                    animator.SetBool("IsOpen", false);
                }
            }
        }

        protected override void SubscribeEvent()
        {
            EventBus.Subscribe<OnUpdateDropdown>(UpdateSkillGhostList);
            EventBus.Subscribe<OnGhostUseAction>(GhostUseAction);
        }

        protected override void UnsubscribeEvent()
        {
            EventBus.Unsubscribe<OnUpdateDropdown>(UpdateSkillGhostList);
            EventBus.Unsubscribe<OnGhostUseAction>(GhostUseAction);
        }

        protected override void OnClick()
        {
            if (animator == null)
            {
                return;
            }

            isOpen = !isOpen;
            animator.SetBool("IsOpen", isOpen);
            PlayTuto();
        }

        #endregion
    }
}