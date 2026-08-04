using System.Collections.Generic;
using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "Node", menuName = "Crimson/Map/Node")]
    public class MapNode : ScriptableObject
    {
        #region Public Fields

        public ENodeType NodeType { get { return nodeType; } private set { } }
        public string Id { get { return id; } private set { } }
        public string NodeName { get { return nodeName; } private set { } }
        public Sprite NodeIcon { get { return nodeIcon; } private set { } }
        public bool IsVisible { get { return isVisible; } private set { } }
        public bool IsLocked { get { return isLocked; } private set { } }
        public bool IsCompleted { get {  return isCompleted; } private set { } }
        public List<MapReward> Rewards { get { return rewards; } private set { } }

        #endregion

        #region Private Fields

        [Tooltip("Type of the Node")]
        [SerializeField]
        private ENodeType nodeType = ENodeType.None;

        [Tooltip("Id of the Node")]
        [SerializeField]
        private string id = string.Empty;

        [Tooltip("Name of the node. Use on display")]
        [SerializeField]
        private string nodeName = string.Empty;

        [Tooltip("Icon of the node. Use on display")]
        [SerializeField]
        private Sprite nodeIcon = null;

        [Tooltip("Use to know if this node is visible for the player")]
        [SerializeField]
        private bool isVisible = false;

        [Tooltip("Use to know if this node is locked")]
        [SerializeField]
        private bool isLocked = false;

        [Tooltip("Use to know if the player has already completed this node")]
        [SerializeField]
        private bool isCompleted = false;

        [Tooltip("Reward granted when this node is completed")]
        [SerializeField]
        private List<MapReward> rewards = new List<MapReward>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}