using UnityEngine;

namespace Crimson.Setting
{
    public abstract class SettingSO : ScriptableObject
    {
        public SettingType Type { get { return type; } set { type = value; } }

        /// <summary>
        /// Type of the setting
        /// </summary>
        [SerializeField]
        private SettingType type = SettingType.None;

        public abstract void Apply();
        public abstract void ResetToDefault();
    }
}