using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class EnnemySliderHealth : SliderParent
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnSubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// When the ennemy take damages, change the value of this slider
        /// </summary>
        /// <param name="e"></param>
        private void SliderChangeValue(OnEnnemyIsHit e)
        {
            if (e == null || sld == null)
            {
                return;
            }

            sld.value -= e.DamageTaken;

            if (sld.value < sld.minValue)
            {
                sld.value = sld.minValue;
            }
        }

        /// <summary>
        /// Update the max value of the slider
        /// </summary>
        /// <param name="newValue"></param>
        private void ChangeMaxValue(NextLevel lvl)
        {
            if (lvl == null || sld == null)
            {
                return;
            }

            sld.maxValue = lvl.Data.EnnemyLevel.EnnemyHealth;
            sld.value = sld.maxValue;
        }

        protected override void OnValueChanged(float value)
        {
            ///To manage the different visual effect on the slider when the ennemy lost any health
        }

        /// <summary>
        /// Subscribe with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnEnnemyIsHit>(SliderChangeValue);
            EventBus.Subscribe<NextLevel>(ChangeMaxValue);
        }

        /// <summary>
        /// Unsubscribe with the EventBus
        /// </summary>
        private void UnSubscribe()
        {
            EventBus.Unsubscribe<OnEnnemyIsHit>(SliderChangeValue);
            EventBus.Unsubscribe<NextLevel>(ChangeMaxValue);
        }

        #endregion
    }
}