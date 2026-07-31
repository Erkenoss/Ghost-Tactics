using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crimson.Core.Audio
{
    [Serializable]
    public class PlayerSoundType
    {
        #region Public Fields

        public EAudio Type { get { return type; } }
        public List<AudioClip> ClipType { get { return clipType; } }

        #endregion

        #region Private Fields

        [Tooltip("What type of sound are stocked here")]
        [SerializeField]
        private EAudio type = EAudio.None;

        [Tooltip("List of all clip base on type enum")]
        [SerializeField]
        private List<AudioClip> clipType = new List<AudioClip>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return an audio clip base on the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AudioClip GetClip(string name)
        {
            if (clipType == null || clipType.Count == 0)
            {
                return null;
            }

            AudioClip clip = clipType.Find(c => c.name == name);
            return clip;
        }

        #endregion

        #region Private Methods
        #endregion
    }


    [Serializable]
    public class SoundContext
    {
        #region Public Fields

        public EPlayerContext PlayerContext { get {  return playerContext; } }
        public List<PlayerSoundType> SoundTypeByContext {  get { return soundTypeByContext; } }

        #endregion

        #region Private Fields

        [Tooltip("When we need to play this type of sound")]
        [SerializeField]
        private EPlayerContext playerContext = EPlayerContext.None;

        [Tooltip("List of sound by type")]
        [SerializeField]
        private List<PlayerSoundType> soundTypeByContext = new List<PlayerSoundType>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Retur the PlayerSOundType needed base on EAudio type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public PlayerSoundType GetPlayerSoundType(EAudio type)
        {
            if (type == EAudio.None || soundTypeByContext == null || soundTypeByContext.Count == 0)
            {
                return null;
            }

            return soundTypeByContext.Find(p => p.Type == type);
        }

        #endregion

        #region Private Methods
        #endregion
    }

    [CreateAssetMenu(fileName = "PlayerSoundContainer", menuName = "Crimson/Audio/Player Container")]
    public class PlayerSoundContainer : ScriptableObject
    {
        #region Public Fields

        public List<SoundContext> AllPlayerSound { get { return AllPlayerSound; } }

        #endregion

        #region Private Fields

        [Tooltip("All the sound of the player")]
        [SerializeField]
        private List<SoundContext> allPlayerSound = new List<SoundContext>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return the player sound context needed
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public SoundContext GetSoundContext(EPlayerContext context)
        {
            if (allPlayerSound == null || allPlayerSound.Count == 0)
            {
                return null;
            }

            return allPlayerSound.Find(s => s.PlayerContext == context);
        }

        #endregion

        #region Private Methods
        #endregion
    }
}