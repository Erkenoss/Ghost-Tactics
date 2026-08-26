using Tutorial.Runtime;
using Tutorial.Runtime.Flow;
using UnityEngine;

namespace Crimson.Core.Settings
{
    public class BoolSliderTutorial : BoolSlider
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

        protected override void OnValueChanged(float value)
        {
            base.OnValueChanged(value);
            bool tutorialsEnabled = value == 0;
            TutoEventBus.Publish<OnTutorialsEnabledChanged>(new OnTutorialsEnabledChanged(tutorialsEnabled));
        }

        #endregion
    }
}