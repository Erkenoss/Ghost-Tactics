using UnityEngine;
using Crimson.Core;
using System;

namespace GhostTactics.Core
{
    public class LoadPlayer
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
        /// PlayerData... Of the player? 
        /// </summary>
        private SavePlayer playerData = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            saveFilePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Load the player data
        /// </summary>
        private void LoadPlayer(LoadPlayer _player)
        {
            if (!System.IO.File.Exists(saveFilePath))
            {
                Player startPlayer = new Player();
                startPlayer.UpdatePlayerBiome(ETypeLevelContainer.Beginning);
                startPlayer.UpdatePlayerLevel(1);
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpdatePlayer(startPlayer);
                }

                return;
            }

            string json = System.IO.File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<SavePlayer>(json);

            if (playerData == null)
            {
                Debug.LogError("Save corrupted, creating new player");

                Player newPlayer = new Player();
                newPlayer.UpdatePlayerBiome(ETypeLevelContainer.Beginning);
                newPlayer.UpdatePlayerLevel(1);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpdatePlayer(newPlayer);
                }

                return;
            }

            Player player = new Player();
            player.UpdatePlayerBiome(playerData.PlayerBiome);
            player.UpdatePlayerLevel(playerData.PlayerLevel);

            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.UpdatePlayer(player);
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
        /// Subscribe the different listener in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<SavePlayer>(SavePlayer);
            EventBus.Subscribe<LoadPlayer>(LoadPlayer);
        }

        /// <summary>
        /// Unsubscribe the different listener in the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<SavePlayer>(SavePlayer);
            EventBus.Unsubscribe<LoadPlayer>(LoadPlayer);
        }



        #endregion
    }
}