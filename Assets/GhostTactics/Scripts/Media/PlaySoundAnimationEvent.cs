using UnityEngine;

namespace Crimson.Core.Audio
{
    public class PlaySoundAnimationEvent : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("On what type of source the different sound need to play. In general, Effect or SFX")]
        [SerializeField]
        private EAudio type = EAudio.None;

        [Tooltip("Use to play audio voice in the animation")]
        [SerializeField]
        private EAudio voiceType = EAudio.None;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Use to play an 
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="type"></param>
        public void PlaySound(AudioClip clip)
        {
            EventBus.Publish<OnPlaySoundEvent>(new OnPlaySoundEvent(type, clip, false));
        }

        /// <summary>
        /// Play a voice for the character
        /// </summary>
        /// <param name="clip"></param>
        public void PlayVoice()
        {
            EventBus.Publish<OnPlayerSoundEvent>(new OnPlayerSoundEvent(EPlayerContext.Respawn, voiceType));
        }

        #endregion

        #region Private Methods
        #endregion
    }
}