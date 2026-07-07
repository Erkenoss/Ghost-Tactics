using UnityEngine;
using Crimson.Core;
using System.Collections.Generic;

namespace Crimson.Setting
{
    public enum SettingType
    {
        None,
    }

    public class SettingsManager : Singleton<SettingsManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Contains all SO in a list
        /// </summary>
        [SerializeField]
        private ContainerSettingSO container = null;

        /// <summary>
        /// Contains all setting in the game
        /// </summary>
        private Dictionary<SettingType, SettingSO> settingDictionary = new Dictionary<SettingType, SettingSO>();

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            if (container != null)
            {
                InitDict();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Init a setting in a dict
        /// </summary>
        /// <param name="type"></param>
        /// <param name="so"></param>
        public void AddInDict(SettingType type, SettingSO so)
        {
            if (!settingDictionary.ContainsKey(type) && so != null)
            {
                settingDictionary[type] = so;
            }
        }

        /// <summary>
        /// Get the value of a child of every setting in settingDictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public T GetSettingValue<T>(SettingType type)
        {
            if (settingDictionary.TryGetValue(type, out SettingSO setting))
            {
                return setting.GetChoice<T>();
            }

            throw new KeyNotFoundException($"No setting of type {type} found.");
        }

        /// <summary>
        /// Set the value of a setting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void SetSettingValue<T>(SettingType type, T value)
        {
            if (settingDictionary.TryGetValue(type, out SettingSO setting))
            {
                setting.SetChoice<T>(value);
            }
        }

        /// <summary>
        /// Apply the effect of the setting depending the SettingType
        /// </summary>
        /// <param name="type"></param>
        public void ApplySettling(SettingType type)
        {
            if (settingDictionary.ContainsKey(type))
            {
                
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Init the values of the dict settinggDictionary
        /// </summary>
        private void InitDict()
        {
            if (container.Settings != null && container.Settings.Count > 0)
            {
                foreach (SettingSO setting in container.Settings)
                {
                    AddInDict(setting.Type, setting);
                }
            }
        }

        #endregion
    }
}