using System.Collections.Generic;
using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "Map Data", menuName = "Crimson/Map/Map Data")]
    public class MapData : ScriptableObject
    {
        #region Public Fields

        public Sprite Background { get { return background; } }
        public MapNode StartNode { get { return startNode; } }
        public List<MapNode> MapNodes { get { return mapNodes; } }
        public List<MapRoute> MapRoutes { get { return mapRoutes; } }

        #endregion

        #region Private Fields

        [Tooltip("Background of the map")]
        [SerializeField]
        private Sprite background = null;

        [Tooltip("Start node of the graph/map")]
        [SerializeField]
        private MapNode startNode = null;

        [Tooltip("All the node of the map")]
        [SerializeField]
        private List<MapNode> mapNodes = new List<MapNode>();

        [Tooltip("All the Route of the map")]
        [SerializeField]
        private List<MapRoute> mapRoutes = new List<MapRoute>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}