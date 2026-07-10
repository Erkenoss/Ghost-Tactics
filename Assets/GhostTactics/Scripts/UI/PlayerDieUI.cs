using UnityEngine;
using System.Collections.Generic;
using GhostTactics.Core;
using Crimson.Core;

namespace GhostTactics.UI
{
    public class OnChooseForGhost
    {
        #region Public Fields

        public ChooseActionButton Button { get { return button; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Button that the player choose to save a ghost action
        /// </summary>
        private ChooseActionButton button = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public OnChooseForGhost(ChooseActionButton ability)
        {
            button = ability;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class OnRemoveGhostChoice
    {
        #region Public Fields

        public ChooseActionButton Button { get { return button; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Reference of the button we need to remove the choice
        /// </summary>
        private ChooseActionButton button = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public OnRemoveGhostChoice(ChooseActionButton button)
        {
            this.button = button;
        }

        #endregion

        #region Private Methods
        #endregion
    }


    public class PlayerDieUI : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("List of action use by the player during this turn")]
        [SerializeField]
        private List<ChooseActionButton> actionButtonChooseByPlayer = new List<ChooseActionButton>();

        [Tooltip("List of button that the player can use to set the ghost actions")]
        [SerializeField]
        private List<GhostChoiceButton> ghostChoiceButtonList = new List<GhostChoiceButton>();

        [Tooltip("Panel witch contains the player die UI")]
        [SerializeField]
        private GameObject panel = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            UnSubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the panel of the player die UI
        /// </summary>
        public void ShowPanel(OnPlayerDie p)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(true);
        }

        /// <summary>
        /// Hide the panel of the player die UI
        /// </summary>
        public void HidePanel()
        {
            if (panel == null)
            {
                return;
            }

            ResetPanel();
            panel.SetActive(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Base on the current level actually played. Enable the number of button that the player can use to choose as ghost actions
        /// </summary>
        /// <param name="currentLevel"></param>
        private void EnableButton(CombatResolutionEvent combat)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentLevel == null)
            {
                return;
            }

            if (actionButtonChooseByPlayer == null || actionButtonChooseByPlayer.Count == 0 || ghostChoiceButtonList == null || ghostChoiceButtonList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < actionButtonChooseByPlayer.Count; i++)
            {
                if (i < GameManager.Instance.CurrentLevel.LevelActionSlot && !actionButtonChooseByPlayer[i].gameObject.activeSelf)
                {
                    actionButtonChooseByPlayer[i].gameObject.SetActive(true);
                    actionButtonChooseByPlayer[i].Set(combat.PlayerList[i]);
                }
                else
                {
                    actionButtonChooseByPlayer[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0;i < ghostChoiceButtonList.Count; i++)
            { 
                if (i < GameManager.Instance.CurrentLevel.LevelGhostActionSlot && !ghostChoiceButtonList[i].gameObject.activeSelf)
                {
                    ghostChoiceButtonList[i].gameObject.SetActive(true);
                }
                else
                {
                    ghostChoiceButtonList[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Reset the value of the different button of the lists
        /// </summary>
        private void ResetPanel()
        {
            if (actionButtonChooseByPlayer == null || actionButtonChooseByPlayer.Count == 0 || ghostChoiceButtonList == null || ghostChoiceButtonList.Count == 0)
            {
                return;
            }

            foreach (ChooseActionButton button in actionButtonChooseByPlayer)
            {
                button.Disable();
                button.gameObject.SetActive(false);
            }

            foreach (GhostChoiceButton button in ghostChoiceButtonList)
            {
                button.Disable();
                button.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// When the player choose an action for the ghost
        /// </summary>
        /// <param name="action"></param>
        private void Choose(OnChooseForGhost action)
        {
            if (action == null || action.Button == null)
            {
                return;
            }

            foreach (GhostChoiceButton button in ghostChoiceButtonList)
            {
                if (button.Data == null && button.gameObject.activeSelf)
                {
                    button.Set(action.Button);
                    action.Button.DisableButton();
                    break;
                }
            }
        }

        /// <summary>
        /// Remove a choice of an action for the ghost
        /// </summary>
        /// <param name="choice"></param>
        private void RemoveGhostActionChoice(OnRemoveGhostChoice choice)
        {
            if (choice == null || choice.Button == null)
            {
                return;
            }

            foreach (ChooseActionButton button in actionButtonChooseByPlayer)
            {
                if (button == choice.Button)
                {
                    button.RemoveGhostChoice();
                }
            }
        }

        /// <summary>
        /// Subscribe to the event bus to listen for CombatResolutionEvent and enable the appropriate buttons when the event is triggered.
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<CombatResolutionEvent>(EnableButton);
            EventBus.Subscribe<OnChooseForGhost>(Choose);
            EventBus.Subscribe<OnRemoveGhostChoice>(RemoveGhostActionChoice);
            EventBus.Subscribe<OnPlayerDie>(ShowPanel);
        }

        /// <summary>
        /// Unsubscribe from the event bus to stop listening for CombatResolutionEvent when the object is destroyed.
        /// </summary>
        private void UnSubscribe()
        {
            EventBus.Unsubscribe<CombatResolutionEvent>(EnableButton);
            EventBus.Unsubscribe<OnChooseForGhost>(Choose);
            EventBus.Unsubscribe<OnRemoveGhostChoice>(RemoveGhostActionChoice);
            EventBus.Unsubscribe<OnPlayerDie>(ShowPanel);
        }

        #endregion
    }
}