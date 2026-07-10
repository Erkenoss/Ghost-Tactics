using UnityEngine;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "GhostTactics/LevelData")]
    public class LevelData : ScriptableObject
    {
        #region Public Fields

        public int LevelNumber { get { return levelNumber; } }
        public int LevelActionSlot { get { return levelActionSlot; } }
        public int LevelGhostActionSlot { get { return levelGhostActionSlot; } }
        public EnnemyData EnnemyLevel { get { return ennemyLevel; } }

        #endregion

        #region Private Fields

        [Tooltip("Level of this Data. 1 is the first of the container, 2 the second... etc")]
        [SerializeField]
        private int levelNumber = 0;

        [Tooltip("how many action the player can use in this level to fight the ennemy")]
        [SerializeField]
        private int levelActionSlot = 0;

        [Tooltip("How many action the player can use with the ghost to win the level")]
        [SerializeField]
        private int levelGhostActionSlot = 0;

        [Tooltip("Use to know how many try the player can be to visualized ennemies actions before to choose his actions")]
        [SerializeField]
        private int getVisual = 0;

        [Tooltip("Ennemy of the level")]
        [SerializeField]
        private EnnemyData ennemyLevel = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}