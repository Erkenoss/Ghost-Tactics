using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "Node", menuName = "Crimson/Map/Node")]
    public class MapNode : ScriptableObject
    {
        #region Public Fields

        public string Id { get { return id; } private set { } }
        public string NodeName { get { return nodeName; } private set { } }

        #endregion

        #region Private Fields

        [Tooltip("Id of the Node")]
        [SerializeField]
        private string id = string.Empty;

        [Tooltip("Name of the node. Use on display")]
        [SerializeField]
        private string nodeName = string.Empty;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}