using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class SkillGhost : MonoBehaviour
    {
        #region Public Fields
     
        public Image Img { get { return img; } }

        #endregion

        #region Private Fields

        [Tooltip("Image where the icon will be display")]
        [SerializeField]
        private Image img = null;

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
            ViewGameObject(false);
        }

        /// <summary>
        /// Set the Icon in the Img
        /// </summary>
        /// <param name="icon"></param>
        public void SetImage(Sprite icon)
        {
            if (img == null || icon == null)
            {
                return;
            }

            img.sprite = icon;
        }

        /// <summary>
        /// Enable or disable the gameObject container
        /// </summary>
        public void ViewGameObject(bool isOpen)
        {
            gameObject.SetActive(isOpen);
        }

        
        #endregion

        #region Private Methods
        #endregion
    }
}