using Crimson.Core.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Crimson.Core
{
    public class OnDisableButton
    {

    }

    public class OnEnableButton
    {

    }

    public abstract class ButtonParent : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Button of this script")]
        [SerializeField]
        protected Button btn = null;

        [Tooltip("Clip of the button to set UI Sound")]
        [SerializeField]
        protected AudioClip btnClip  = null;

        [Tooltip("Where the clip will be set")]
        [SerializeField]
        protected EAudio audioType = EAudio.None;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Start()
        {
            if (btn == null)
            {
                return;
            }
         
            btn.onClick.AddListener(OnClick);
            SubscribeEvent();
        }

        protected virtual void OnDestroy()
        {
            if (btn == null)
            {
                return;
            }

            btn.onClick.RemoveListener(OnClick);
            UnsubscribeEvent();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Use to disable the button
        /// </summary>
        /// <param name="d"></param>
        protected virtual void DisableButton(OnDisableButton d)
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = false;
        }

        /// <summary>
        /// Use to Enable or disable the button
        /// </summary>
        /// <param name="e"></param>
        protected virtual void EnableButton(OnEnableButton e)
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = true;
        }
        
        /// <summary>
        /// Where the player push the button
        /// </summary>
        protected virtual void OnClick()
        {
            if (btnClip != null && audioType != EAudio.None)
            {
               EventBus.Publish<OnPlaySoundEvent>(new OnPlaySoundEvent(audioType, btnClip, false));
            }
        }

        protected virtual void SubscribeEvent()
        {
            EventBus.Subscribe<OnDisableButton>(DisableButton);
            EventBus.Subscribe<OnEnableButton>(EnableButton);
        }

        protected virtual void UnsubscribeEvent()
        {
            EventBus.Unsubscribe<OnDisableButton>(DisableButton);
            EventBus.Unsubscribe<OnEnableButton>(EnableButton);
        }
        
        #endregion
    }
}