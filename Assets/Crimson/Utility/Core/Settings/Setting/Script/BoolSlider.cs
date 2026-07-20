using UnityEngine;


namespace Crimson.Core.Settings
{
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
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnValueChanged(float value)
        {
            EventBus.Publish<OnBoolSettingChanges>(new OnBoolSettingChanges(type, (int)value));
        }

        #endregion
    }
}