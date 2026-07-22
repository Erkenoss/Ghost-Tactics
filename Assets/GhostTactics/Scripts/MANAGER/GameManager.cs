using Crimson.Core;
using Crimson.Core.Scenes;
using GhostTactics.Data;
using UnityEngine;
using System.Collections.Generic;
using GhostTactics.UI;

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

    public class Visualization
    {

    }

    public class OnSwitchLevel
    {

    }

    public class OnGhostAction
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Ghost of the player
        /// </summary>
        public Ghost Ghost = null;

        /// <summary>
        /// Action we will use with the ghost
        /// </summary>
        public AbilityData Action = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ghost"></param>
        public OnGhostAction(Ghost ghost, AbilityData action)
        {
            Ghost = ghost;
            Action = action;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    /// <summary>
    /// Class use with the Eventbus to manage the end of the level and update the buttonComponent with the new number of action that the player can select in the next level
    /// </summary>
    public class NextLevel
    {
        #region Public Fields

        public LevelData Data { get { return data; } }
        public Player Player { get { return player; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Data of this class
        /// </summary>
        private LevelData data = null;

        /// <summary>
        /// The player currently playing
        /// </summary>
        private Player player = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public NextLevel(LevelData data, Player player)
        {
            this.data = data;
            this.player = player;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class GameManager : Singleton<GameManager>
    {
        #region Public Fields

        public LevelData CurrentLevel { get { return currentLevel; } private set { } }
        public Player Player { get { return player; } private set { } }

        #endregion

        #region Private Fields

        [Tooltip("Scene to load as test")]
        [SerializeField]
        private SceneGroupSO gameGroup = null;

        [Tooltip("All Level Container in the game")]
        [SerializeField]
        private List<LevelContainer> containers = new List<LevelContainer>();

        /// <summary>
        /// The current container of the current level
        /// </summary>
        private LevelContainer currentContainer = null;

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
            EventBus.Publish<OnLoadPlayer>(new OnLoadPlayer());
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
        /// Update the PlayerGhost with the new abilities selected by the player in the level
        /// </summary>
        /// <param name="ghostAbilities"></param>
        public void UpdatePlayerGhost(List<AbilityData> ghostAbilities)
        {
            if (player == null)
            {
                return;
            }

            player.UpdateGhostAbilities(ghostAbilities);
            player.UpResult();

            SavePlayer(new OnSavePlayer());
            LoadLevel();
        }

        /// <summary>
        /// Pass by player to save the Player
        /// </summary>
        public void SavePlayer(OnSavePlayer e)
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
        public void LoadLevel(StartGameEvent game)
        {
            if (player == null)
            {
                return;
            }

            if (!player.HasBeenAlreadyCreated)
            {
                EventBus.Publish<OnNeedCharacterGender>(new OnNeedCharacterGender());
                return;
            }

            currentLevel = GetContainer(player.Biome, player.CurrentLevel);

            if (currentLevel == null)
            {
                return;
            }
            
            EventBus.Publish<OnSceneToLoad>(new OnSceneToLoad(gameGroup));
        }

        /// <summary>
        /// Load the level base on the current level
        /// </summary>
        public void LoadLevel()
        {
            EventBus.Publish<NextLevel>(new NextLevel(currentLevel, player));
        }
        
        /// <summary>
        /// Confirm a try by the player
        /// </summary>
        /// <param name="confirm"></param>
        public void Confirm(ConfirmTry confirm)
        {
            EventBus.Publish<StartResolution>(new StartResolution(player, currentLevel.EnnemyLevel));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// When the ghost can dodge, slow the time in the game to use the dodge of the ghost
        /// </summary>
        /// <param name="g"></param>
        private void GhostDodge(OnGhostAction g)
        {
            if (g == null)
            {
                return;
            }

            Debug.Log("Combat paused: Ghost can dodge");
        }

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
            currentContainer = container;
            
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
        /// Change to pass at the next level
        /// </summary>
        private void SwitchLevel(OnSwitchLevel e)
        {
            if (e == null || currentLevel == null)
            {
                return;
            }

            LevelData next = GetContainer(currentLevel.BiomeType, currentLevel.LevelNumber + 1);

            if (next == null)
            {
                LevelContainer nextContainer = GetLevelContainer(currentContainer);
                next = nextContainer.Container[0];
                
                currentContainer = nextContainer;
                currentLevel = next;
            }

            currentLevel = next;
            LoadLevel();
        }

        /// <summary>
        /// Return the LevelContainer based on the previous container
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private LevelContainer GetLevelContainer(LevelContainer current)
        {
            if (containers == null || containers.Count == 0 || current == null)
            {
                return null;
            }

            for (int i = 0; i < containers.Count - 1; i++)
            {
                if (containers[i] == current)
                {
                    return containers[i + 1];
                }
            }

            return null;
        }

        /// <summary>
        /// Use to visualized the action of an ennemy
        /// </summary>
        /// <param name="v"></param>
        private void Visualized(Visualization v)
        {
            if (currentLevel == null)
            {
                return;
            }

            EnnemyData ennemy = currentLevel.EnnemyLevel;

            if (ennemy == null || ennemy.Abilities == null || ennemy.Abilities.Count == 0)
            {
                return;
            }

            foreach (AbilityData data in ennemy.Abilities)
            {
                Debug.Log(data.Ability);
            }
        }

        /// <summary>
        /// When the gamme group is loaded
        /// </summary>
        /// <param name="e"></param>
        private void SceneGroupLoaded(OnSceneGroupLoaded e)
        {
            if (e == null || e.Group != gameGroup || currentLevel == null || player == null)
            {
                return;
            }

            EventBus.Publish<NextLevel>(new NextLevel(currentLevel, player));
        }

        /// <summary>
        /// Subscribe the listener in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<StartGameEvent>(LoadLevel);
            EventBus.Subscribe<OnSceneGroupLoaded>(SceneGroupLoaded);

            EventBus.Subscribe<OnSavePlayer>(SavePlayer);
            EventBus.Subscribe<OnSwitchLevel>(SwitchLevel);
            
            EventBus.Subscribe<ConfirmTry>(Confirm);
            EventBus.Subscribe<Visualization>(Visualized);

            EventBus.Subscribe<OnGhostAction>(GhostDodge);
        }

        /// <summary>
        /// Unsubscribe the listener in the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<StartGameEvent>(LoadLevel);
            EventBus.Unsubscribe<OnSceneGroupLoaded>(SceneGroupLoaded);
            
            EventBus.Unsubscribe<OnSavePlayer>(SavePlayer);
            EventBus.Unsubscribe<OnSwitchLevel>(SwitchLevel);
            
            EventBus.Unsubscribe<ConfirmTry>(Confirm);
            EventBus.Unsubscribe<Visualization>(Visualized);

            EventBus.Unsubscribe<OnGhostAction>(GhostDodge);
        }

        #endregion
    }
}