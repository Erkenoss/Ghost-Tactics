using Crimson.Core;
using GhostTactics.Core;
using TMPro;
using UnityEngine;

namespace GhostTactics.UI
{
    public class PrievisualizedEnnemiesAction : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Text of the button")]
        [SerializeField]
        private TextMeshProUGUI buttonText = null;

        /// <summary>
        /// Use to know how many time the player can visualized ennemies actions before to choose his actions;
        /// </summary>
        private int visualized = 0;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
        }

        protected override void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {

        }

        /// <summary>
        /// Use to update the value of visualization
        /// </summary>
        /// <param name="level"></param>
        private void UpdateVisualized(NextLevel level)
        {
            if (level == null)
            {
                return;
            }


        }

        /// <summary>
        /// Subscribe the different event in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(UpdateVisualized);
        }

        /// <summary>
        /// Unsubscribe the different event in the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(UpdateVisualized);
        }

        #endregion

    }
}