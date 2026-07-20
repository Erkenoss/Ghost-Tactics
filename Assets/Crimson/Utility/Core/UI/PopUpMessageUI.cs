using Crimson.Core;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Crimson.Utilities
{
    public class OnPopUpMessage
    {
        public string Message = string.Empty;

        public OnPopUpMessage(string message)
        {
            Message = message;
        }
    }

    public class PopUpMessageUI : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The container of the Pop Up")]
        [SerializeField]
        private GameObject popUp = null;

        [Tooltip("The color of the text")]
        [SerializeField]
        private Color textColor = Color.white;

        [Tooltip("The text for the Pop Up")]
        [SerializeField]
        private TextMeshProUGUI text = null;

        [Tooltip("Timer before the pop up disappears. Set to 0 to keep it displayed.")]
        [SerializeField, Min(0f)]
        private float timer = 2f;

        /// <summary>
        /// Coroutine used to manage the pop up timer.
        /// </summary>
        private Coroutine timerCoroutine = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            UpdateTextColor();

            if (popUp != null)
            {
                popUp.SetActive(false);
            }

            Subscribe();
        }

        private void OnDestroy()
        {
            StopTimer();
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Update the color of the TMPro text.
        /// </summary>
        private void UpdateTextColor()
        {
            if (text == null)
            {
                return;
            }

            text.color = textColor;
        }

        /// <summary>
        /// Display a message in the pop up.
        /// </summary>
        /// <param name="messageEvent"></param>
        private void DisplayMessage(OnPopUpMessage messageEvent)
        {
            if (popUp == null || text == null || messageEvent == null || messageEvent.Message == text.text)
            {
                return;
            }

            StopTimer();

            if (string.IsNullOrWhiteSpace(messageEvent.Message))
            {
                HidePopUp();
                return;
            }

            text.text = messageEvent.Message;
            popUp.SetActive(true);

            if (timer > 0f)
            {
                timerCoroutine = StartCoroutine(StartTimer());
            }
        }

        /// <summary>
        /// Wait before hiding the pop up.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StartTimer()
        {
            yield return new WaitForSecondsRealtime(timer);

            timerCoroutine = null;
            HidePopUp();
        }

        /// <summary>
        /// Hide and clear the pop up.
        /// </summary>
        private void HidePopUp()
        {
            if (text != null)
            {
                text.text = string.Empty;
            }

            if (popUp != null)
            {
                popUp.SetActive(false);
            }
        }

        /// <summary>
        /// Stop the current pop up timer.
        /// </summary>
        private void StopTimer()
        {
            if (timerCoroutine == null)
            {
                return;
            }

            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        /// <summary>
        /// Subscribe to the EventBus.
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnPopUpMessage>(DisplayMessage);
        }

        /// <summary>
        /// Unsubscribe from the EventBus.
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnPopUpMessage>(DisplayMessage);
        }

        #endregion
    }
}