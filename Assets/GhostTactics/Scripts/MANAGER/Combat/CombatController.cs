using Crimson.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GhostTactics.Core.Combat
{
    [Serializable]
    public class TargetStructure
    {
        [Tooltip("Target position in the fight system. -2 full left. 2 full right")]
        [SerializeField]
        private int index = 0;

        [Tooltip("GameObject targeted by the index")]
        [SerializeField]
        private RectTransform targetAssociated = null;

        public int Index => index;
        public RectTransform TargetAssociated => targetAssociated;
    }

    public class CombatController : MonoBehaviour
    {
        #region Private Fields

        [Tooltip("List of all targets we have in the fight area")]
        [SerializeField]
        private List<TargetStructure> targetList = new();

        [Tooltip("Player object")]
        [SerializeField]
        private RectTransform playerObject = null;

        [Tooltip("Enemy object")]
        [SerializeField]
        private RectTransform ennemyObject = null;

        [Tooltip("Duration of the movement based on the dash character speed")]
        [SerializeField]
        private float movementDuration = 0.2f;

        /// <summary>
        /// Fast access to targets by logical position
        /// </summary>
        private readonly Dictionary<int, RectTransform> targetMap = new();

        /// <summary>
        /// Coroutine currently used by player movement
        /// </summary>
        private Coroutine playerMovementCoroutine = null;

        /// <summary>
        /// Coroutine currently used by enemy movement
        /// </summary>
        private Coroutine enemyMovementCoroutine = null;

        /// <summary>
        /// Is player currently moving
        /// </summary>
        private bool playerMoving = false;

        /// <summary>
        /// Is enemy currently moving
        /// </summary>
        private bool enemyMoving = false;

        #endregion

        #region Public Properties

        public bool IsAnyCharacterMoving => playerMoving || enemyMoving;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            BuildTargetMap();
            Subscribe();
        }

        private void Start()
        {
            if (CombatManager.Instance == null)
            {
                return;
            }

            CombatManager.Instance.RegisterController(this);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildTargetMap();
        }
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Move one character to a logical target index
        /// </summary>
        public void MoveCharacter(ECharacterSide side, int index)
        {
            if (index < -2 || index > 2)
            {
                return;
            }

            RectTransform objectToMove = side == ECharacterSide.Player ? playerObject : ennemyObject;

            if (objectToMove == null)
            {
                return;
            }

            TargetStructure structure = targetList.Find(target => target != null && target.Index == index);

            if (structure == null)
            {
                return;
            }

            if (structure.TargetAssociated == null)
            {
                return;
            }

            StartMovement(side, objectToMove, structure.TargetAssociated, movementDuration);
        }

        /// <summary>
        /// Move player and enemy simultaneously, then wait until both movements are finished
        /// </summary>
        public IEnumerator MoveCharacters(int playerIndex, int ennemyIndex)
        {
            if (TryGetTarget(playerIndex, out RectTransform playerTarget) && playerObject != null)
            {
                StartMovement(ECharacterSide.Player, playerObject, playerTarget, movementDuration);
            }

            if (TryGetTarget(ennemyIndex, out RectTransform ennemyTarget) && ennemyObject != null)
            {
                StartMovement(ECharacterSide.Enemy, ennemyObject, ennemyTarget, movementDuration);
            }

            yield return new WaitUntil(() => !IsAnyCharacterMoving);
        }

        /// <summary>
        /// Returns true if the given side is currently moving
        /// </summary>
        public bool IsMoving(ECharacterSide side)
        {
            return side == ECharacterSide.Player ? playerMoving : enemyMoving;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Build the target dictionary
        /// </summary>
        private void BuildTargetMap()
        {
            targetMap.Clear();

            for (int i = 0; i < targetList.Count; i++)
            {
                TargetStructure structure = targetList[i];

                if (structure == null || structure.TargetAssociated == null)
                {
                    continue;
                }

                targetMap[structure.Index] = structure.TargetAssociated;
            }
        }

        /// <summary>
        /// Try to get a target by index
        /// </summary>
        private bool TryGetTarget(int index, out RectTransform target)
        {
            target = null;

            if (index < -2 || index > 2)
            {
                return false;
            }

            if (targetMap.Count == 0)
            {
                BuildTargetMap();
            }

            return targetMap.TryGetValue(index, out target) && target != null;
        }

        /// <summary>
        /// Start a movement for one specific side
        /// </summary>
        private void StartMovement(ECharacterSide side, RectTransform characterSlot, RectTransform target, float duration)
        {
            if (side == ECharacterSide.Player)
            {
                if (playerMovementCoroutine != null)
                {
                    StopCoroutine(playerMovementCoroutine);
                }

                playerMovementCoroutine = StartCoroutine(MoveCharacterToTargetX(side, characterSlot, target, duration));
            }
            else
            {
                if (enemyMovementCoroutine != null)
                {
                    StopCoroutine(enemyMovementCoroutine);
                }

                enemyMovementCoroutine = StartCoroutine(MoveCharacterToTargetX(side, characterSlot, target, duration));
            }
        }

        /// <summary>
        /// Moves a UI character toward the X coordinate of a target.
        /// The current Y and Z coordinates are preserved.
        /// </summary>
        private IEnumerator MoveCharacterToTargetX(ECharacterSide side, RectTransform characterSlot, RectTransform target, float duration)
        {
            SetMovingState(side, true);

            Vector3 startPosition = characterSlot.position;
            Vector3 targetPosition = startPosition;
            targetPosition.x = target.position.x;

            if (duration <= 0f)
            {
                characterSlot.position = targetPosition;
                ClearMovementReference(side);
                SetMovingState(side, false);
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float progression = Mathf.Clamp01(elapsedTime / duration);
                progression = Mathf.SmoothStep(0f, 1f, progression);

                characterSlot.position = Vector3.Lerp(startPosition, targetPosition, progression);

                yield return null;
            }

            characterSlot.position = targetPosition;

            ClearMovementReference(side);
            SetMovingState(side, false);
        }

        /// <summary>
        /// Set moving state for one side
        /// </summary>
        private void SetMovingState(ECharacterSide side, bool isMoving)
        {
            if (side == ECharacterSide.Player)
            {
                playerMoving = isMoving;
            }
            else
            {
                enemyMoving = isMoving;
            }
        }

        /// <summary>
        /// Clear coroutine reference for one side
        /// </summary>
        private void ClearMovementReference(ECharacterSide side)
        {
            if (side == ECharacterSide.Player)
            {
                playerMovementCoroutine = null;
            }
            else
            {
                enemyMovementCoroutine = null;
            }
        }

        /// <summary>
        /// Reset the position of the character
        /// </summary>
        /// <param name="die"></param>
        private void ResetCharacterPosition(OnPlayerDie die)
        {
            if (playerObject == null || ennemyObject == null)
            {
                return;
            }

            MoveCharacter(ECharacterSide.Player, -1);
            MoveCharacter(ECharacterSide.Enemy, 1);
        }

        /// <summary>
        /// Reset the position of the character
        /// </summary>
        /// <param name="die"></param>
        private void ResetCharacterPosition(OnEnnemyDie die)
        {
            if (playerObject == null || ennemyObject == null)
            {
                return;
            }

            MoveCharacter(ECharacterSide.Player, -1);
            MoveCharacter(ECharacterSide.Enemy, 1);
        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnPlayerDie>(ResetCharacterPosition);
            EventBus.Subscribe<OnEnnemyDie>(ResetCharacterPosition);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnPlayerDie>(ResetCharacterPosition);
            EventBus.Unsubscribe<OnEnnemyDie>(ResetCharacterPosition);
        }

        #endregion
    }
}