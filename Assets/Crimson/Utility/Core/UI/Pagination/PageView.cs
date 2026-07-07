using UnityEngine;

namespace Crimson.Pagination
{
    public class PageView : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            if (PaginationManager.Instance == null)
            {
                return;
            }

            PaginationManager.Instance.SetCurrentPanel(gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Display the current page
        /// </summary>
        /// <param name="page"></param>
        public virtual void PageDisplayer(Page page)
        {

        }

        /// <summary>
        /// Method generic to apply different instructions in the differents views
        /// </summary>
        public virtual void Apply()
        {

        }

        /// <summary>
        /// Notify the PaginationManager to change the current panel
        /// </summary>
        public virtual void NotifyCurrentPanel()
        {

        }

        #endregion

        #region Private Methods
        #endregion
    }
}