using UnityEngine;

namespace Crimson.Setting
{
    [CreateAssetMenu(fileName = "Setting Toogle SO", menuName = "Crimson/Setting/Toogle Setting")]
    public class SettingToogleSO : SettingSO, ISetting<bool>
    {
        public bool Choice { get { return choice; } set { choice = value; } }

        /// <summary>
        /// Use to reset
        /// </summary>
        [SerializeField]
        private bool defaultValue = false;

        /// <summary>
        /// Value choose by the player
        /// </summary>
        private bool choice = false;

        /// <summary>
        /// Apply the setting depending choice
        /// </summary>
        public override void Apply()
        {

        }

        /// <summary>
        /// Apply defaultValue when reset
        /// </summary>
        public override void ResetToDefault()
        {
            choice = defaultValue;
            Apply();
        }
    }
}