using UnityEngine;
using Crimson.Core;

namespace Crimson.Pagination
{
    [CreateAssetMenu(fileName = "Switch Button", menuName = "Crimson/ButtonActions/Switch Button")]
    public class SwitchButton : ButtonBase
    {
        /// <summary>
        /// Defined if this button go to the previous page in a pagination system
        /// </summary>
        [SerializeField]
        protected bool isPrev = false;

        public override void Execute()
        {
             if (PaginationManager.Instance != null)
             {
                if (isPrev)
                 {
                    PaginationManager.Instance.Prev();
                 }
                 else
                 {
                    PaginationManager.Instance.Next();
                 }
             }
        }
    }
}
