using System;
using System.Collections.Generic;
using Crimson.Core.Audio;
using GhostTactics.Core;

namespace Crimson.Core.Settings
{
    public enum SpeedFight
    {
        None = 0,
        VerySlow,
        Slow,
        Normal,
        Fast,
        VeryFast
    }

    public enum SettingBoolType
    {
        None = 0,
        Vibration,
        CameraShake,
        ReduceFlashing,
        Tutorial
    }

    public class OnReceiveLoadSetting
    {
        public Settings Settings = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        public OnReceiveLoadSetting(Settings settings)
        {
            Settings = settings;
        }
    }

    public class OnResetSetting
    {

    }

    public class SaveSetting
    {

    }

    public class OnBoolSettingChanges
    {
        public SettingBoolType Type = SettingBoolType.None;
        public int Value = 0;
    
        public OnBoolSettingChanges(SettingBoolType type, int value)
        {
            Type = type;
            Value = value;
        }
    }

    public class OnChangeSoundSetting
    {
        public EAudio Type = EAudio.None;
        public float Volume = 0.0f;

        public OnChangeSoundSetting(EAudio type, float volume)
        {
            Type = type;
            Volume = volume;
        }
    }
    
    [Serializable]
    public class SoundSetting
    {
        #region Public Fields

        /// <summary>
        /// Volume of the Setting
        /// </summary>
        public float Volume = 0f;

