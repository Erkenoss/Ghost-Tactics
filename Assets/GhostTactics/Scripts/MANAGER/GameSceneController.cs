using Crimson.Core;
using UnityEngine;
using GhostTactics.UI;

namespace GhostTactics.Core
{
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

            if (level.Data.HasPreviousDialogue)
            {
                dialogueBackground.SetActive(true);

                dialogueUI.UpdateView(level.Data);
            }
        }

        /// <summary>
        /// Sub in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(CheckDialogue);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(CheckDialogue);
        }

        #endregion
    }
}