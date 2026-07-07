using UnityEngine;
using System.Collections.Generic;

namespace Crimson.Core.Scenes
{
    [CreateAssetMenu(fileName = "Scene Databasse", menuName = "Crimson/Scene/Database")]
    public class SceneDatabase : ScriptableObject
    {
        #region Public Fields

        public List<SceneGroupSO> ContainerList { get { return containerList; } }

        #endregion

        #region Private Fields

        [Tooltip("All SceneContainer in the game")]
        [SerializeField]
        private List<SceneGroupSO> containerList = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}