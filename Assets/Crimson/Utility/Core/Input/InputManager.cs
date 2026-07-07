using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Crimson.Core.Input
{
    /// <summary>
    /// Each type of map
    /// </summary>
    public enum ETypeMap
    {
        none = 0,
        UI,
        Player,
        Fight
    }

    /// <summary>
    /// Each type of inputs
    /// </summary>
    public enum EInputType
    {
        none = 0,
        Interact,
        Move,
        Look,
        Jump,
        Inventory,
        Skill,
        Zoom,
        OnLeftClick,
        OnRightClick,
        OnMouseMove,
        HoldRotation,
        DoubleClick,
    }

    public class InputManager : Singleton<InputManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// PlayerInput of the game
        /// </summary>
        [Tooltip("Player Input of the game")]
        [SerializeField]
        private PlayerInput playerInput = null;

        /// <summary>
        /// HasSet of all active map
        /// </summary>
        private HashSet<InputActionMap> activeMaps = new HashSet<InputActionMap>();

        /// <summary>
        /// Where all action are contains
        /// </summary>
        private Dictionary<(ETypeMap, EInputType, InputActionPhase), Action<InputAction.CallbackContext>> callback = new Dictionary<(ETypeMap, EInputType, InputActionPhase), Action<InputAction.CallbackContext>>();

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            Sub();
        }

        private void OnDisable()
        {
            UnSub();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Return the PlayerInput component
        /// </summary>
        /// <returns></returns>
        public PlayerInput GetPlayerInput()
        {
            if (playerInput == null)
            {
                return null;
            }

            return playerInput;
        }

        /// <summary>
        /// Enable a map of Input
        /// </summary>
        /// <param name="map"></param>
        public void EnableMap(ETypeMap eMap)
        {
            if (eMap == ETypeMap.none || playerInput == null)
            {
                return;
            }

            InputActionMap map = playerInput.actions.FindActionMap(eMap.ToString(), throwIfNotFound: false);

            if (map == null)
            {
                return;
            }

            if (!activeMaps.Contains(map))
            {
                activeMaps.Add(map);
                map.Enable();
            }
        }

        /// <summary>
        /// Disable a map of Input
        /// </summary>
        /// <param name="map"></param>
        public void DisableMap(ETypeMap eMap)
        {
            if (eMap == ETypeMap.none || playerInput == null)
            {
                return;
            }

            InputActionMap map = playerInput.actions.FindActionMap(eMap.ToString(), throwIfNotFound: false);

            if (map == null)
            {
                return;
            }

            if (activeMaps.Contains(map))
            {
                map.Disable();
                activeMaps.Remove(map);
            }
        }

        /// <summary>
        /// Use to invoke the different inputs
        /// </summary>
        /// <param name="context"></param>
        public void TreatInput(InputAction.CallbackContext context)
        {
            if (!activeMaps.Contains(context.action.actionMap))
            {
                return;
            }

            ETypeMap map = (ETypeMap)Enum.Parse(typeof(ETypeMap), context.action.actionMap.name);
            EInputType type = (EInputType)Enum.Parse(typeof(EInputType), context.action.name);

            if (callback.TryGetValue((map, type, context.phase), out Action<InputAction.CallbackContext> act))
            {
                act?.Invoke(context);
            }
        }

        /// <summary>
        /// Add input in the dictionary
        /// </summary>
        public void AddInput(ETypeMap map, EInputType type, InputActionPhase phase, Action<InputAction.CallbackContext> toPerformed)
        {
            if (map == ETypeMap.none || type == EInputType.none || toPerformed == null)
            {
                return;
            }

            callback[(map, type, phase)] = toPerformed;
        }

        /// <summary>
        /// Remove an abonment of an inputt
        /// </summary>
        /// <param name="map"></param>
        /// <param name="type"></param>
        /// <param name="phase"></param>
        /// <param name="toPerformed"></param>
        public void RemoveInput(ETypeMap map, EInputType type, InputActionPhase phase)
        {
            if (map == ETypeMap.none || type == EInputType.none)
            {
                return;
            }

            if (callback.TryGetValue((map, type, phase), out Action<InputAction.CallbackContext> act))
            {
                callback.Remove((map, type, phase));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Init all control in the given map
        /// </summary>
        private void SubscribeControls(InputActionMap map)
        {
            if (map == null)
            {
                return;
            }

            foreach (InputAction action in map.actions)
            {
                action.started += TreatInput;
                action.performed += TreatInput;
                action.canceled += TreatInput;
            }
        }

        /// <summary>
        /// Disable all control in the given map
        /// </summary>
        private void UnsubscribeControls(InputActionMap map)
        {
            if (map == null)
            {
                return;
            }

            foreach (InputAction action in map.actions)
            {
                action.started -= TreatInput;
                action.performed -= TreatInput;
                action.canceled -= TreatInput;
            }
        }

        /// <summary>
        /// Sub every actions
        /// </summary>
        private void Sub()
        {
            if (playerInput == null)
            {
                return;
            }

            foreach (InputActionMap map in playerInput.actions.actionMaps)
            {
                SubscribeControls(map);
            }
        }

        /// <summary>
        /// UnSub every actions
        /// </summary>
        private void UnSub()
        {
            if (playerInput == null)
            {
                return;
            }

            foreach (InputActionMap map in playerInput.actions.actionMaps)
            {
                UnsubscribeControls(map);
            }
        }

        #endregion
    }
}