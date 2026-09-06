using UnityEngine;
using Crimson.Core;
using System;
using System.Collections.Generic;
using Crimson.Core.Settings;

namespace GhostTactics.Core
{
    public class OnResetPlayer
    {

    }

    public class OnSavePlayer
    {

    }

    public class OnLoadPlayer
    {

    }

    public class OnSaveSettings
    {
        public Settings Setting = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="set"></param>
        public OnSaveSettings(Settings set)
        {
            Setting = set;
        }
    }

    public class OnLoadSettings
    {

    }


    [Serializable]
    public class SavePlayer
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// biome of the player we want to save
        /// </summary>
        public ETypeLevelContainer PlayerBiome = ETypeLevelContainer.None;

        /// <summary>
        /// Level in the biome of the player we want to save
        /// </summary>
        public int PlayerLevel = 0;

        /// <summary>
        /// How much time the player can visualized
        /// </summary>
        public int VisualizationValue = 0;

        /// <summary>
        /// Gender of the player we want to save, 0 for male, 1 for female
        /// </summary>
        public int PlayerGender = 0;

        /// <summary>
        /// Use to know if the player has been already created or not
        /// </summary>
        public bool HasBeenAlreadyCreated = false;

        /// <summary>
        /// Result of the player try in the level we want to save
        /// </summary>
        public int PlayerTryResult = 0;

        /// <summary>
        /// Reference of the ghost that the player is currently playing with
        /// </summary>
        public List<string> GhostActions = new List<string>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="biome"></param>
        /// <param name="level"></param>
        public SavePlayer(Player player)
        {
            PlayerBiome = player.Biome;
            PlayerLevel = player.CurrentLevel;
            VisualizationValue = player.VisualizationValue;
            PlayerGender = player.Gender;
            PlayerTryResult = player.TryResult;
            GhostActions = player.PlayerGhost.AbilitiesName;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class SaveManager : Singleton<SaveManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Path of the save file
        /// </summary>
        private string saveFilePath = string.Empty;

        /// <summary>
        /// Path of the settings save file
        /// </summary>
        private string settingsFilePath = string.Empty;

        /// <summary>
        /// PlayerData... Of the player? 
        /// </summary>
        private SavePlayer playerData = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            saveFilePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
            settingsFilePath = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Use to debug the save file
        /// </summary>
        public void DebugReadSaveFile()
        {
            if (!System.IO.File.Exists(saveFilePath))
            {
                Debug.LogWarning("No save file found");
                return;
            }

            string json = System.IO.File.ReadAllText(saveFilePath);
            Debug.Log($"SAVE JSON:\n{json}");
        }

        /// <summary>
        /// Reset the different value in the player
        /// </summary>
        public void ResetPlayer(OnResetPlayer player)
        {
            Player startPlayer = new Player();
            startPlayer.UpdatePlayerBiome(ETypeLevelContainer.Beginning);
            startPlayer.UpdatePlayerLevel(1);
            startPlayer.UpdateVisualizationValue(0);
            startPlayer.UpdateResult(0);
            startPlayer.UpdateGender(1);
            startPlayer.CreateGhost(null);
            startPlayer.Save();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayer(startPlayer);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load the player data
        /// </summary>
        private void LoadPlayer(OnLoadPlayer _player)
        {
            if (!System.IO.File.Exists(saveFilePath))
            {
                ResetPlayer(null);   
                return;
            }

            string json = System.IO.File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<SavePlayer>(json);

            if (playerData == null)
            {
                ResetPlayer(null);
                return;
            }

            Player player = new Player();

            player.UpdatePlayerBiome(playerData.PlayerBiome);
            player.UpdatePlayerLevel(playerData.PlayerLevel);
            player.UpdateVisualizationValue(playerData.VisualizationValue);
            player.UpdateResult(playerData.PlayerTryResult);
            player.UpdateGender(playerData.PlayerGender);
            player.CreateGhost(playerData.GhostActions);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayer(player);
            }
        }

        /// <summary>
        /// Load the Setting of the game
        /// </summary>
        /// <param name="set"></param>
        private void LoadSetting(OnLoadSettings set)
        {
            if (!System.IO.File.Exists(settingsFilePath))
            {
                EventBus.Publish<OnReceiveLoadSetting>(new OnReceiveLoadSetting(null));
                return;
            }

            string json = System.IO.File.ReadAllText(settingsFilePath);
            Settings settings = JsonUtility.FromJson<Settings>(json);
        
            EventBus.Publish<OnReceiveLoadSetting>(new OnReceiveLoadSetting(settings));
        }

        /// <summary>
        /// Save the data of the player
        /// </summary>
        /// <param name="savePlayer"></param>
        private void SavePlayer(SavePlayer savePlayer)
        {
            string json = JsonUtility.ToJson(savePlayer, true);
            System.IO.File.WriteAllText(saveFilePath, json);

            DebugReadSaveFile();
        }

        /// <summary>
        /// Save the Settings of the game
        /// </summary>
        /// <param name="setting"></param>
        private void SaveSettings(OnSaveSettings setting)
        {
            if (setting == null || setting.Setting == null)
            {
                return;
            }

            string json = JsonUtility.ToJson(setting.Setting, true);
            System.IO.File.WriteAllText(settingsFilePath, json);
        }

        /// <summary>
        /// Subscribe the different listener in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<SavePlayer>(SavePlayer);
            EventBus.Subscribe<OnLoadPlayer>(LoadPlayer);
            EventBus.Subscribe<OnSaveSettings>(SaveSettings);
            EventBus.Subscribe<OnLoadSettings>(LoadSetting);
            EventBus.Subscribe<OnResetPlayer>(ResetPlayer);
        }

        /// <summary>
        /// Unsubscribe the different listener in the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<SavePlayer>(SavePlayer);
            EventBus.Unsubscribe<OnLoadPlayer>(LoadPlayer);
            EventBus.Unsubscribe<OnSaveSettings>(SaveSettings);
            EventBus.Unsubscribe<OnLoadSettings>(LoadSetting);
            EventBus.Unsubscribe<OnResetPlayer>(ResetPlayer);
        }

        #endregion
    }
}