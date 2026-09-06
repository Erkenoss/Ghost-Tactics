using Crimson.Core;
using Crimson.Core.Audio;
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

    public class CombatManager : Singleton<CombatManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("How many time waiting for Idle if no animation are played except Idle itself")]
        [SerializeField]
        private float idleStepDuration = 0.4f;

        [Tooltip("How many time to fade the screen")]
        [SerializeField]
        private float fadeDuration = 0.4f;

        /// <summary>
        /// Controller to manage the fight like movement on the target
        /// </summary>
        private CombatController controller = null;

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

        /// <summary>
        /// Container of animation with character as key
        /// </summary>
        private readonly Dictionary<ECharacterSide, ECharacterAnimation> pendingAnimations = new Dictionary<ECharacterSide, ECharacterAnimation>();

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
        /// Register the controller at the start of the game scene
        /// </summary>
        /// <param name="control"></param>
        public void RegisterController(CombatController control)
        {
            if (control == null)
            {
                return;
            }

            controller = control;
        }

        /// <summary>
        /// When a fight start
        /// </summary>
        public void StartFight()
        {
            if (controller == null)
            {
                return;
            }

            PlayAndTrackAnimation(ECharacterAnimation.Respawn, ECharacterSide.Player);
        }

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
            if (e == null || e.Player == null || e.Ennemy == null || e.PlayerList == null || e.PlayerList.Count == 0 || e.EnnemyList == null || e.EnnemyList.Count == 0 || e.PlayerList.Count != e.EnnemyList.Count)
            {
                fightCoroutine = null;
                yield break;
            }

            tryStates.Clear();

            if (currentState == null)
            {
                currentState = new CombatState(-1, 1, e.Ennemy.EnnemyHealth, e.Player);
            }

            for (int i = 0; i < e.PlayerList.Count; i++)
            {
                AbilityData playerAbility = e.PlayerList[i];
                AbilityData enemyAbility = e.EnnemyList[i];

                TryState step = CreateTryState(i, playerAbility, enemyAbility, currentState);
                currentTry = step;

                yield return ResolveCombatStep(currentState, step, playerAbility, enemyAbility);

                step.PlayerEndPosition = currentState.PlayerPosition;
                step.EnnemyEndPosition = currentState.EnnemyPosition;
                step.EnnemyEndHealth = currentState.EnnemyHealth;
                step.DistanceEnd = currentState.Distance;
                step.PlayerDied = !currentState.PlayerAlive;
                step.EnnemyDied = currentState.EnnemyHealth <= 0;

                tryStates.Add(step);

                if (!step.PlayerDied && !step.EnnemyDied)
                {
                    continue;
                }

                yield return EndFight(step.PlayerDied, step.EnnemyDied, e.PlayerList);
                break;
            }

            EventBus.Publish(new OnSavePlayer());
            EventBus.Publish(new ResetAll());

            currentTry = null;
            fightCoroutine = null;
        }

        /// <summary>
        /// Calculates a target position without modifying the CombatState.
        /// </summary>
        private int CalculateMovementTarget(int currentPosition, AbilityData ability, bool isPlayer, bool isSwapped)
        {
            if (ability == null)
            {
                return currentPosition;
            }

            int direction = isPlayer ? 1 : -1;
            int targetPosition = currentPosition;

            if (ability.Ability == Abilities.Forward)
            {
                targetPosition += direction;
            }
            else if (ability.Ability == Abilities.Backward)
            {
                targetPosition -= direction;
            }

            return Mathf.Clamp(targetPosition, -2, 2);
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

            AbilityData ghostDodge = null;

            if (state.Player.PlayerGhost.ActionsGhost != null && state.Player.PlayerGhost.ActionsGhost.Count > 0)
            {
                ghostDodge = GetGhostAbility(state.Player, Abilities.Dodge);
            }

            if (playerAttack && ennemyAttack)
            {
                if (ghostDodge != null)
                {
                    state.EnnemyHealth -= playerDamage;
                    tryState.PlayerHitEnnemy = true;

                    EventBus.Publish<OnEnnemyIsHit>(new OnEnnemyIsHit(playerDamage));

                    waitingForGhost = true;

                    EventBus.Publish<OnGhostAction>(new OnGhostAction(state.Player.PlayerGhost, ghostDodge));
                    EventBus.Publish<OnDisableButton>(new OnDisableButton(null));

                    return;
                }
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
                        EventBus.Publish<OnDisableButton>(new OnDisableButton(null));
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
                //GetGhostAbility(state.Player, Abilities.Backward);
                if (ghostDodge != null)
                {
                    waitingForGhost = true;

                    EventBus.Publish<OnGhostAction>(new OnGhostAction(state.Player.PlayerGhost, ghostDodge));
                    EventBus.Publish<OnDisableButton>(new OnDisableButton(null));
                    return;
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
        /// currentState become null when new level
        /// </summary>
        /// <param name="lvl"></param>
        private void NextLevel(NextLevel lvl)
        {
            currentState = null;
        }

        /// <summary>
        /// Reset the current CombatState
        /// </summary>
        private void ResetCurrentState()
        {
            currentState = null;
        }

        /// <summary>
        /// Resolve the dodge of the ghost
        /// </summary>
        private void ResolveGhostDodge()
        {
            EventBus.Publish<OnGhostAnimationPlay>(new OnGhostAnimationPlay(EGhostAnimation.Interupt));

            currentTry.PlayerDodged = true;
            currentTry.EnnemyHitPlayer = false;
            currentState.PlayerAlive = true;
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
        /// When an animation ended
        /// </summary>
        /// <param name="end"></param>
        private void AnimationEnded(OnAnimationEnded end)
        {
            if (!pendingAnimations.TryGetValue(end.Side, out ECharacterAnimation expectedAnimation))
            {
                return;
            }

            if (end.Animation != expectedAnimation)
            {
                return;
            }

            pendingAnimations.Remove(end.Side);
        }

        /// <summary>
        /// When the ghost animation ended
        /// </summary>
        /// <param name="end"></param>
        private void GhostAnimationEnded(OnGhostAnimationEnded end)
        {

        }

        /// <summary>
        /// Play character animation
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="side"></param>
        private void PlayAndTrackAnimation(ECharacterAnimation animation, ECharacterSide side)
        {
            if (animation == ECharacterAnimation.None)
            {
                return;
            }

            if (animation != ECharacterAnimation.Idle)
            {
                pendingAnimations[side] = animation;
            }

            EventBus.Publish(new OnAnimationPlay(animation, side));
        }

        /// <summary>
        /// Plays and resolves both character actions simultaneously.
        /// </summary>
        private IEnumerator ResolveCombatStep(CombatState state, TryState step, AbilityData playerAbility, AbilityData enemyAbility)
        {
            if (state == null || step == null || controller == null)
            {
                yield break;
            }

            int playerStartPosition = state.PlayerPosition;
            int enemyStartPosition = state.EnnemyPosition;

            int playerTargetPosition = CalculateMovementTarget(playerStartPosition, playerAbility, true, false);
            int enemyTargetPosition = CalculateMovementTarget(enemyStartPosition, enemyAbility, false, false);

            bool playerRequestedMovement = playerTargetPosition != playerStartPosition;
            bool enemyRequestedMovement = enemyTargetPosition != enemyStartPosition;

            bool playerBlocked = playerRequestedMovement && playerTargetPosition == enemyStartPosition && enemyAbility.Ability != Abilities.Backward;
            bool enemyBlocked = enemyRequestedMovement && enemyTargetPosition == playerStartPosition && playerAbility.Ability != Abilities.Backward;
            bool sameTarget = playerRequestedMovement && enemyRequestedMovement && playerTargetPosition == enemyTargetPosition;

            if (sameTarget)
            {
                playerBlocked = true;
                enemyBlocked = true;
            }

            if (playerBlocked)
            {
                playerTargetPosition = playerStartPosition;
            }

            if (enemyBlocked)
            {
                enemyTargetPosition = enemyStartPosition;
            }

            bool playerMoves = playerTargetPosition != playerStartPosition;
            bool enemyMoves = enemyTargetPosition != enemyStartPosition;

            ECharacterAnimation playerAnimation = GetStepAnimation(playerAbility, playerMoves);
            ECharacterAnimation enemyAnimation = GetStepAnimation(enemyAbility, enemyMoves);

            pendingAnimations.Clear();

            PlayAndTrackAnimation(playerAnimation, ECharacterSide.Player);
            PlayAndTrackAnimation(enemyAnimation, ECharacterSide.Enemy);

            if (playerMoves)
            {
                controller.MoveCharacter(ECharacterSide.Player, playerTargetPosition);
            }

            if (enemyMoves)
            {
                controller.MoveCharacter(ECharacterSide.Enemy, enemyTargetPosition);
            }

            state.PlayerPosition = playerTargetPosition;
            state.EnnemyPosition = enemyTargetPosition;

            if (!step.ContactTriggered)
            {
                ResolveAttacks(state, step, playerAbility, enemyAbility);
            }

            if (waitingForGhost)
            {
                Time.timeScale = 0.15f;
                yield return new WaitUntil(() => !waitingForGhost);
                Time.timeScale = 1f;
            }

            if (pendingAnimations.Count > 0 || controller.IsAnyCharacterMoving)
            {
                yield return new WaitUntil(() => pendingAnimations.Count == 0 && !controller.IsAnyCharacterMoving);
            }
            else
            {
                yield return new WaitForSeconds(idleStepDuration);
            }

            if (playerAnimation != ECharacterAnimation.Idle && playerAnimation != ECharacterAnimation.None)
            {
                EventBus.Publish(new OnAnimationPlay(ECharacterAnimation.Idle, ECharacterSide.Player));
            }

            if (enemyAnimation != ECharacterAnimation.Idle && enemyAnimation != ECharacterAnimation.None)
            {
                EventBus.Publish(new OnAnimationPlay(ECharacterAnimation.Idle, ECharacterSide.Enemy));
            }
        }

        /// <summary>
        /// Returns the animation associated with the resolved ability.
        /// </summary>
        private ECharacterAnimation GetStepAnimation(AbilityData ability, bool movementAccepted)
        {
            if (ability == null)
            {
                return ECharacterAnimation.Idle;
            }

            switch (ability.Ability)
            {
                case Abilities.Forward: 
                    return movementAccepted ? ECharacterAnimation.Dash : ECharacterAnimation.Idle;

                case Abilities.Backward:
                    return movementAccepted ? ECharacterAnimation.BackDash : ECharacterAnimation.Idle;

                case Abilities.Attack:
                    return ECharacterAnimation.Attack;

                case Abilities.Dodge:
                    return ECharacterAnimation.Dodge;

                case Abilities.Idle:
                case Abilities.none:
                default:
                    return ECharacterAnimation.Idle;
            }
        }

        /// <summary>
        /// Coroutine to manage the end of a fight.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EndFight(bool isPlayerDie, bool isEnemyDie, List<AbilityData> playerList)
        {
            pendingAnimations.Clear();

            if (isPlayerDie)
            {
                PlayAndTrackAnimation(ECharacterAnimation.Death, ECharacterSide.Player);
                EventBus.Publish<OnPlayerSoundEvent>(new OnPlayerSoundEvent(EPlayerContext.Death, EAudio.Voice));
            }
            else if (isEnemyDie)
            {
                PlayAndTrackAnimation(ECharacterAnimation.Death, ECharacterSide.Enemy);
            }

            yield return new WaitUntil(() => pendingAnimations.Count == 0);

            UIManager uiManager = UIManager.Instance;

            if (uiManager != null)
            {
                yield return uiManager.FadeToBlack(fadeDuration);
            }

            currentState = null;

            if (isPlayerDie)
            {
                EventBus.Publish(new OnPlayerDie(playerList));
                ResetCurrentState();
            }
            else if (isEnemyDie)
            {
                EventBus.Publish(new OnEnnemyDie());
                ResetCurrentState();
            }

            if (uiManager != null)
            {
                yield return uiManager.FadeFromBlack(fadeDuration);
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
            EventBus.Subscribe<OnGhostUseAction>(GhostUseAction);
            EventBus.Subscribe<NextLevel>(NextLevel);
            EventBus.Subscribe<OnAnimationEnded>(AnimationEnded);
            EventBus.Subscribe<OnGhostAnimationEnded>(GhostAnimationEnded);
        }

        /// <summary>
        /// Unsubscribe to the EventBus the differents listeners
        /// </summary>
        private void UnSubscribe()
        {
            EventBus.Unsubscribe<CombatResolutionEvent>(FightResolution);
            EventBus.Unsubscribe<OnGhostUseAction>(GhostUseAction);
            EventBus.Unsubscribe<NextLevel>(NextLevel);
            EventBus.Unsubscribe<OnAnimationEnded>(AnimationEnded);
            EventBus.Unsubscribe<OnGhostAnimationEnded>(GhostAnimationEnded);
        }

        #endregion
    }
}