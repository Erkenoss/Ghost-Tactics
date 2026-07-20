using Crimson.Core;
using GhostTactics.Core.Combat;
using GhostTactics.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhostTactics.Core
{
    public class OnGhostUseAction
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// ABility we will use here
        /// </summary>
        public AbilityData Data = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public OnGhostUseAction(AbilityData data)
        {
            Data = data;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class OnEnnemyDie
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

    public class OnEnnemyIsHit
    {
        #region Public Fields

        public int DamageTaken { get { return damageTaken; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// How many damages the ennemy will lost
        /// </summary>
        private int damageTaken = 0;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dmgs"></param>
        public OnEnnemyIsHit(int dmgs)
        {
            damageTaken = dmgs;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class OnPlayerDie
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// List of the player Ability
        /// </summary>
        public List<AbilityData> PlayerList = new List<AbilityData>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public OnPlayerDie(List<AbilityData> abilities)
        {
            this.PlayerList = abilities;
        }

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

        /// <summary>
        /// Coroutine to manage the fight
        /// </summary>
        private Coroutine fightCoroutine = null;

        /// <summary>
        /// Use to know if the system wait for the ghost dodge
        /// </summary>
        private bool waitingForGhost = false;

        /// <summary>
        /// Current try of the fight
        /// </summary>
        private TryState currentTry = null;

        /// <summary>
        /// Current state of the fight
        /// </summary>
        private CombatState currentState = null;

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
        /// Resolution of the fight with the list of action of the player and the ennemy. Use as bridge to call the combat manager
        /// </summary>
        private void FightResolution(CombatResolutionEvent e)
        {
            if (fightCoroutine != null)
            {
                StopCoroutine(fightCoroutine);
            }

            fightCoroutine = StartCoroutine(FightResolutionCoroutine(e));
        }

        /// <summary>
        /// Use to manage the fight and control it in asynchrone to let the player use the ghost as he want
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private IEnumerator FightResolutionCoroutine(CombatResolutionEvent e)
        {
            if (e.PlayerList == null || e.PlayerList.Count == 0 || e.EnnemyList == null || e.EnnemyList.Count == 0 || e.PlayerList.Count != e.EnnemyList.Count)
            {
                yield break;
            }

            tryStates.Clear();
            currentState = new CombatState(-1, 1, e.Ennemy.EnnemyHealth, e.Player);

            for (int i = 0; i < e.PlayerList.Count; i++)
            {
                TryState step = CreateTryState(i, e.PlayerList[i], e.EnnemyList[i], currentState);
                currentTry = step;

                ApplyMovement(currentState, e.PlayerList[i], true, currentState.PositionSwapped);
                ApplyMovement(currentState, e.EnnemyList[i], false, currentState.PositionSwapped);

                if (currentState.Distance == 0)
                {
                    ResolveContact(currentState, currentTry);
                }

                if (!step.ContactTriggered)
                {
                    ResolveAttacks(currentState, currentTry, e.PlayerList[i], e.EnnemyList[i]);
                }


                if (waitingForGhost)
                {
                    Time.timeScale = 0.15f;

                    yield return new WaitUntil(() => !waitingForGhost);

                    Time.timeScale = 1f;
                }

                currentTry.PlayerEndPosition = currentState.PlayerPosition;
                currentTry.EnnemyEndPosition = currentState.EnnemyPosition;
                currentTry.EnnemyEndHealth = currentState.EnnemyHealth;
                currentTry.DistanceEnd = currentState.Distance;
                currentTry.PlayerDied = !currentState.PlayerAlive;
                currentTry.EnnemyDied = currentState.EnnemyHealth <= 0;

                tryStates.Add(currentTry);

                if (currentTry.PlayerDied || currentTry.EnnemyDied)
                {
                    if (currentTry.PlayerDied)
                    {
                        Debug.Log("==== PLAYER DIE ====");
                        EventBus.Publish<OnPlayerDie>(new OnPlayerDie(e.PlayerList));
                    }
                    else
                    {
                        Debug.Log("==== ENNEMY DIE ====");
                        EventBus.Publish<OnEnnemyDie>(new OnEnnemyDie());
                        EventBus.Publish<OnSavePlayer>(new OnSavePlayer());
                    }

                    break;
                }
            }

            EventBus.Publish<CombatResolveEvent>(new CombatResolveEvent(tryStates, currentState));
            EventBus.Publish<OnSavePlayer>(new OnSavePlayer());
            EventBus.Publish<ResetAll>(new ResetAll());
            fightCoroutine = null;
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

            newTry.PlayerDodged = pData.Ability == Abilities.Dodge;
            newTry.EnnemyDodged = eData.Ability == Abilities.Dodge;

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
            int playerDamage = 50;

            if (playerAttack && ennemyAttack)
            {
                /// Other feature
            }
            else if (playerDodging && ennemyAttack && state.Distance <= attackRange)
            {
                if (state.Player.PlayerGhost.ActionsGhost != null && state.Player.PlayerGhost.ActionsGhost.Count > 0)
                {
                    AbilityData ghostAttack = GetGhostAbility(state.Player, Abilities.Attack);

                    if (ghostAttack != null)
                    {
                        waitingForGhost = true;

                        EventBus.Publish<OnGhostAction>(new OnGhostAction(state.Player.PlayerGhost, ghostAttack));
                        EventBus.Publish<OnDisableButton>(new OnDisableButton());
                        return;
                    }
                }
            }
            else if (playerAttack && !ennemyDodging && state.Distance <= attackRange)
            {
                state.EnnemyHealth -= playerDamage;
                {
                    tryState.PlayerHitEnnemy = true;
                    EventBus.Publish<OnEnnemyIsHit>(new OnEnnemyIsHit(playerDamage));
                }
            }
            else if (ennemyAttack && !playerDodging && state.Distance <= attackRange)
            {
                if (state.Player.PlayerGhost.ActionsGhost != null && state.Player.PlayerGhost.ActionsGhost.Count > 0)
                {
                    AbilityData ghostDodge = GetGhostAbility(state.Player, Abilities.Backward);

                    if (ghostDodge == null)
                    {
                        ghostDodge = GetGhostAbility(state.Player, Abilities.Dodge);
                    }
                   
                    if (ghostDodge != null)
                    {
                        waitingForGhost = true;

                        EventBus.Publish<OnGhostAction>(new OnGhostAction(state.Player.PlayerGhost, ghostDodge));
                        EventBus.Publish<OnDisableButton>(new OnDisableButton());
                        return;
                    }
                }

                state.PlayerAlive = false;
                tryState.EnnemyHitPlayer = true;
            }
        }

        /// <summary>
        /// Return an AbilityData in the ghost base on ability
        /// </summary>
        /// <param name="g"></param>
        /// <param name="ability"></param>
        /// <returns></returns>
        private AbilityData GetGhostAbility(Player g, Abilities ability)
        {
            if (g  == null || ability == Abilities.none || ability == Abilities.Idle)
            {
                return null;
            }

            AbilityData temp = g.PlayerGhost.ActionsGhost.Find(a => a.Ability == ability);

            if (temp == null)
            {
                return null;
            }

            return temp;
        }

        /// <summary>
        /// When the player use the ghost in fight
        /// </summary>
        /// <param name="g"></param>
        private void GhostUseAction(OnGhostUseAction g)
        {
            if (!waitingForGhost || g == null || g.Data == null)
            {
                return;
            }

            ResolveGhostAction(g.Data);

            waitingForGhost = false;

            Debug.Log($"=== GHOST USED {g.Data.Ability} ===");
        }
        
        /// <summary>
        /// Resolve the differents actions use by the Ghost
        /// </summary>
        /// <param name="ghostAbility"></param>
        private void ResolveGhostAction(AbilityData ghostAbility)
        {
            switch (ghostAbility.Ability)
            {
                case Abilities.Dodge:
                    ResolveGhostDodge();
                    break;

                case Abilities.Attack:
                    ResolveGhostAttack(ghostAbility);
                    break;

                case Abilities.Forward:
                    ResolveGhostMovement(ghostAbility);
                    break;

                case Abilities.Backward:
                    ResolveGhostMovement(ghostAbility);
                    break;
            }
        }

        /// <summary>
        /// Resolve the dodge of the ghost
        /// </summary>
        private void ResolveGhostDodge()
        {
            currentTry.PlayerDodged = true;
            currentTry.EnnemyHitPlayer = false;
        }

        /// <summary>
        /// Resolve the attack of the ghost
        /// </summary>
        /// <param name="ability"></param>
        private void ResolveGhostAttack(AbilityData ability)
        {
            int damage = 50;

            currentState.EnnemyHealth -= damage;
            currentTry.PlayerHitEnnemy = true;

            EventBus.Publish<OnEnnemyIsHit>(new OnEnnemyIsHit(damage));
        }

        /// <summary>
        /// Resolve the movement of the ghost
        /// </summary>
        /// <param name="ability"></param>
        private void ResolveGhostMovement(AbilityData ability)
        {
            ApplyMovement(currentState, ability, true, currentState.PositionSwapped);

            if (currentState.Distance == 0 && !currentTry.ContactTriggered)
            {
                ResolveContact(currentState, currentTry);
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

        /// <summary>
        /// Subscribe to the EventBus the differents listeners
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<CombatResolutionEvent>(FightResolution);
            EventBus.Subscribe<CombatResolveEvent>(FightResolve);
            EventBus.Subscribe<OnGhostUseAction>(GhostUseAction);
        }

        /// <summary>
        /// Unsubscribe to the EventBus the differents listeners
        /// </summary>
        private void UnSubscribe()
        {
            EventBus.Unsubscribe<CombatResolutionEvent>(FightResolution);
            EventBus.Unsubscribe<CombatResolveEvent>(FightResolve);
            EventBus.Unsubscribe<OnGhostUseAction>(GhostUseAction);
        }

        #endregion
    }
}