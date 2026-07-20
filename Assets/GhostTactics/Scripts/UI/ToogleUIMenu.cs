using Crimson.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class OnTooglePanelEvent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Panel to toogle
        /// </summary>
        public GameObject Panel = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="go"></param>
        public OnTooglePanelEvent(GameObject go)
        {
            Panel = go;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class ToogleUIMenu : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Panel we want to manage with this button")]
        [SerializeField]
        private GameObject panelToToogle = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            EventBus.Publish<OnTooglePanelEvent>(new OnTooglePanelEvent(panelToToogle));
        }

        /// <summary>
        /// Toogle ON/OFF a UI panel
        /// </summary>
        private void TooglePanel(OnTooglePanelEvent p)
        {
            if (p == null || panelToToogle == null || p.Panel == null)
            {
                return;
            }

            if (panelToToogle == p.Panel)
            {
                panelToToogle.SetActive(!panelToToogle.activeSelf);
            }
            else
            {
                panelToToogle.SetActive(false);
            }
        }

        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventBus.Subscribe<OnTooglePanelEvent>(TooglePanel);
        }

        protected override void UnsubscribeEvent()
        {
            base.UnsubscribeEvent();

            EventBus.Unsubscribe<OnTooglePanelEvent>(TooglePanel);
        }

        #endregion
    }
}