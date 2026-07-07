using UnityEngine;
using UnityEngine.UI;

namespace Crimson.Core
{
    public class UIButton : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Action will be connect with the button onClick")]
        [SerializeField]
        protected ButtonBase action = null;

        [Tooltip("Button of this script")]
        [SerializeField]
        protected Button button = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            if (button != null && action != null)
            {
                button.onClick.AddListener(action.Execute);
            }
        }

        protected virtual void OnDestroy()
        {
            if (button != null && action != null)
            {
                button.onClick.RemoveListener(action.Execute);
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}