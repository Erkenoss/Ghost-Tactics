using Crimson.Core;
using GhostTactics.UI;
using GhostTactics.Data;
using GhostTactics.Core.Combat;
using UnityEngine;
using System.Collections.Generic;
using Crimson.Utilities;

namespace GhostTactics.Core
{
    /// <summary>
    /// The differents type of ability that the player can use in the game
    /// </summary>
    public enum Abilities
    {
        none = 0,
        Idle = 1,
        Backward = 2,
        Forward = 3,
        Dodge = 4,
        Attack = 5,
    }

    /// <summary>
    /// Class use with the Eventbus to manage the reset of the buttonComponent
    /// </summary>
    public class ResetAll
    {

    }

    /// <summary>
    /// Use to start a fight resolution. Use with EventBus to call
    /// </summary>
    public class StartResolution
    {
        #region Public Fields

        public Player Player { get { return player; } }
        public EnnemyData Ennemy { get { return ennemy; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Player of the game
        /// </summary>
        private Player player = null;

        /// <summary>
        /// List of the ennemy
        /// </summary>
        private EnnemyData ennemy = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public StartResolution(Player p, EnnemyData e)
        {
            this.player = p;
            this.ennemy = e;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    /// <summary>
    /// Resolution of the combat with two list of action. Player and Ennemy will fight. Use with EventBus
    /// </summary>
    public class CombatResolutionEvent
    {
        #region Public Fields

        public List<AbilityData> PlayerList { get { return playerList; } }
        public List<AbilityData> EnnemyList { get { return ennemyList; } }
        public EnnemyData Ennemy { get { return ennemy; } }
        public Player Player { get { return player; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// List of the player
        /// </summary>
        private List<AbilityData> playerList = new List<AbilityData>();

        /// <summary>
        /// List of the ennemy
        /// </summary>
        private List<AbilityData> ennemyList = new List<AbilityData>();

        /// <summary>
        /// Ennemy of this fight
        /// </summary>
        private EnnemyData ennemy = null;

        /// <summary>
        /// Player of the game
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
        public CombatResolutionEvent(List<AbilityData> player, List<AbilityData> ennemy, EnnemyData en, Player p)
        {
            this.playerList = player;
            this.ennemyList = ennemy;
            this.ennemy = en;
            this.player = p;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    /// <summary>
    /// Class use with the Eventbus to manage the choice of the player when he select an ability
    /// </summary>
    public class AbilityChoice
    {
        #region Public Fields

        public AbilityData Data { get { return data; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Data pass in the constructor of the class to know which ability is selected by the player
        /// </summary>
        private AbilityData data = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public AbilityChoice (AbilityData data)
        {
            this.data = data;
        }

        #endregion
        
        #region Private Methods
        #endregion
    }

    public class ActionManager : Singleton<ActionManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("List of abilities available in the game.")]
        [SerializeField] 
        private AbilitiesContainer container = null;

        /// <summary>
        /// Component to manage how many buttons can be selected in the level by the player
        /// </summary>
        private ButtonSelectedComponent buttonComponent = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        private void OnDestroy()
        {
            UnSubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the buttonComponent value
        /// </summary>
        /// <param name="component"></param>
        public void UpdateButtonSelectedComponent(ButtonSelectedComponent component)
        {
            if (component == null && buttonComponent == null)
            {
                return;
            }

            buttonComponent = component;
        }

        /// <summary>
        /// Get the AbilityData from the AbilitiesContainer by the name of the ability
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AbilityData GetAbilityByName(string name)
        {
            if (container == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            return container.GetAbilityByName(name);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// /Use as bridge to reset all button in the buttonComponent
        /// </summary>
        /// <param name="all"></param>
        public void ResetAllAbilities(ResetAll all)
        {
            if (buttonComponent == null || all == null)
            {
                return;
            }

            buttonComponent.ResetAllButton();
        }

        /// <summary>
        /// When the player choose an ability, this method is call by the EventBus and update the buttonComponent
        /// </summary>
        /// <param name="choice"></param>
        private void OnAbilityChoice(AbilityChoice choice)
        {
            if (buttonComponent == null || choice == null)
            {
                return;
            }

            buttonComponent.AddAction(choice.Data);
        }

        /// <summary>
        /// When the player come on a new level
        /// </summary>
        /// <param name="nextLevel"></param>
        private void OnNextLevel(NextLevel nextLevel)
        {
            if (buttonComponent == null || nextLevel == null)
            {
                if (buttonComponent == null)
                {
                    Debug.LogError("ButtonComponent is null in ActionManager");
                }

                return;
            }

            buttonComponent.EnableButtonAction(nextLevel.Data.LevelActionSlot);
        }

        /// <summary>
        /// Use to call Fight Resolution with the EventBus and start the resolution of the fight
        /// </summary>
        /// <param name="s"></param>
        private void StartResolution(StartResolution s)
        {
            List<AbilityData> playerAbility = buttonComponent.GetSelectedAbilities();

            if (playerAbility == null || playerAbility.Count == 0 || playerAbility.Count != s.Ennemy.Abilities.Count)
            {
                EventBus.Publish<OnPopUpMessage>(new OnPopUpMessage("You must complete your action bar"));
                return;
            }

            s.Player.UpResult();
            EventBus.Publish<CombatResolutionEvent>(new CombatResolutionEvent(playerAbility, s.Ennemy.Abilities, s.Ennemy, s.Player));
        }

        /// <summary>
        /// Subscribe to the EventBus the differents listeners
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<AbilityChoice>(OnAbilityChoice);
            EventBus.Subscribe<NextLevel>(OnNextLevel);
            EventBus.Subscribe<ResetAll>(ResetAllAbilities);
            EventBus.Subscribe<StartResolution>(StartResolution);
        }

        /// <summary>
        /// Unsubscribe to the EventBus the differents listeners
        /// </summary>
        private void UnSubscribe()
        {
            EventBus.Unsubscribe<AbilityChoice>(OnAbilityChoice);
            EventBus.Unsubscribe<NextLevel>(OnNextLevel);
            EventBus.Unsubscribe<ResetAll>(ResetAllAbilities);
            EventBus.Unsubscribe<StartResolution>(StartResolution);
        }

        #endregion
    }
}