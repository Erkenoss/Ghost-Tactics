using UnityEngine;
using UnityEngine.UI;

namespace Crimson.Core
{
    public abstract class ButtonParent : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Button of this script")]
        [SerializeField]
        protected Button btn = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Start()
        {
            if (btn == null)
            {
                return;
            }
         
            btn.onClick.AddListener(OnClick);
        }

        protected virtual void OnDestroy()
        {
            if (btn == null)
            {
                return;
            }

            btn.onClick.RemoveListener(OnClick);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Where the player push the button
        /// </summary>
        protected virtual void OnClick()
        {

        }
        
        #endregion
    }
}