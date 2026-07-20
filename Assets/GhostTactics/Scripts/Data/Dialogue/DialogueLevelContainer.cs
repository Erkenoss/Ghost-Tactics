using I2.Loc;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GhostTactics.Core.Dialogue
{
    [Serializable]
    public class DialogueLine
    {
        #region Public Fields
        
        public Sprite Portrait { get { return portrait; } }
        public string CharacterName { get { return characterName; } }
        public LocalizedString Line { get { return line; } }
        public bool IsPlayer { get { return isplayer; } }

        #endregion

        #region Private Fields

        [Tooltip("Portrait of the character")]
        [SerializeField]
        private Sprite portrait = null;

        [Tooltip("Name of the character")]
        [SerializeField]
        private string characterName = string.Empty;

        [Tooltip("The line said by the character")]
        [SerializeField]
        private LocalizedString line = null;

        [Tooltip("Is the player talk here?")]
        [SerializeField]
        private bool isplayer = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    [CreateAssetMenu(fileName = "Dialogue Level Container", menuName = "GhostTactics/Dialogue/LevelContainer")]
    public class DialogueLevelContainer : ScriptableObject
    {
        #region Public Fields

        public List<DialogueLine> DialogueLines { get { return dialogueLines; } }
        public int Level { get { return level; } }

        #endregion

        #region Private Fields

        [Tooltip("List of dialogue line for this level")]
        [SerializeField]
        private List<DialogueLine> dialogueLines = new List<DialogueLine>();

        [Tooltip("Level of this Dialogue Level Container")]
        [SerializeField]
        private int level = 0;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}