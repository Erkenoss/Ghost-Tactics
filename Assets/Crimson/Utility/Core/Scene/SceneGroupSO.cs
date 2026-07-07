using System.Collections.Generic;
using UnityEngine;

namespace Crimson.Core.Scenes
{
    [CreateAssetMenu(fileName = "Scene Group", menuName = "Crimson/Scene/Group")]
    public class SceneGroupSO : ScriptableObject
    {
        #region Public Fields

        public string GroupName { get { return groupName; } }
        public List<SceneReference> SceneToLoad { get { return sceneToLoad; } }
        public bool IsPlayerUnload { get { return isPlayerUnload; } }
        public bool IsCoreUnload { get { return isCoreUnload; } }

        #endregion

        #region Private Fields

        [Tooltip("Name of the group")]
        [SerializeField]
        private string groupName = string.Empty;

        [Tooltip("Scenes to load when this group is activated. All other non-persistent scenes will be unloaded")]
        [SerializeField]
        private List<SceneReference> sceneToLoad = new List<SceneReference>();

        [Tooltip("If true, the PlayerGroupScene will be unloaded when this group is loaded")]
        [SerializeField]
        private bool isPlayerUnload = false;

        [Tooltip("If true, the CoreScene will be unloaded")]
        [SerializeField]
        private bool isCoreUnload = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}