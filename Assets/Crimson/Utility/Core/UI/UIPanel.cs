using I2.Loc;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Crimson.Core.UI
{
    public abstract class UIPanel : UIBehaviour
    {
        #region Public Fields

        public bool IsOpen => panel != null && panel.activeSelf;
        public LocalizedString Title { get { return title; } }

        #endregion

        #region Private Fields

        [Tooltip("What the title?")]
        [SerializeField]
        private LocalizedString title = null;

        [Tooltip("Panel we want to manage")]
        [SerializeField]
        private GameObject panel = null;

        [Tooltip("Type of this panel")]
        [SerializeField]
        private EUIType type = EUIType.None;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();
            Sub();
        }

        protected override void Start()
        {
            base.Start();
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.Add(type, this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unsub();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Open the panel
        /// </summary>
        public void TogglePanel()
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(!panel.activeSelf);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sue to sub in the EventBus
        /// </summary>
        protected virtual void Sub()
        { 
        
        }

        /// <summary>
        /// Use to sub in the EventBus
        /// </summary>
        protected virtual void Unsub()
        {

        }

        #endregion
    }
}