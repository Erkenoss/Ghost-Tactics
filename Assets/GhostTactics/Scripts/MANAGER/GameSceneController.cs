using Crimson.Core;
using UnityEngine;
using GhostTactics.UI;
using Crimson.Core.Audio;
using GhostTactics.Data;

namespace GhostTactics.Core
{
    public class OnEndDialogue
    {

    }

    public class GameSceneController : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Reference of the DialogueUI script in the scene")]
        [SerializeField]
        private DialogueUI dialogueUI = null;

        [Tooltip("Where the dialogue will be display")]
        [SerializeField]
        private GameObject dialogueBackground = null;

        [Tooltip("The music combat context. Use when combat")]
        [SerializeField]
        private EMusicContext combatContext = EMusicContext.None;

        [Tooltip("The music ambiance context. Use when dialogue")]
        [SerializeField]
        private EMusicContext ambianceContext = EMusicContext.None;

        /// <summary>
        /// The currentLevel actually running
        /// </summary>
        private LevelData currentLevel = null;

        /// <summary>
        /// Use to know if a previous combat dialogue has already done
        /// </summary>
        private bool hasAPreviousDial = false;

        /// <summary>
        /// Use to know if we already have a preivous dialogue
        /// </summary>
        private bool previousDialAlreadyDone = false;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Check if we need to display the dialogue panel and display it
        /// </summary>
        /// <param name="level"></param>
        private void CheckDialogue(NextLevel level)
        {
            if (dialogueBackground == null || dialogueUI == null || level.Data == null)
            {
                return;
            }

            currentLevel = level.Data;

            if (!previousDialAlreadyDone && level.Data.HasPreviousDialogue)
            {
                hasAPreviousDial = true;
                previousDialAlreadyDone = true;
                dialogueBackground.SetActive(true);
                dialogueUI.UpdateViewAtLunch(level.Data, hasAPreviousDial);
                EventBus.Publish<OnNewMusicContainer>(new OnNewMusicContainer(ambianceContext));
            }
            else
            {
                dialogueBackground.SetActive(false);
                EventBus.Publish<OnNewMusicContainer>(new OnNewMusicContainer(combatContext));
            }
        }

        /// <summary>
        /// Check the dialogue after the end of the fight
        /// </summary>
        /// <param name="die"></param>
        private void CheckDialogue(OnEnnemyDie die)
        {
            if (dialogueBackground == null || dialogueUI == null)
            {
                return;
            }

            if (currentLevel == null || !currentLevel.HasNextDialogue)
            {
                EventBus.Publish<OnSwitchLevel>(new OnSwitchLevel());
                previousDialAlreadyDone = false;
            }
            else
            {
                dialogueBackground.SetActive(true);
                dialogueUI.UpdateViewAtLunch(currentLevel, hasAPreviousDial);
                EventBus.Publish<OnNewMusicContainer>(new OnNewMusicContainer(ambianceContext));
            }
        }

        /// <summary>
        /// Manage the end of a dialogue
        /// </summary>
        /// <param name="dial"></param>
        private void EndDialogue(OnEndDialogue dial)
        {
            if (dialogueBackground == null)
            {
                return;
            }

            dialogueBackground.SetActive(false);
            
            if (hasAPreviousDial)
            {
                EventBus.Publish<OnNewMusicContainer>(new OnNewMusicContainer(combatContext));
                hasAPreviousDial = false;
            }
            else
            {
                EventBus.Publish<OnSwitchLevel>(new OnSwitchLevel());
                previousDialAlreadyDone = false;
            }
        }

        /// <summary>
        /// Sub in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(CheckDialogue);
            EventBus.Subscribe<OnEndDialogue>(EndDialogue);
            EventBus.Subscribe<OnEnnemyDie>(CheckDialogue);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(CheckDialogue);
            EventBus.Unsubscribe<OnEnnemyDie>(CheckDialogue);
            EventBus.Unsubscribe<OnEndDialogue>(EndDialogue);
        }

        #endregion
    }
}