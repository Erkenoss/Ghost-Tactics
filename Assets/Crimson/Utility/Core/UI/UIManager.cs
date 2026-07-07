using System.Collections.Generic;
using UnityEngine;
using Crimson.Core.Input;
using UnityEngine.InputSystem;
using TMPro;

namespace Crimson.Core.UI
{
    /// <summary>
    /// Use to Open or Close a panel
    /// </summary>
    public struct TogglePanelEvent
    {
        public EUIType Type;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="newType"></param>
        public TogglePanelEvent (EUIType newType)
        {
            Type = newType;
        }
    }

    /// <summary>
    /// The different type of UI in the game
    /// </summary>
    public enum EUIType
    {
        None = 0,
        Inventory,
        Skill,
        HUD,
        Character,
        Map
    }

    public class UIManager : Singleton<UIManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Title of the UIPage")]
        [SerializeField]
        private TextMeshProUGUI title = null;

        [Tooltip("Header of the UI Canvas")]
        [SerializeField]
        private GameObject header = null;

        /// <summary>
        /// Container of all UI in the game
        /// </summary>
        private Dictionary<EUIType, UIPanel> uiContainer = new Dictionary<EUIType, UIPanel>();

        /// <summary>
        /// Current panel actually enable
        /// </summary>
        private UIPanel currentPanel = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            Sub();
        }

        private void OnDestroy()
        {
            UnSub();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a panel in the dictionary uiContainer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="panel"></param>
        public void Add(EUIType type, UIPanel panel)
        {
            if (type == EUIType.None || panel == null)
            {
                return;
            }

            uiContainer[type] = panel;
        }

        /// <summary>
        /// Use to toogle a panel
        /// </summary>
        /// <param name="panelEvent"></param>
        public void TogglePanel(TogglePanelEvent panelEvent)
        {
            if (uiContainer.TryGetValue(panelEvent.Type, out UIPanel panel))
            {
                if (currentPanel != null && currentPanel != panel)
                {
                    currentPanel.TogglePanel();
                }

                panel.TogglePanel();
                currentPanel = panel.IsOpen ? panel : null;
            }

            if (header == null)
            {
                return;
            }

            if (currentPanel == null)
            {
                header.SetActive(false);
            }
            else
            {
                header.SetActive(true);

                if (title != null || panel.Title != null)
                {
                    title.text = panel.Title;
                }
            }
        }

        /// <summary>
        /// Publish an event to open or close the inventory
        /// </summary>
        public void OpenInventory(InputAction.CallbackContext context)
        {
            EventBus.Publish(new TogglePanelEvent(EUIType.Inventory));
        }

        /// <summary>
        /// Publish an event to open or close the skill panel
        /// </summary>
        /// <param name="context"></param>
        public void OpenSkill(InputAction.CallbackContext context)
        {
            EventBus.Publish(new TogglePanelEvent(EUIType.Skill));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sub the different input in the EventBus
        /// </summary>
        private void Sub()
        {
            if (InputManager.Instance == null)
            {
                return;    
            }

            InputManager.Instance.AddInput(ETypeMap.UI, EInputType.Inventory, InputActionPhase.Performed, OpenInventory);
            InputManager.Instance.AddInput(ETypeMap.UI, EInputType.Skill, InputActionPhase.Performed, OpenSkill);

            EventBus.Subscribe<TogglePanelEvent>(TogglePanel);
        }

        /// <summary>
        /// Unsub the differents input n the EventBus
        /// </summary>
        private void UnSub()
        {
            if (InputManager.Instance == null)
            {
                return;
            }

            InputManager.Instance.RemoveInput(ETypeMap.UI, EInputType.Inventory, InputActionPhase.Performed);
            InputManager.Instance.RemoveInput(ETypeMap.UI, EInputType.Skill, InputActionPhase.Performed);

            EventBus.Unsubscribe<TogglePanelEvent>(TogglePanel);
        }

        #endregion
    }
}