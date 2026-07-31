using GhostTactics.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class SkillGhost : MonoBehaviour
    {
        #region Public Fields
     
        public Abilities GhostAbility { get { return ghostAbility; } }

        #endregion

        #region Private Fields

        [Tooltip("Image where the icon will be display")]
        [SerializeField]
        private Image img = null;

        /// <summary>
        /// Ability of the ghost givven on this slot
        /// </summary>
        private Abilities ghostAbility = Abilities.none;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Reset the view of this SkillGhost
        /// </summary>
        public void ResetSkill()
        {
            if (img == null)
            {
                return;
            }

            img.sprite = null;
            ghostAbility = Abilities.none;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Set the Icon in the Img
        /// </summary>
        /// <param name="icon"></param>
        public void SetImage(Sprite icon, Abilities a)
        {
            if (img == null || icon == null)
            {
                return;
            }

            img.sprite = icon;
            ghostAbility = a;
            gameObject.SetActive(true);
        }
        
        #endregion

        #region Private Methods
        #endregion
    }
}