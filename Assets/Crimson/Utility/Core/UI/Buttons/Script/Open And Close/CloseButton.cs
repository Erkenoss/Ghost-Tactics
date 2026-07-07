using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Crimson.Utilities
{
    public class CloseButton : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// GameObject to close
        /// </summary>
        [SerializeField]
        protected GameObject toClose = null;

        /// <summary>
        /// Close Button
        /// </summary>
        [SerializeField]
        private Button button = null;   

        /// <summary>
        /// Action when we click on the button
        /// </summary>
        private UnityAction onClickAction = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            if (button != null && toClose != null)
            {
                onClickAction = () => Execute();
                button.onClick.AddListener(onClickAction);
            }
        }

        private void OnDisable()
        {
            if (button != null && toClose != null)
            {
                button.onClick.RemoveListener(onClickAction);
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected virtual void Execute()
        {
            if (toClose != null)
            {
                toClose.SetActive(false);
            }
        }

        #endregion
    }
}
