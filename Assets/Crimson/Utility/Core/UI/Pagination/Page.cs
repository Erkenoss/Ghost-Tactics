using UnityEngine;

namespace Crimson.Pagination
{
    [CreateAssetMenu(fileName = "Page", menuName = "Crimson/Pagination/Page")]
    public class Page : ScriptableObject
    {
        #region Public Fields

        public string Title { get { return title; } }

        #endregion

        #region Private Fields

        [Tooltip("Title of the page")]
        [SerializeField]
        private string title = string.Empty;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}