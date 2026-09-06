using Crimson.Core;
using GhostTactics.Core;

namespace GhostTactics.UI
{
    public class Previsualized : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Visualization of the player
        /// </summary>
        private int visualization = 0;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// Play the tutorial link with this button
        /// </summary>
        public void PlayTuto()
        {
        }
        
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            EventBus.Publish<Visualization>(new Visualization());
            visualization--;

            if (visualization <= 0)
            {
                visualization = 0;
                DisableButton();
            }

            PlayTuto();
        }

        /// <summary>
        /// Disable the button interaction
        /// </summary>
        private void DisableButton()
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = false;
        }

        /// <summary>
        /// Enable the button interaction
        /// </summary>
        private void EnableButton()
        {
            if (btn == null)
            {
                return;
            }

            btn.enabled = true;
        }

        /// <summary>
        /// Update the value of the visualization variable to manage the time number the player can use this button before disable it
        /// </summary>
        /// <param name="lvl"></param>
        private void UpdateVisualization(NextLevel lvl)
        {
            if (lvl == null)
            {
                return;
            }

            visualization = lvl.Player.VisualizationValue;

            if (visualization > 0)
            {
                EnableButton();
            }
            else
            {
                DisableButton();
            }
        }

        /// <summary>
        /// Subscribe with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(UpdateVisualization);
        }

        /// <summary>
        /// Unsubsscribe with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(UpdateVisualization);
        }

        #endregion
    }
}