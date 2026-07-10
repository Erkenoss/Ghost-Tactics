using Crimson.Core;
using Crimson.Core.Scenes;
using GhostTactics.Data;
using UnityEngine;
using System.Collections.Generic;
using GhostTactics.Ennemi;

namespace GhostTactics.Core
{
    /// <summary>
    /// The different type of level Container.
    /// </summary>
    public enum ETypeLevelContainer
    {
        None = 0,
        Beginning,
        Middle,
        End
    }

    /// <summary>
    /// The different state of the game
    /// </summary>
    public enum EGameState
    {
        none = 0,
        Playing,
        Paused,
        GameOver
    }

    public class StartGameEvent
    {

    }

    public class ConfirmTry
    {

    }

    /// <summary>
    /// Class use with the Eventbus to manage the end of the level and update the buttonComponent with the new number of action that the player can select in the next level
    /// </summary>
    public class NextLevel
    {
        #region Public Fields

        public LevelData Data { get { return data; } }
        public int TryResult {  get { return tryResult; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Data of this class
        /// </summary>
        private LevelData data = null;

        /// <summary>
        /// Result of the previous level
        /// </summary>
        private int tryResult = 0;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public NextLevel(LevelData data)
        {
            this.data = data;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class GameManager : Singleton<GameManager>
    {
        #region Public Fields

        public LevelData CurrentLevel { get { return currentLevel; } }

        #endregion

        #region Private Fields

        [Tooltip("Scene to load as test")]
        [SerializeField]
        private SceneGroupSO gameGroup = null;

        [Tooltip("All Level Container in the game")]
        [SerializeField]
        private List<LevelContainer> containers = new List<LevelContainer>();

        /// <summary>
        /// Level currently travelled by the player. It will be updated at the end of each level and use to load the next level
        /// </summary>
        private LevelData currentLevel = null;

        /// <summary>
        /// Player currently in the game
        /// </summary>
        private Player player = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        private void Start()
        {
            EventBus.Publish<LoadPlayer>(new LoadPlayer());
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the value of player to manage the evolution of the player in the game
        /// </summary>
        /// <param name="player"></param>
        public void UpdatePlayer(Player _player)
        {
            player = _player;
        }

        /// <summary>
        /// Pass by player to save the Player
        /// </summary>
        public void SavePlayer(EnnemiDieEvent e)
        {
            if (player == null)
            {
                return;
            }

            player.Save();
        }
        
        /// <summary>
        /// Load the level base on the current level in the PlayerData
        /// </summary>
        /// <param name="game"></param>
        public async void LoadLevel(StartGameEvent game)
        {
            if (CrimsonSceneManager.Instance == null || gameGroup == null)
            {
                return;
            }

            await CrimsonSceneManager.Instance.UnloadCurrentGroup();
            await CrimsonSceneManager.Instance.LoadGroupAsync(gameGroup);

            if (player != null)
            {
                LevelData level = GetContainer(player.Biome, player.CurrentLevel);
                currentLevel = level;

                if (level != null)
                {
                    EventBus.Publish<NextLevel>(new NextLevel(level));
                }
            }
        }

        /// <summary>
        /// Confirm a try by the player
        /// </summary>
        /// <param name="confirm"></param>
        public void Confirm(ConfirmTry confirm)
        {
            player.UpdateResult();
            EventBus.Publish<StartResolution>(new StartResolution(player, currentLevel.EnnemyLevel));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Return the level container base on the current level in the PlayerData and the type of the container we need
        /// </summary>
        /// <returns></returns>
        private LevelData GetContainer(ETypeLevelContainer type, int level)
        {
            if (containers == null || containers.Count == 0)
            {
                return null;
            }

            LevelContainer container = containers.Find(c => c.Type == type);
            
            if (container == null)
            {
                return null;
            }

            LevelData tempLevel = container.Container.Find(l => l.LevelNumber == level);

            if (tempLevel == null)
            {
                return null;
            }

            return tempLevel;
        }

        /// <summary>
        /// Subscribe the listener in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<StartGameEvent>(LoadLevel);
            EventBus.Subscribe<EnnemiDieEvent>(SavePlayer);
            EventBus.Subscribe<ConfirmTry>(Confirm);
        }

        /// <summary>
        /// Unsubscribe the listener in the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<StartGameEvent>(LoadLevel);
            EventBus.Unsubscribe<EnnemiDieEvent>(SavePlayer);
            EventBus.Unsubscribe<ConfirmTry>(Confirm);
        }

        #endregion
    }
}