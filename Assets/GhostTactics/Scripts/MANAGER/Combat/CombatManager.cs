using Crimson.Core;
using GhostTactics.Core.Combat;
using GhostTactics.Data;
using GhostTactics.Ennemi;
using System.Collections.Generic;
using UnityEngine;

namespace GhostTactics.Core
{
    public class OnPlayerDie
    {
        #region Public Fields
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

    public class CombatResolveEvent
    {
        #region Public Fields

        public List<TryState> CurrentStepsList { get { return currentStepsList; } }
        public CombatState CurrentState { get { return currentState; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// List of the different steps in a fight
        /// </summary>
        List<TryState> currentStepsList = new List<TryState>();

        /// <summary>
        /// Current state of the fight
        /// </summary>
        private CombatState currentState = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public CombatResolveEvent(List<TryState> steps, CombatState state)
        {
            currentStepsList = steps;
            currentState = state;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class CombatManager : Singleton<CombatManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// List of the current tries in a fight
        /// </summary>
        private List<TryState> tryStates = new List<TryState>();

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
        #endregion

        #region Private Methods

        /// <summary>
        /// Subscribe to the EventBus the differents listeners
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<CombatResolutionEvent>(FightResolution);
            EventBus.Subscribe<CombatResolveEvent>(FightResolve);
        }

        /// <summary>
        /// Unsubscribe to the EventBus the differents listeners
        /// </summary>
        private void UnSubscribe()
        {
            EventBus.Unsubscribe<CombatResolutionEvent>(FightResolution);
            EventBus.Unsubscribe<CombatResolveEvent>(FightResolve);
        }

        /// <summary>
        /// Resolution of the fight with the list of action of the player and the ennemy. Use as bridge to call the combat manager
        /// </summary>
        private void FightResolution(CombatResolutionEvent e)
        {
            if (e.PlayerList == null || e.PlayerList.Count == 0 || e.EnnemyList == null || e.EnnemyList.Count == 0 || e.PlayerList.Count != e.EnnemyList.Count)
            {
                return;
            }

            tryStates.Clear();
            tryStates.TrimExcess();
            CombatState state = new CombatState(-1, 1, 100);

            for (int i = 0; i < e.PlayerList.Count; i++)
            {
                TryState step = CreateTryState(i, e.PlayerList[i], e.EnnemyList[i], state);
                tryStates.Add(step);

                if (step.PlayerDied || step.EnnemyDied)
                {
                    if (step.PlayerDied)
                    {
                        Debug.Log("==== PLAYER DIE ====");
                        EventBus.Publish<OnPlayerDie>(new OnPlayerDie());
                    }
                    else
                    {
                        Debug.Log("==== ENNEMY DIE ====");
                    }

                    break;
                }
            }

            EventBus.Publish<CombatResolveEvent>(new CombatResolveEvent(tryStates, state));
        }

        /// <summary>
        /// Resolve the fight base on the current CombatState of the CombatResolveEvent class
        /// </summary>
        /// <param name="e"></param>
        private void FightResolve(CombatResolveEvent e)
        {
            if (e == null || e.CurrentStepsList == null || e.CurrentStepsList.Count == 0 || e.CurrentState == null)
            {
                return;
            }

            DebugCombatResult(e.CurrentStepsList, e.CurrentState);
        }

        /// <summary>
        /// Create a new TryState to keep an historique of the fight
        /// </summary>
        private TryState CreateTryState(int step, AbilityData pData, AbilityData eData, CombatState state)
        {
            TryState newTry = new TryState();

            newTry.StepIndex = step;
            newTry.PlayerAbility = pData.Ability;
            newTry.EnnemyAbility = eData.Ability;

            newTry.PlayerStartPosition = state.PlayerPosition;
            newTry.EnnemyStartPosition = state.EnnemyPosition;
            newTry.EnnemyStartHealth = state.EnnemyHealth;
            newTry.DistanceStart = state.Distance;

            bool playerDodge = pData.Ability == Abilities.Dodge;
            bool ennemyDodge = eData.Ability == Abilities.Dodge;

            newTry.PlayerDodged = playerDodge;
            newTry.EnnemyDodged = ennemyDodge;

            ApplyMovement(state, pData, true, state.PositionSwapped);
            ApplyMovement(state, eData, false, state.PositionSwapped);

            if (state.Distance == 0)
            {
                ResolveContact(state, newTry);
            }

            if (!newTry.ContactTriggered)
            {
                ResolveAttacks(state, newTry, pData, eData);
            }

            newTry.PlayerEndPosition = state.PlayerPosition; 
            newTry.EnnemyEndPosition = state.EnnemyPosition; 
            newTry.EnnemyEndHealth = state.EnnemyHealth; 
            newTry.DistanceEnd = state.Distance; 
            newTry.PlayerDied = !state.PlayerAlive; 
            newTry.EnnemyDied = state.EnnemyHealth <= 0;

            return newTry;
        }

        /// <summary>
        /// Use to Apply the Movement of the different opponent
        /// </summary>
        /// <param name="state"></param>
        /// <param name="pData"></param>
        /// <param name="EData"></param>
        /// <param name="isPlayer"></param>
        private void ApplyMovement(CombatState state, AbilityData ability, bool isPlayer, bool isSwapped)
        {
            int direction = isPlayer ? 1 : -1;

            if (isSwapped)
            {
                direction *= -1;
            }

            if (ability.Ability == Abilities.Forward)
            {
                if (isPlayer)
                {
                    state.PlayerPosition += direction;
                }
                else
                {
                    state.EnnemyPosition += direction;
                }
            }
            else if (ability.Ability == Abilities.Backward)
            {
                if (isPlayer)
                {
                    state.PlayerPosition -= direction;
                }
                else
                {
                    state.EnnemyPosition -= direction;
                }
            }
        }

        /// <summary>
        /// Resolve contact after a movement.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="tryState"></param>
        private void ResolveContact(CombatState state, TryState tryState)
        {
            tryState.ContactTriggered = true;
            tryState.ContactVictoryForPlayer = true;

            state.EnnemyHealth -= 1;
            tryState.PlayerHitEnnemy = true;

            int playerPos = state.PlayerPosition;
            state.PlayerPosition = state.EnnemyPosition;
            state.EnnemyPosition = playerPos;

            state.PositionSwapped = !state.PositionSwapped;
            tryState.PositionSwapped = state.PositionSwapped;
        }

        /// <summary>
        /// Resolve the attack betwween player and ennemy
        /// </summary>
        /// <param name="state"></param>
        /// <param name="tryState"></param>
        /// <param name="pData"></param>
        /// <param name="eData"></param>
        private void ResolveAttacks(CombatState state, TryState tryState, AbilityData pData, AbilityData eData)
        {
            bool playerAttack = pData.Ability == Abilities.Attack;
            bool ennemyAttack = eData.Ability == Abilities.Attack;

            bool playerDodging = pData.Ability == Abilities.Dodge;
            bool ennemyDodging = eData.Ability == Abilities.Dodge;

            int attackRange = 1;
            int playerDamage = 1;

            if (playerAttack && !ennemyDodging && state.Distance <= attackRange)
            {
                state.EnnemyHealth -= playerDamage;
                tryState.PlayerHitEnnemy = true;
            }

            if (ennemyAttack && !playerDodging && state.Distance <= attackRange)
            {
                state.PlayerAlive = false;
                tryState.EnnemyHitPlayer = true;
            }
        }

        /// <summary>
        /// Debug a fight
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="finalState"></param>
        private void DebugCombatResult(List<TryState> steps, CombatState finalState)
        {
            Debug.Log("===== COMBAT START =====");

            foreach (var step in steps)
            {
                string log =
                    $"[Step {step.StepIndex}] " +
                    $"P:{step.PlayerAbility} vs E:{step.EnnemyAbility} | " +
                    $"Pos {step.PlayerStartPosition}->{step.PlayerEndPosition} / {step.EnnemyStartPosition}->{step.EnnemyEndPosition} | " +
                    $"Dist {step.DistanceStart}->{step.DistanceEnd} | " +
                    $"HP {step.EnnemyStartHealth}->{step.EnnemyEndHealth}";

                if (step.PlayerDodged)
                    log += " | Player DODGE";

                if (step.EnnemyDodged)
                    log += " | Enemy DODGE";

                if (step.PlayerHitEnnemy)
                    log += " | Player HIT";

                if (step.EnnemyHitPlayer)
                    log += " | Enemy HIT";

                if (step.ContactTriggered)
                    log += " | CONTACT";

                if (step.PositionSwapped)
                    log += " | SWAP";

                if (step.PlayerDied)
                    log += " | PLAYER DEAD";

                if (step.EnnemyDied)
                    log += " | ENEMY DEAD";

                Debug.Log(log);
            }

            Debug.Log($"===== COMBAT END ===== | Final HP: {finalState.EnnemyHealth} | PlayerAlive: {finalState.PlayerAlive}");
        }

        #endregion
    }
}