using UnityEngine;
using UnityEngine.UI;

namespace Crimson.Core
{
    public class SliderParent : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Slider of this script")]
        [SerializeField]
        protected Slider sld = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            if (sld == null)
            {
                return;
            }

            sld.onValueChanged.AddListener(OnValueChanged);
            Subscribed();
        }

        protected virtual void OnDestroy()
        {
            if (sld == null)
            { 
                return; 
            }

            sld.onValueChanged.RemoveListener(OnValueChanged);
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// When the value changed, this method is called
        /// </summary>
        /// <param name="value"></param>
        protected virtual void OnValueChanged(float value)
        {

        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        protected virtual void Subscribed()
        {

        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        protected virtual void Unsubscribe()
        {

        }

        #endregion
    }
}