using Crimson.Core.Audio;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


namespace Crimson.Core.Settings
{
    public class OnUpdateBoolSlider
    {
        public int Value = 0;
        public SettingBoolType Type = SettingBoolType.None;

        public OnUpdateBoolSlider(int value, SettingBoolType type)
        {
            Value = value;
            Type = type;
        }
    }


    public class BoolSlider : SliderParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Type of setting we will change with this slider")]
        [SerializeField]
        private SettingBoolType type = SettingBoolType.None;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            if (sld == null || SettingManager.Instance == null)
            {
                return;
            }

            int value = SettingManager.Instance.GetBoolSettingValue(type) ? 1 : 0; 

            sld.SetValueWithoutNotify(value);
            UpdateTextDisplay(value);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Update the value of the slider
        /// </summary>
        /// <param name="slider"></param>
        private void UpdateSlider(OnUpdateBoolSlider slider)
        {
            if (sld == null || slider == null || slider.Type != type || text == null)
            {
                return;
            }

            sld.SetValueWithoutNotify(slider.Value);
            UpdateTextDisplay(sld.value);
        }

        protected override void UpdateTextDisplay(float value)
        {
            if (text == null)
            {
                return;
            }

            if (value > 0)
            {
                text.text = "ON";
            }
            else
            {
                text.text = "OFF";
            }
        }

        protected override void OnValueChanged(float value)
        {
            base.OnValueChanged(value);
            EventBus.Publish<OnBoolSettingChanges>(new OnBoolSettingChanges(type, (int)value));
        }

        protected override void Subscribed()
        {
            base.Subscribed();

            EventBus.Subscribe<OnUpdateBoolSlider>(UpdateSlider);
        }

        protected override void Unsubscribe()
        {
            base.Unsubscribe();

            EventBus.Unsubscribe<OnUpdateBoolSlider>(UpdateSlider);
        }

        #endregion
    }
}