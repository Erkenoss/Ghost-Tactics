using UnityEngine;
using System.Collections.Generic;

namespace Crimson.Core.Audio
{
    [CreateAssetMenu(fileName = "SFX container", menuName = "Crimson/Audio/SFX Container")]
    public class SFXClipContainer : ScriptableObject
    {
        #region Public Fields

        public ESFXContext Context { get { return context; } }
        public List<AudioClip> SfxList { get { return SfxList; } }

        #endregion

        #region Private Fields

        [Tooltip("Context of the SFX of the list")]
        [SerializeField]
        private ESFXContext context = ESFXContext.None;

        [Tooltip("SFX of the type we have")]
        [SerializeField]
        private List<AudioClip> sfxList = new List<AudioClip>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}