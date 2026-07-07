namespace Crimson.Setting
{
    public static class SettingExtention
    {
        /// <summary>
        /// Base on a SettingSO inherit of ISetting, out the variable Choice
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setting"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidCastException"></exception>
        public static T GetChoice<T>(this SettingSO setting)
        {
            if (setting is ISetting<T> typedSetting)
            {
                return typedSetting.Choice;
            }

            throw new System.InvalidCastException($"Setting of type {setting.GetType().Name} does not implement ISetting<{typeof(T).Name}>");
        }

        /// <summary>
        /// Idem as GetChoice except is for set the Choice variable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setting"></param>
        /// <param name="value"></param>
        public static void SetChoice<T>(this SettingSO setting, T value)
        {
            if (setting is ISetting<T> typedSetting)
            {
                typedSetting.Choice = value;
            }
        }
    }
}