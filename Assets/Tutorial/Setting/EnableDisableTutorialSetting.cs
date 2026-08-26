using Tutorial.Runtime;
using Tutorial.Runtime.Flow;
using UnityEngine;

namespace Tutorial.Integration
{
    public sealed class EnableDisableTutorialSetting : MonoBehaviour
    {
        /// <summary>
        /// Enable or disable tutorial execution
        /// </summary>
        /// <param name="enabled"></param>
        public void SetTutorialsEnabled(bool enabled)
        {
            TutoEventBus.Publish<OnTutorialsEnabledChanged>(new OnTutorialsEnabledChanged(enabled));
        }
    }
}