using Crimson.Core;
using GhostTactics.Core;
using GhostTactics.Core.Dialogue;
using GhostTactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GhostTactics.UI
{
    public class OnNextLine
    {

    }

    public class DialogueUI : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The image where the player will be display")]
        [SerializeField]
        private Image playerPortrait = null;

        [Tooltip("The other character wwill be display here")]
        [SerializeField]
        private Image otherPortrait = null;

        [Tooltip("Where the dialogue will be set")]
        [SerializeField]
        private TextMeshProUGUI dialogueText = null;

        [Tooltip("ScrollRect used to display the dialogue")]
        [SerializeField]
        private ScrollRect dialogueScroll = null;

        [Tooltip("Content moved by the ScrollRect")]
        [SerializeField]
        private RectTransform dialogueContent = null;

        [Tooltip("Color to display the name of the player")]
        [SerializeField]
        private Color playerColor = Color.white;

        [Tooltip("Color to display the others name")]
        [SerializeField]
        private Color otherColor = Color.white;

        /// <summary>
        /// Current DialogueContainer actualy display
        /// </summary>
        private DialogueLevelContainer currentData = null;

        /// <summary>
        /// CurrentList actually used to manage the dialogue display
        /// </summary>
        private List<DialogueLine> currentList = new List<DialogueLine>();

        /// <summary>
        /// Index use to navigate in the dialogue line
        /// </summary>
        private int currentIndex = -1;

        /// <summary>
        /// Use to know if the player has talk
        /// </summary>
        private bool playerHasTalk = false;

        /// <summary>
        /// Use to know if other has already talk
        /// </summary>
        private bool otherHasTalk = false;

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

        /// <summary>
        /// Update the view base on the current level
        /// </summary>
        public void UpdateViewAtLunch(LevelData data, bool previous)
        {
            if (data == null || data.DialogueContainer == null || playerPortrait == null || otherPortrait == null || dialogueText == null)
            {
                return;
            }

            currentData = data.DialogueContainer;
            currentList = previous ? currentData.PrevDialogueLines : currentData.NextDialogueLines;

            playerHasTalk = false;
            otherHasTalk = false;

            if (currentList == null || currentList.Count == 0 || currentList[0] == null)
            {
                return;
            }

            currentIndex = 0;
            DialogueLine line = currentList[currentIndex];
            
            if (line.Line == null)
            {
                return;
            }

            if (line.IsPlayer)
            {
                if (line.Portrait == null)
                {
                    return;
                }

                playerPortrait.sprite = line.Portrait;

                Color color = playerPortrait.color;
                color.a = 1;
                playerPortrait.color = color;
                playerPortrait.sprite = line.Portrait;

                otherPortrait.sprite = null;
                color = otherPortrait.color;
                color.a = 0;
                otherPortrait.color = color;

                playerHasTalk = true;
            }
            else
            {
                if (line.Portrait == null)
                {
                    otherPortrait.sprite = null;
                    Color color = otherPortrait.color;
                    color.a = 0;
                    otherPortrait.color = color;

                    color = playerPortrait.color;
                    color.a = 0;
                    playerPortrait.color = color;
                }
                else
                {
                    Color color = otherPortrait.color;
                    color.a = 1;
                    otherPortrait.color = color;
                    otherPortrait.sprite = line.Portrait;

                    color = playerPortrait.color;
                    color.a = 0;
                    playerPortrait.color = color;

                    otherHasTalk = true;
                }
            }

            dialogueText.text = FormatLine(line);
            ScrollToLastLine();
        }

        /// <summary>
        /// Pass to the next line
        /// </summary>
        /// <param name="line"></param>
        private void UpdateView(OnNextLine nextLine)
        {
            currentIndex++;
            
            if (playerPortrait == null || otherPortrait == null || dialogueText == null)
            {
                return;
            }

            if (currentIndex >= currentList.Count || currentList[currentIndex] == null)
            {
                EventBus.Publish<OnEndDialogue>(new OnEndDialogue());
                return;
            }

            DialogueLine line = currentList[currentIndex];

            if (line.Line == null)
            {
                return;
            }

            if (line.IsPlayer)
            {
                if (line.Portrait == null)
                {
                    return;
                }

                playerPortrait.sprite = line.Portrait;
                
                if (!playerHasTalk)
                {
                    Color color = playerPortrait.color;
                    color.a = 1;
                    playerPortrait.color = color;
                    playerPortrait.sprite = line.Portrait;

                    playerHasTalk = true;
                }
            }
            else
            {
                if (otherHasTalk && line.Portrait != null)
                {
                    if (otherPortrait.sprite != line.Portrait)
                    {
                        otherPortrait.sprite = line.Portrait;
                    }
                }
                if (!otherHasTalk && line.Portrait != null)
                {
                    Color color = otherPortrait.color;
                    color.a = 1;
                    otherPortrait.color = color;
                    otherPortrait.sprite = line.Portrait;

                    otherHasTalk = true;
                }
                if (line.Portrait == null)
                {
                    otherPortrait.sprite = null;
                    Color color = otherPortrait.color;
                    color.a = 0;
                    otherPortrait.color = color;

                    otherHasTalk = false;
                }
            }

            dialogueText.text += $"\n{FormatLine(line)}";
            ScrollToLastLine();
        }

        /// <summary>
        /// Format a dialogue line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string FormatLine(DialogueLine line)
        {
            Color nameColor = line.IsPlayer ? playerColor : otherColor;
            string color = ColorUtility.ToHtmlStringRGBA(nameColor);
            string content = line.IsThinking ? $"<i>{line.Line}</i>" : line.Line;

            return $"<b><color=#{color}>{line.CharacterName}</color></b>: {content}";
        }

        /// <summary>
        /// Scroll to the last dialogue line
        /// </summary>
        private void ScrollToLastLine()
        {
            if (dialogueScroll == null || dialogueContent == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueContent);
            dialogueScroll.verticalNormalizedPosition = 0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnNextLine>(UpdateView);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnNextLine>(UpdateView);
        }

        #endregion
    }
}