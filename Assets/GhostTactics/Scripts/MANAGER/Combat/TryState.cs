using UnityEngine;

namespace GhostTactics.Core.Combat
{
    public class TryState
    {
        #region Public Fields

        /// <summary>
        /// The current step of the fight
        /// </summary>
        public int StepIndex = 0;

        /// <summary>
        /// The abilities use by the player
        /// </summary>
        public Abilities PlayerAbility = Abilities.none;

        /// <summary>
        /// The abilities use by the ennemy
        /// </summary>
        public Abilities EnnemyAbility = Abilities.none;

        /// <summary>
        /// Where the player start in this try
        /// </summary>
        public int PlayerStartPosition = 0;

        /// <summary>
        /// Where the ennemy start in this try
        /// </summary>
        public int EnnemyStartPosition = 0;

        /// <summary>
        /// Where the ennemy health is in this try
        /// </summary>
        public int EnnemyStartHealth = 0;

        /// <summary>
        /// Is the player dogging?
        /// </summary>
        public bool PlayerDodged = false;

        /// <summary>
        /// Is the Ennemy dogging?
        /// </summary>
        public bool EnnemyDodged = false;

        /// <summary>
        /// Is the player touch the ennemy?
        /// </summary>
        public bool PlayerHitEnnemy = false;

        /// <summary>
        /// Is the ennemy hit the player?
        /// </summary>
        public bool EnnemyHitPlayer = false;

        /// <summary>
        /// Is the player die?
        /// </summary>
        public bool PlayerDied = false;

        /// <summary>
        /// Is the ennemy die?
        /// </summary>
        public bool EnnemyDied = false;

        /// <summary>
        /// Where the player finish this try
        /// </summary>
        public int PlayerEndPosition = 0;

        /// <summary>
        /// Where the Ennemy finish this try
        /// </summary>
        public int EnnemyEndPosition = 0;

        /// <summary>
        /// How many life the ennemy has loss?
        /// </summary>
        public int EnnemyEndHealth = 0;

        /// <summary>
        /// The distance between the both at the start of the try
        /// </summary>
        public int DistanceStart = 0;

        /// <summary>
        /// The distance between player and ennemy at the en fo try
        /// </summary>
        public int DistanceEnd = 0;

        /// <summary>
        /// Is a contact between player and Ennemy has been triggered?
        /// </summary>
        public bool ContactTriggered = false;

        /// <summary>
        /// The position has been swapped?
        /// </summary>
        public bool PositionSwapped = false;

        /// <summary>
        /// Is a contact victory for the player?
        /// </summary>
        public bool ContactVictoryForPlayer = false;

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
}