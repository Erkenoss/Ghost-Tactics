using UnityEngine;
using System.Collections.Generic;

namespace Crimson.Audio
{
    [CreateAssetMenu(fileName = "Music Container Name", menuName = "Crimson/Audio/Music Container")]
    public class MusicClipContainer : ScriptableObject
    {
        #region Public Fields

        public List<AudioClip> MusicList { get { return musicList; } }
        public bool IsLooping { get { return isLooping; } set { isLooping = value; } }
        public bool IsRandom { get { return isRandom; } set { isRandom = value; } }

        #endregion

        #region Private Fields

        [Tooltip("List of all music in the game")]
        [SerializeField]
        private List<AudioClip> musicList = new List<AudioClip>();

        [Tooltip("This list play in loop or not?")]
        [SerializeField]
        private bool isLooping = true;

        [Tooltip("This list play Rrandomly?")]
        [SerializeField]
        private bool isRandom = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}