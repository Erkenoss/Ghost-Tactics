using GhostTactics.Core;
using GhostTactics.Core.Dialogue;
using UnityEngine;

namespace GhostTactics.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "GhostTactics/LevelData")]
    public class LevelData : ScriptableObject
    {
        #region Public Fields

        public bool HasPreviousDialogue { get { return hasPreviousDialogue; } }
        public bool HasNextDialogue { get { return hasNextDialogue; } } 
        public ETypeLevelContainer BiomeType { get { return biomeType; } }
        public Sprite LevelImage { get { return levelImage; } }
        public int LevelNumber { get { return levelNumber; } }
        public int LevelActionSlot { get { return levelActionSlot; } }
        public int LevelGhostActionSlot { get { return levelGhostActionSlot; } }
        public int GetVisual { get { return getVisual; } }
        public EnnemyData EnnemyLevel { get { return ennemyLevel; } }
        public DialogueLevelContainer DialogueContainer {  get { return dialogueContainer; } }

        #endregion

        #region Private Fields

        [Tooltip("Is this level need to display dialogue before fight")]
        [SerializeField]
        private bool hasPreviousDialogue = false;

        [Tooltip("Is this level need to display dialogue after the fight")]
        [SerializeField]
        private bool hasNextDialogue = false;

        [Tooltip("Type of the container where we find the level")]
        [SerializeField]
        private ETypeLevelContainer biomeType = ETypeLevelContainer.None;

        [Tooltip("Image of the level")]
        [SerializeField]
        private Sprite levelImage = null;

        [Tooltip("Level of this Data. 1 is the first of the container, 2 the second... etc")]
        [SerializeField]
        private int levelNumber = 0;

        [Tooltip("how many action the player can use in this level to fight the ennemy")]
        [SerializeField]
        private int levelActionSlot = 0;

        [Tooltip("How many action the player can use with the ghost to win the level")]
        [SerializeField]
        private int levelGhostActionSlot = 0;

        [Tooltip("Use to know how much the player win of previsualization when he succeed in the level perfectly")]
        [SerializeField]
        private int getVisual = 0;

        [Tooltip("Ennemy of the level")]
        [SerializeField]
        private EnnemyData ennemyLevel = null;

        [Tooltip("Dialogue Container of this level")]
        [SerializeField]
        private DialogueLevelContainer dialogueContainer = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}