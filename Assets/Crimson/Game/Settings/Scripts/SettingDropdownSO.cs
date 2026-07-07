using UnityEngine;

namespace Crimson.Setting
{
    [CreateAssetMenu(fileName = "Setting Dropdown SO", menuName = "Crimson/Setting/Dropdown Setting")]
    public class SettingDropdownSO : SettingSO, ISetting<int>
    {
        public int Choice { get { return choice; } set { choice = value; } }

        /// <summary>
        /// Default value given at the start
        /// </summary>
        [SerializeField]
        private int defaultValue = 0;

        /// <summary>
        /// Value choose by the player
        /// </summary>
        private int choice = 0;

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