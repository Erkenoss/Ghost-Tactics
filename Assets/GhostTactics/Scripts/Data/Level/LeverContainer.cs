using GhostTactics.Core;
using UnityEngine;
using System.Collections.Generic;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "LevelContainer", menuName = "GhostTactics/LevelContainer")]
    public class LevelContainer : ScriptableObject
    {
        #region Public Fields

        public ETypeLevelContainer Type { get { return type; } }
        public List<LevelData> Container { get { return container; } }

        #endregion

        #region Private Fields

        [Tooltip("Type of level container")]
        [SerializeField]
        private ETypeLevelContainer type = ETypeLevelContainer.None;

        [Tooltip("List of levels of a part of the game.")]
        [SerializeField]
        private List<LevelData> container = new List<LevelData>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
