using Crimson.Core;
using System;

namespace GhostTactics.Core
{
    public class Player
    {
        #region Public Fields

        public ETypeLevelContainer Biome { get { return biome; } }
        public int CurrentLevel { get { return currentLevel; }  }
        public int TryResult { get { return tryResult; } }

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

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        
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
        public void UpdateResult()
        {
            tryResult++;
        }

        /// <summary>
        /// Save the player
        /// </summary>
        public void Save()
        {
            EventBus.Publish<SavePlayer>(new SavePlayer(this));
        }

        #endregion

        #region Private Methods
        #endregion
    }
}