        /// <summary>
        /// Name of the Setting
        /// </summary>
        public string Setting = string.Empty;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="name"></param>
        public SoundSetting(float volume, string name)
        {
            Volume = volume;
            Setting = name;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    [Serializable]
    public class BoolSetting
    {
        #region Public Fields

        /// <summary>
        /// 0 for true, 1 for false
        /// </summary>
        public int Value = 0;

        /// <summary>
        /// Name of the setting
        /// </summary>
        public string Setting = string.Empty;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        public BoolSetting(int value, string name)
        {
            Value = value;
            Setting = name;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    [Serializable]
    public class Settings
    {
        #region Public Fields

        /// <summary>
        /// List of all sound setting in the game
        /// </summary>
        public List<SoundSetting> SoundDictionary = new List<SoundSetting>();

        /// <summary>
        /// List of all setting work as boolean
        /// </summary>
        public List<BoolSetting> BooleanDicitionary = new List<BoolSetting>();

        /// <summary>
        /// The speed fight setting
        /// </summary>
        public string SpeedFight = string.Empty;

        /// <summary>
        /// Fps for the game
        /// </summary>
        public int FPS = 0;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class SettingManager : Singleton<SettingManager>
    {
        #region Public Fields

        public float TargetFPS { get { return targetFPS; } }
        public SpeedFight SpeedFight { get { return speed; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Where the setting will be stocked and saved
        /// </summary>
        private Settings settings = null;

        /// <summary>
        /// target FPS we want to have in the game
        /// </summary>
        private float targetFPS = 0;

        /// <summary>
        /// Speed fight of the game
        /// </summary>
        private SpeedFight speed = SpeedFight.None;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        private void Start()
        {
            EventBus.Publish<OnLoadSettings>(new OnLoadSettings());
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the value and transform int as bool
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetBoolSettingValue(SettingBoolType type)
        {
            if (settings == null)
            {
                return false;
            }

            if (settings.BooleanDicitionary != null && settings.BooleanDicitionary.Count > 0)
            {
                foreach (BoolSetting boo in settings.BooleanDicitionary)
                {
                    if (Enum.TryParse(boo.Setting, out SettingBoolType t))
                    {
                        if (t == type)
                        {
                            if (boo.Value == 0)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load all settings of the game
        /// </summary>
        private void LoadSetting(OnReceiveLoadSetting setting)
        {
            if (setting.Settings == null)
            {
                InitSettings();
                return;
            }

            settings = setting.Settings;

            if (settings.SoundDictionary != null &&  settings.SoundDictionary.Count > 0)
            {
                foreach (SoundSetting soundSetting in settings.SoundDictionary)
                {
                    if (Enum.TryParse(soundSetting.Setting, out EAudio audioType))
                    {
                        EventBus.Publish<OnSetMixerValue>(new OnSetMixerValue(audioType, soundSetting.Volume, true));
                    }
                }
            }

            if (settings.BooleanDicitionary != null && settings.BooleanDicitionary.Count > 0)
            {
                foreach (BoolSetting boo in settings.BooleanDicitionary)
                {
                    if (Enum.TryParse(boo.Setting, out SettingBoolType type))
                    {
                        // New event to apply setting here
                    }
                }
            }

            targetFPS = settings.FPS;

            if (Enum.TryParse(settings.SpeedFight, out SpeedFight value))
            {
                speed = value;
            }
            else
            {
                speed = SpeedFight.Normal;
            }
        }

        /// <summary>
        /// Save the setting of the game
        /// </summary>
        private void SaveSetting(SaveSetting save)
        {
            if (settings == null)
            {
                return;
            }

            EventBus.Publish<OnSaveSettings>(new OnSaveSettings(settings));
        }

        /// <summary>
        /// Reset all the settings with the base parameter
        /// </summary>
        private void ResetSettings(OnResetSetting reset)
        {

        }

        /// <summary>
        /// Init all the Settings for the game
        /// </summary>
        private void InitSettings()
        {
            if (settings == null)
            {
                settings = new Settings();
            }

            foreach (SettingBoolType set in Enum.GetValues(typeof(SettingBoolType)))
            {
                if (set == SettingBoolType.None)
                {
                    continue;
                }

                settings.BooleanDicitionary.Add(new BoolSetting(0, set.ToString()));
                // New event to apply setting here
            }

            foreach (EAudio audio in Enum.GetValues(typeof(EAudio)))
            {
                if (audio == EAudio.None)
                {
                    continue;
                }

                settings.SoundDictionary.Add(new SoundSetting(0.5f, audio.ToString()));
                EventBus.Publish<OnSetMixerValue>(new OnSetMixerValue(audio, 0.5f, true));
            }

            settings.FPS = 60;
            settings.SpeedFight = SpeedFight.Normal.ToString();

            targetFPS = settings.FPS;
            speed = SpeedFight.Normal;

            EventBus.Publish<OnSaveSettings>(new OnSaveSettings(settings));
        }

        /// <summary>
        /// Change in the setting a value of a sound slider
        /// </summary>
        /// <param name="change"></param>
        private void ChangeSoundSetting(OnChangeSoundSetting change)
        {
            if (change == null || settings == null || settings.SoundDictionary == null || settings.SoundDictionary.Count == 0)
            {
                return;
            }

            string audioToChange = change.Type.ToString();

            foreach (SoundSetting soundSetting in settings.SoundDictionary)
            {
                if (audioToChange == soundSetting.Setting)
                {
                    soundSetting.Volume = change.Volume;
                    break;
                }
            }
        }

        /// <summary>
        /// Change a setting with work as a bool
        /// </summary>
        /// <param name="b"></param>
        private void ChangeBoolSetting(OnBoolSettingChanges b)
        {
            if (b == null || settings == null || settings.BooleanDicitionary == null || settings.BooleanDicitionary.Count == 0)
            {
                return;
            }

            string boolToChange = b.Type.ToString();

            foreach (BoolSetting set in settings.BooleanDicitionary)
            {
                if (boolToChange == set.Setting)
                {
                    set.Value = b.Value;
                    break;
                }
            }
        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnResetSetting>(ResetSettings);
            EventBus.Subscribe<SaveSetting>(SaveSetting);
            EventBus.Subscribe<OnReceiveLoadSetting>(LoadSetting);
            EventBus.Subscribe<OnChangeSoundSetting>(ChangeSoundSetting);
            EventBus.Subscribe<OnBoolSettingChanges>(ChangeBoolSetting);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnResetSetting>(ResetSettings);
            EventBus.Unsubscribe<SaveSetting>(SaveSetting);
            EventBus.Unsubscribe<OnReceiveLoadSetting>(LoadSetting);
            EventBus.Unsubscribe<OnChangeSoundSetting>(ChangeSoundSetting);
            EventBus.Unsubscribe<OnBoolSettingChanges>(ChangeBoolSetting);
        }

        #endregion
    }
}