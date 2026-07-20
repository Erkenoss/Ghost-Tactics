using I2.Loc;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crimson.Core.Settings.Languages
{
    public enum ELangs
    {
        None,
        English,
        French,
        Polish,
        Spanish
    }

    public class OnChangeLanguage
    {
        public ELangs Langs = ELangs.None;

        /// <summary>
        /// Construtor
        /// </summary>
        public OnChangeLanguage(ELangs newLang)
        {
            Langs = newLang;
        }
    }

    public class LanguagesManager : Singleton<LanguagesManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Current language
        /// </summary>
        protected ELangs currentLang = ELangs.None;

        [Tooltip("Color for the different link")]
        [SerializeField]
        protected Color linkColor = Color.white;

        /// <summary>
        /// Color in hexa base on linkColor to the link
        /// </summary>
        protected string colorHex = string.Empty;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();
            colorHex = ColorToHex();
            ChangeLanguages(new OnChangeLanguage(ELangs.English));

            SceneManager.sceneLoaded += OnSceneLoad;
            Subscribe();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply language when load a scene
        /// </summary>
        public void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            ChangeLanguages(new OnChangeLanguage(currentLang));
        }

        /// <summary>
        /// Change the current languages by enum
        /// </summary>
        public void ChangeLanguages(OnChangeLanguage language)
        {
            if (language.Langs == ELangs.None)
            {
                return;
            }

            if (currentLang != language.Langs)
            {
                currentLang = language.Langs;
            }

            LocalizationManager.CurrentLanguage = currentLang.ToString();
            LocalizationManager.LocalizeAll();
        }

        /// <summary>
        /// Get a translation base on a string key, generaly Enum by ToString() key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetTranslation(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return LocalizationManager.GetTranslation(key);
        }

        /// <summary>
        /// Create a uniq link
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page"></param>
        /// <param name="SubKeys"></param>
        /// <returns></returns>
        public string CreateLink<T>(string text, T subKeys) where T : Enum
        {
            return $"<link=\"{subKeys.ToString()}\"><color={colorHex}>{text}</color></link>";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Change a color in hexadecimal value
        /// </summary>
        /// <returns></returns>
        private string ColorToHex()
        {
            Color32 c32 = linkColor;
            return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}";
        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnChangeLanguage>(ChangeLanguages);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnChangeLanguage>(ChangeLanguages);
        }

        #endregion
    }
}