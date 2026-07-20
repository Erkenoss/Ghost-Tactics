using Crimson.Core;
using System.Collections.Generic;
using GhostTactics.Data;

namespace GhostTactics.Core
{
    public class OnCleanGhost
    {

    }

    public class OnRemoveGhostAction
    {
        public AbilityData Data = null;

        public OnRemoveGhostAction(AbilityData data)
        {
            Data = data;
        }
    }

    public class Player
    {
        #region Public Fields

        public ETypeLevelContainer Biome { get { return biome; } }
        public int CurrentLevel { get { return currentLevel; }  }
        public int TryResult { get { return tryResult; } }
        public int VisualizationValue { get { return visualizationValue; } }
        public int Gender { get { return gender; } }
        public bool HasBeenAlreadyCreated { get { return hasBeenAlreadyCreated; } }
        public Ghost PlayerGhost { get { return playerGhost; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Biome where the player is
        /// </summary>
        private ETypeLevelContainer biome = ETypeLevelContainer.None;

        /// <summary>
        /// Current level of the player
        /// </summary>
        private int currentLevel = 0;

        /// <summary>
        /// Result of the player try in the level
        /// </summary>
        private int tryResult = 0;

        /// <summary>
        /// Visualization value of the player, used to determine how many time the player can previsualized the pattern of the ennemy.
        /// </summary>
        private int visualizationValue = 0;

        /// <summary>
        /// Gender of the player, 0 for male, 1 for female
        /// </summary>
        private int gender = 0;

        /// <summary>
        /// True, if the plauyer has already choose his gender. Else, false
        /// </summary>
        private bool hasBeenAlreadyCreated = false;

        /// <summary>
        /// Reference of the ghost that the player is currently playing with
        /// </summary>
        private Ghost playerGhost = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Create a ghost for the player
        /// </summary>
        public void CreateGhost(List<string> abilities)
        {
            if (playerGhost == null)
            {
                playerGhost = new Ghost();

                if (abilities == null || abilities.Count == 0)
                {
                    return;
                }
                
                playerGhost.GetAbilityData(abilities);
            }
        }

        /// <summary>
        /// Updates the abilities of the player's ghost
        /// </summary>
        /// <param name="abilities"></param>
        public void UpdateGhostAbilities(List<AbilityData> abilities)
        {
            if (playerGhost == null)
            {
                return;
            }

            playerGhost.AddAction(abilities);
        }

        /// <summary>
        /// Update the biome of the player
        /// </summary>
        /// <param name="bio"></param>
        public void UpdatePlayerBiome(ETypeLevelContainer bio)
        {
            biome = bio;
        }
        
        /// <summary>
        /// Update the level of the player
        /// </summary>
        /// <param name="level"></param>
        public void UpdatePlayerLevel(int level)
        {
            currentLevel = level;
        }

        /// <summary>
        /// Update the number of try of the player
        /// </summary>
        /// <param name="result"></param>
        public void UpdateResult(int result)
        {
            tryResult = result;
        }

        /// <summary>
        /// Up the number of try of the player
        /// </summary>
        public void UpResult()
        {
            tryResult++;
        }

        /// <summary>
        /// Update the visualizationValue variable
        /// </summary>
        /// <param name="visualizationValue"></param>
        public void UpdateVisualizationValue(int vis)
        {
            visualizationValue = vis;
        }

        /// <summary>
        /// Up the visualization value of the player    
        /// </summary>
        /// <param name="vis"></param>
        public void UpVisualizationValue(int vis)
        {
            visualizationValue += vis;
        }

        /// <summary>
        /// Update the gender of the player
        /// </summary>
        /// <param name="g"></param>
        public void UpdateGender(int g)
        {
            gender = g;
        }

        /// <summary>
        /// Update when the player choose his gender
        /// </summary>
        /// <param name="created"></param>
        public void UpdateHasBeenAlreadyCreated(bool created)
        {
            hasBeenAlreadyCreated = created;
        }

        /// <summary>
        /// Clean the Ability llist of the ghost
        /// </summary>
        public void CleanGhost(OnCleanGhost g)
        {
            if (PlayerGhost == null)
            {
                return;
            }

            playerGhost.ClearActionsList();
        }

        /// <summary>
        /// Remove the ability given in the ghost
        /// </summary>
        /// <param name="action"></param>
        public void RemoveGhostAction(OnRemoveGhostAction action)
        {
            if (action.Data == null || playerGhost == null)
            {
                return;
            }

            playerGhost.RemoveAction(action.Data);
        }

        /// <summary>
        /// Save the player
        /// </summary>
        public void Save()
        {
            EventBus.Publish<SavePlayer>(new SavePlayer(this));
        }

        /// <summary>
        /// Subscribe with the EventBus
        /// </summary>
        public void Subscribe()
        {
            EventBus.Subscribe<OnCleanGhost>(CleanGhost);
            EventBus.Subscribe<OnRemoveGhostAction>(RemoveGhostAction);
        }

        #endregion

        #region Private Methods
        #endregion
    }
}