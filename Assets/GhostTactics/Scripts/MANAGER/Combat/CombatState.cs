using UnityEngine;

namespace GhostTactics.Core.Combat
{
    public class CombatState
    {
        #region Public Fields

        /// <summary>
        /// Where the player is in this state
        /// </summary>
        public int PlayerPosition = 0;

        /// <summary>
        /// Where the ennemy is in this state
        /// </summary>
        public int EnnemyPosition = 0;

        /// <summary>
        /// The ennemy health
        /// </summary>
        public int EnnemyHealth = 0;

        /// <summary>
        /// Is the player alive or not? 
        /// </summary>
        public bool PlayerAlive = true;

        /// <summary>
        /// Return the distance between ennemy and player. Use to know if an attack touch
        /// </summary>
        public int Distance => Mathf.Abs(EnnemyPosition - PlayerPosition);

        /// <summary>
        /// The current step of the fight
        /// </summary>
        public int CurrentStepIndex = 0;

        /// <summary>
        /// Is the position has swapped between player and ennemy
        /// </summary>
        public bool PositionSwapped = false;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="playerPos"></param>
        /// <param name="ennemyPos"></param>
        /// <param name="ennemyHealth"></param>
        public CombatState(int playerPos, int ennemyPos, int ennemyHealth)
        {
            PlayerPosition = playerPos;
            EnnemyPosition = ennemyPos;
            EnnemyHealth = ennemyHealth;
            PlayerAlive = true;
            CurrentStepIndex = 0;
        }

        /// <summary>
        /// Create a Clone to continu the flow of the fight
        /// </summary>
        /// <returns></returns>
        public CombatState Clone()
        {
            return new CombatState(PlayerPosition, EnnemyPosition, EnnemyHealth)
            {
                PlayerAlive = this.PlayerAlive,
                CurrentStepIndex = this.CurrentStepIndex
            };
        }

        #endregion

        #region Private Methods
        #endregion



    }
}