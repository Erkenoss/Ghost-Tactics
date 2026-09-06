using System.Collections;
using Crimson.Core;
using GhostTactics.Data;
using UnityEngine;
using GhostTactics.Core;
using Tutorial.Runtime.Flow;
using UnityEngine.EventSystems;

namespace GhostTactics.UI
{
    public class PlayerActionButton : ButtonParent, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Ability triggered when this button is clicked.")]
        [SerializeField]
        private AbilityData data = null;

        [Tooltip("Bubble displayed when the player holds this button to show additional information.")]
        [SerializeField]
        private GameObject toDisplay = null;

        [Tooltip("Duration in seconds required to consider the input as a long press.")]
        [SerializeField]
        private float longPressDuration = 0.5f;

        /// <summary>
        /// Coroutine currently handling the long press detection.
        /// </summary>
        private Coroutine longPressCoroutine = null;

        /// <summary>
        /// Defines whether the current input has already triggered a long press.
        /// </summary>
        private bool longPressTriggered = false;

        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Initializes the button and starts the tutorial behavior when required.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            if (TutorialFlowController.Instance == null || TutorialFlowController.Instance.Runner != null && TutorialFlowController.Instance.Runner.IsCompleted)
            {
                return;
            }

            PlayTuto();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the tutorial behavior associated with this button.
        /// </summary>
        public void PlayTuto()
        {
        }

        /// <summary>
        /// Play the tutorial for the information bubble associated with this button.
        /// </summary>
        public void PlayBubbleTutorial()
        {
        }

        /// <summary>
        /// Starts detecting a long press when the pointer is pressed on the button.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            longPressTriggered = false;
            longPressCoroutine = StartCoroutine(LongPressRoutine());
        }

        /// <summary>
        /// Stops the long press detection and hides the information bubble when the pointer is released.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            StopLongPress();
            HideBubble();
        }

        /// <summary>
        /// Stops the long press detection and hides the information bubble when the pointer leaves the button.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            StopLongPress();
            HideBubble();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Waits for the required duration before triggering the long press behavior.
        /// </summary>
        private IEnumerator LongPressRoutine()
        {
            yield return new WaitForSeconds(longPressDuration);

            longPressTriggered = true;
            ShowBubble();
            longPressCoroutine = null;
        }

        /// <summary>
        /// Stops the active long press coroutine if one is currently running.
        /// </summary>
        private void StopLongPress()
        {
            if (longPressCoroutine == null)
            {
                return;
            }

            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }

        /// <summary>
        /// Displays the information bubble associated with this button.
        /// </summary>
        private void ShowBubble()
        {
            if (toDisplay == null)
            {
                return;
            }

            toDisplay.SetActive(true);
        }

        /// <summary>
        /// Hides the information bubble associated with this button.
        /// </summary>
        private void HideBubble()
        {
            if (toDisplay == null)
            {
                return;
            }

            toDisplay.SetActive(false);
        }

        /// <summary>
        /// Publishes the selected ability unless the input was used as a long press.
        /// </summary>
        protected override void OnClick()
        {
            if (longPressTriggered)
            {
                longPressTriggered = false;
                return;
            }

            EventBus.Publish(new AbilityChoice(data));
        }

        #endregion
    }
}
