using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "MapRoute", menuName = "Crimson/Map/Route")]
    public class MapRoute : ScriptableObject
    {
        #region Public Fields

        public string Id { get { return id; } }
        public MapNode NodeA { get { return nodeA; } }
        public MapNode NodeB { get { return nodeB; } }
        public ERouteDirection Direction { get { return direction; } }
        public float TraversalCost { get { return traversalCost; } }
        public bool IsLocked { get { return isLocked; } }
        public bool IsVisible { get { return isVisible; } }

        #endregion

        #region Private Fields

        [Tooltip("Id of the Route")]
        [SerializeField]
        private string id = string.Empty;

        [Tooltip("The first Map Node of the Route")]
        [SerializeField]
        private MapNode nodeA = null;

        [Tooltip("The second Map Node of the Route")]
        [SerializeField]
        private MapNode nodeB = null;

        [Tooltip("The direction of the Route")]
        [SerializeField]
        private ERouteDirection direction = ERouteDirection.None;

        [Tooltip("Generic cost associated with traversing this route.")]
        [Min(0f)]
        [SerializeField]
        private float traversalCost = 1f;

        [Tooltip("The player can travell along the road or not")]
        [SerializeField]
        private bool isLocked = false;

        [Tooltip("The player can see the road or not")]
        [SerializeField]
        private bool isVisible = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}