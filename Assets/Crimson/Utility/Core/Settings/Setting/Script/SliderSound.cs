using Crimson.Core.Audio;
using UnityEngine;

namespace Crimson.Core.Settings
{
    public class SliderSound : SliderParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Type of this Audio Slider")]
        [SerializeField]
        private EAudio type = EAudio.None;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            if (sld == null || AudioManager.Instance == null)
            {
                return;
            }

            sld.SetValueWithoutNotify(AudioManager.Instance.GetVolume(type));
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnValueChanged(float value)
        {
            EventBus.Publish<OnSetMixerValue>(new OnSetMixerValue(type, value, false));
        }

        #endregion
    }
}