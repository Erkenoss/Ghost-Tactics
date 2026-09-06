using Crimson.Core;
using I2.Loc;
using UnityEngine;

namespace GhostTactics.Tutorial
{
    public class OnTutorialStepRequired
    {
        #region Public Fields

        /// <summary>
        /// Use to display the message for the player to solve the tutorial
        /// </summary>
        public LocalizedString Message = null;
        
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        public OnTutorialStepRequired(LocalizedString message)
        {
            Message = message;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class OnTutorialNotrequired
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class TutorialManager : Singleton<TutorialManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Script to manage the tutorial bubble")]
        [SerializeField]
        private TutorialMessageBubble tutorialBubble = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
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
        /// Duisable the activation of the bubble
        /// </summary>
        private void DisableBubble(OnTutorialNotrequired required)
        {
            if (tutorialBubble == null)
            {
                return;
            }

            tutorialBubble.CloseBubble();
        }

        /// <summary>
        /// Use to manage the tutorial bubble and display the message for the player to solve the tutorial
        /// </summary>
        /// <param name="required"></param>
        private void DisplayStepTutorialInformation(OnTutorialStepRequired required)
        {
            if (required == null || tutorialBubble == null)
            {
                return;
            }

            tutorialBubble.OpenCloseBubble(required.Message);
        }

        private void Subscribe()
        {
            EventBus.Subscribe<OnTutorialStepRequired>(DisplayStepTutorialInformation);
            EventBus.Subscribe<OnTutorialNotrequired>(DisableBubble);
        }

        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnTutorialStepRequired>(DisplayStepTutorialInformation);
            EventBus.Unsubscribe<OnTutorialNotrequired>(DisableBubble);
        }

        #endregion
    }
}