using Crimson.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Crimson.Pagination
{
    public class PaginationManager : Singleton<PaginationManager>
    {
        #region Public Fields

        public List<Page> PaginationList { get { return paginationList; } set { paginationList = value; } }
        public AllPages Pages { get { return pages; } }

        #endregion

        #region Private Fields

        [Tooltip("Is this manager loop on the list of Page or not?")]
        [SerializeField]
        protected bool isLoop = false;

        [Tooltip("All page container to the list of page")]
        [SerializeField]
        protected AllPages pages = null;

        [Tooltip("Next button of the pagination System")]
        [SerializeField]
        protected Button nextButton = null;

        [Tooltip("Prev button of the pagination System")]
        [SerializeField]
        protected Button prevButton = null;

        /// <summary>
        /// Current panel actually active
        /// </summary>
        protected GameObject currentPanel = null;

        /// <summary>
        /// View to display the differents pages
        /// </summary>
        protected PageView view = null;

        /// <summary>
        /// Current list where we iterate
        /// </summary>
        protected List<Page> paginationList = new List<Page>();

        /// <summary>
        /// Current page we want to display
        /// </summary>
        protected Page currentPage = null;

        /// <summary>
        /// Current index to display the currentPage
        /// </summary>
        protected int index = 0;

        /// <summary>
        /// Dictionary to stock every panel in the pagination
        /// </summary>
        protected Dictionary<string, GameObject> panelDictionary = new Dictionary<string, GameObject>();

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            if (paginationList != null && paginationList.Count > 0)
            {
                currentPage = paginationList[0];

                StartCoroutine(WaitForView());
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the current panel by panel
        /// </summary>
        /// <param name="panel"></param>
        public virtual void SetCurrentPanel(GameObject panel)
        {
            currentPanel = panel;
        }

        /// <summary>
        /// Generic method use to have differrent effect
        /// </summary>
        public virtual void Apply()
        {

        }

        /// <summary>
        /// Change page variable
        /// </summary>
        /// <param name="pag"></param>
        public void ChangeAllPages(AllPages p)
        {
            if (p == null)
            {
                return;
            }

            pages = p;
        }

        /// <summary>
        /// Return the current view 
        /// </summary>
        /// <returns></returns>
        public PageView GetView()
        {
            return view;
        }

        /// <summary>
        /// Add a page in the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <param name="page"></param>
        public virtual void Add(string key, GameObject panel)
        {
            if (string.IsNullOrEmpty(key) || panel == null)
            {
                return;
            }

            panelDictionary[key] = panel;
        }

        /// <summary>
        /// Change the list
        /// </summary>
        /// <param name="newList"></param>
        public void ChangeList(List<Page> newList)
        {
            paginationList = newList;

            if (paginationList == null || paginationList.Count == 0 && nextButton != null && prevButton != null)
            {
                nextButton.gameObject.SetActive(false);
                prevButton.gameObject.SetActive(false);
            }
            else if (nextButton != null || prevButton != null)
            {
                nextButton.gameObject.SetActive(true);
                prevButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Change the view
        /// </summary>
        /// <param name="newView"></param>
        public void ChangeView(PageView newView)
        {
            if (newView == null || newView == view)
            {
                return;
            }

            view = newView;
        }

        /// <summary>
        /// Init the view and the current Page
        /// </summary>
        public void InitView()
        {
            InitCurrentPage();
            view.PageDisplayer(currentPage);
        }

        /// <summary>
        /// When the panel change
        /// </summary>
        public void ChangeListAndView(List<Page> newList, PageView newView)
        {
            if (newList != null && newList.Count > 0)
            {
                paginationList = newList;
                InitCurrentPage();
            }

            view = newView;
            view.PageDisplayer(currentPage);
        }

        /// <summary>
        /// Open or close a panel
        /// </summary>
        /// <param name="type"></param>
        public virtual void SwitchPanel(string key)
        {
            if (panelDictionary.TryGetValue(key, out GameObject panel))
            {
                if (currentPanel == panel)
                {
                    return;
                }

                if (currentPanel != null)
                {
                    currentPanel.SetActive(false);
                }

                currentPanel = panel;
                currentPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Use to init or change the value of the currentPage variable
        /// </summary>
        public void InitCurrentPage()
        {
            if (paginationList != null && paginationList.Count > 0)
            {
                currentPage = paginationList[0];
            }
        }

        /// <summary>
        /// Pass to the next page
        /// </summary>
        public virtual void Next()
        {
            index++;
            
            if (isLoop)
            {
                if (index >= paginationList.Count)
                {
                    index = 0;
                }
            }
            else
            {    
                if (index >= paginationList.Count)
                {
                    index = paginationList.Count - 1;
                }
            }

            if (paginationList[index] != currentPage)
            {
                currentPage = paginationList[index];
            }

            if (view == null)
            {
                return;
            }

            view.PageDisplayer(currentPage);
        }

        /// <summary>
        /// Pass to the prev page
        /// </summary>
        public virtual void Prev()
        {
            index--;

            if (isLoop)
            {
                if (index < 0)
                {
                    index = paginationList.Count - 1;
                }
            }
            else
            {
                if (index < 0)
                {
                    index = 0;
                }
            }

            if (paginationList[index] != currentPage)
            {
                currentPage = paginationList[index];
            }

            if (view == null)
            {
                return;
            }

            view.PageDisplayer(currentPage);
        }

        /// <summary>
        /// Go to a specific Page
        /// </summary>
        public virtual void GoToPanel(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }


        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Wait if view is null before update the view
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForView()
        {
            while (view == null)
            {
                yield return null;
            }

            view.PageDisplayer(currentPage);
        }

        #endregion
    }
}