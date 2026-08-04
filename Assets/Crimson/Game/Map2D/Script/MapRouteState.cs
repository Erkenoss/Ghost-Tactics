using UnityEngine;

namespace Crimson.Map
{
    public class MapRouteState
    {
        #region Public Fields

        /// <summary>
        /// Direction of the route
        /// </summary>
        public ERouteDirection Direction = ERouteDirection.None;

        /// <summary>
        /// Cost when the player travell along the Route
        /// </summary>
        public float TraversalCost = 0f;

        /// <summary>
        /// Is this Route visible?
        /// </summary>
        public bool IsVisible = false;

        /// <summary>
        /// Is this Route Locked?
        /// </summary>
        public bool IsLocked = false;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}