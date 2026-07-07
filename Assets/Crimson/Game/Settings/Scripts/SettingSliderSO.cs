using UnityEngine;

namespace Crimson.Setting
{
    [CreateAssetMenu(fileName = "Setting Slider SO", menuName = "Crimson/Setting/Slider Setting")]
    public class SettingSliderSO : SettingSO, ISetting<float>
    {
        public float Choice { get { return choice; } set { choice = value; } }

        /// <summary>
        /// Default value of the slider
        /// </summary>
        [SerializeField]
        private float defaultValue = 0f;

        /// <summary>
        /// Value set by the player
        /// </summary>
        private float choice = 0f;

        public override void Apply()
        {

        }

        public override void ResetToDefault()
        {
            choice = defaultValue;
            Apply();
        }
    }
}