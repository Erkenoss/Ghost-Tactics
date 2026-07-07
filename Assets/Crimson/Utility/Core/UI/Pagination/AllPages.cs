using UnityEngine;
using System.Collections.Generic;

namespace Crimson.Pagination
{
    [CreateAssetMenu(fileName = "All Pages", menuName = "Crimson/Pagination/AllPages")]
    public class AllPages : ScriptableObject
    {
        #region Public Fields

        public List<Page> AllPage { get { return allPage; } }

        #endregion

        #region Private Fields

        [Tooltip("List of all pages")]
        [SerializeField]
        private List<Page> allPage = new List<Page>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    } }
