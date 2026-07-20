using UnityEngine;
using System.Collections.Generic;
using GhostTactics.Core;
using GhostTactics.Data;

namespace GhostTactics.UI
{
    public class ButtonSelectedComponent : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("List of selected actions")]
        [SerializeField]
        private List<ButtonActionSelected> actionSelectedList = new List<ButtonActionSelected>();

        /// <summary>
        /// Count of the current level. Limit of actions that the player can select in the level. It will be updated at the end of each level
        /// </summary>
        private int levelCount = 0;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            if (ActionManager.Instance == null)
            {
                return;
            }

            ActionManager.Instance.UpdateButtonSelectedComponent(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable or disable the buttonActionSelected depending the number of the Level
        /// </summary>
        /// <param name="actionNumber"></param>
        public void EnableButtonAction(int actionNumber)
        { 
            if (actionSelectedList == null || actionSelectedList.Count == 0 || actionSelectedList.Count < actionNumber)
            {
                return;
            }

            for (int i = 0; i < actionSelectedList.Count; i++)
            {
                actionSelectedList[i].ResetButton();

                if (i < actionNumber)
                {
                    actionSelectedList[i].gameObject.SetActive(true);
                }
                else
                {
                    actionSelectedList[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Reset all the button in the container
        /// </summary>
        public void ResetAllButton()
        {
            if (actionSelectedList == null || actionSelectedList.Count == 0)
            {
                return;
            }

            foreach (ButtonActionSelected button in actionSelectedList)
            {
                button.ResetButton();
            }
        }

        /// <summary>
        /// Update the valule of levelCount
        /// </summary>
        /// <param name="count"></param>
        public void NewLevel(int count)
        {
            if(count == 0 || count == levelCount)
            {
                return;
            }

            levelCount = count;
        }

        /// <summary>
        /// Add an action in the list of actions selected by the player
        /// </summary>
        /// <param name="data"></param>
        public void AddAction(AbilityData data)
        {
            if (actionSelectedList == null || actionSelectedList.Count == 0 || actionSelectedList.Count == levelCount || data == null)
            {
                return;
            }

            ButtonActionSelected buttonToEnable = actionSelectedList.Find(b => b.CurrentData == null);

            if (buttonToEnable == null)
            {
                return;
            }

            ButtonActionSelected prevButton = null;
            ButtonActionSelected nextButton = null;

            for (int i = 0; i < actionSelectedList.Count; i++)
            {
                if (actionSelectedList[i] == buttonToEnable)
                {
                    prevButton = i > 0 ? actionSelectedList[i - 1] : null;
                    nextButton = i < actionSelectedList.Count - 1 ? actionSelectedList[i + 1] : null;

                    break;
                }
            }

            if (prevButton != null && prevButton.CurrentData != null && prevButton.CurrentData.Ability == data.Ability || nextButton != null && nextButton.CurrentData != null && nextButton.CurrentData.Ability == data.Ability)
            {
                Debug.Log("Can't have the same ability twice side by side");
                return;
            }

            buttonToEnable.UpdateButton(data);
        }

        /// <summary>
        /// Return the abilities choose of the player
        /// </summary>
        /// <returns></returns>
        public List<AbilityData> GetSelectedAbilities()
        { 
            List<AbilityData> selectedAbilities = new List<AbilityData>();

            if (actionSelectedList == null || actionSelectedList.Count == 0)
            {
                return selectedAbilities;
            }

            foreach (ButtonActionSelected button in actionSelectedList)
            {
                if (button.CurrentData != null)
                {
                    selectedAbilities.Add(button.CurrentData);
                }
            }

            return selectedAbilities;
        }

        #endregion

        #region Private Methods
        #endregion
    }
}