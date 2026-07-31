using Crimson.Core;
using GhostTactics.Data;
using UnityEngine;
using GhostTactics.Core;
using Crimson.Core.Settings;

#if UNITY_STANDALONE
using UnityEngine.EventSystems;
#endif

namespace GhostTactics.UI
{
    public class PlayerActionButton : ButtonParent
#if UNITY_STANDALONE
    
    , IPointerEnterHandler
    , IPointerExitHandler

#endif

    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Ability of the button")]
        [SerializeField]
        private AbilityData data = null;

        [Tooltip("transform of the infos bubble")]
        [SerializeField]
        private Transform infosbubble = null;

#if UNITY_ANDROID

        /// <summary>
        /// If the pllayer play on Android device, this is use to manage the infos bubble
        /// </summary>
        private bool firstClick = false;

#endif

        #endregion

        #region MonoBehaviour Callbacks

#if UNITY_STANDALONE

        /// <summary>
        /// Only on PC
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowBubble();
        }

        /// <summary>
        /// Only on PC
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            HideBubble();
        }
#endif

        private void OnDisable()
        {
            HideBubble();
        }

        #endregion

        #region Public Methods

#if UNITY_ANDROID
        /// <summary>
        /// Hide on android only if the player push another button
        /// </summary>
        public void Hide()
        {
            HideBubble();
        }
#endif

        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            if (SettingManager.Instance == null)
            {
                return;
            }

            if (!SettingManager.Instance.GetBoolSettingValue(SettingBoolType.Description))
            {
                EventBus.Publish(new AbilityChoice(data, this));
                return;
            }

#if UNITY_ANDROID
            if (ActionManager.Instance == null)
            {
                return;
            }

            if (!firstClick)
            {
                firstClick = true;
                ShowBubble();
                ActionManager.Instance.UpdateCurrentBtn(this);
                return;
            }

            ActionManager.Instance.UpdateCurrentBtn(this);
#endif
            EventBus.Publish(new AbilityChoice(data, this));
        }

        protected override void DisableButton(OnDisableButton d)
        {
            base.DisableButton(d);

            HideBubble();
        }

        /// <summary>
        /// Show the infos bubble
        /// </summary>
        private void ShowBubble()
        {
            if (infosbubble == null || SettingManager.Instance == null)
            {
                return;
            }

            infosbubble.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the infos bubble
        /// </summary>
        private void HideBubble()
        {
            if (infosbubble == null)
            {
                return;
            }

            infosbubble.gameObject.SetActive(false);

#if UNITY_ANDROID
            firstClick = false;
#endif

        }

        #endregion
    }
}