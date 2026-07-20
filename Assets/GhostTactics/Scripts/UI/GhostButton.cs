using Crimson.Core;
using GhostTactics.Core;
using GhostTactics.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class GhostButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Slot Container of the button")]
        [SerializeField]
        private GameObject buttonContainer = null;

        [Tooltip("Image of the button")]
        [SerializeField]
        private Image buttonImage = null;

        /// <summary>
        /// Use to move the button in the space
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// Use the ability of the ghost
        /// </summary>
        private AbilityData data = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Start()
        {
            base.Start();

            Subscribe();
        }

        private void Update()
        {
            if (isActive)
            {
                MoveButton();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void DisableButton(OnDisableButton d)
        {

        }

        protected override void EnableButton(OnEnableButton e)
        {

        }

        /// <summary>
        /// Update the data of the button base on the Ghost event
        /// </summary>
        /// <param name="g"></param>
        private void UpdateButtonData(OnGhostAction g)
        {
            if (g == null || g.Action == null || buttonImage == null || buttonContainer == null)
            {
                return;
            }

            buttonImage.sprite = g.Action.AbilityIcon;
            data = g.Action;

            buttonContainer.SetActive(true);
            isActive = true;
        }

        /// <summary>
        /// Move the button in the space
        /// </summary>
        private void MoveButton()
        {
            /// Move the container
        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnGhostAction>(UpdateButtonData);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnGhostAction>(UpdateButtonData);
        }
        
        protected override void OnClick()
        {
            if (buttonContainer == null)
            {
                return;
            }

            isActive = false;
            buttonContainer.SetActive(false);

            EventBus.Publish<OnRemoveGhostAction>(new OnRemoveGhostAction(data));
            EventBus.Publish<OnGhostUseAction>(new OnGhostUseAction(data));
            EventBus.Publish<OnEnableButton>(new OnEnableButton());
        }

        #endregion
    }
}