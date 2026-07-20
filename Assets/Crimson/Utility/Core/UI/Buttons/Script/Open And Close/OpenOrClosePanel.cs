using Crimson.Core;
using UnityEngine;

namespace Crimson.Utilities
{
    public class OpenOrClosePanel : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Panel we want to manage")]
        [SerializeField]
        private GameObject panelToCloseOrOpen = null;

        [Tooltip("If true, use to open. If false, use to close")]
        [SerializeField]
        private bool toOpen = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            if (panelToCloseOrOpen == null)
            {
                return;
            }
        
            if (toOpen)
            {
                panelToCloseOrOpen.SetActive(true);
            }
            else
            {
                panelToCloseOrOpen.SetActive(false);
            }
        }

        #endregion
    }
}