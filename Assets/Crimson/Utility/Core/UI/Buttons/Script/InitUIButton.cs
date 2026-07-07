using UnityEngine;

namespace Crimson.Core
{
    public class InitUIButton : UIButton
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Component we want to pass in the action")]
        [SerializeField]
        private MonoBehaviour script = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            if (action is InitButtonBase _action)
            {
                _action.Init(script);
            }

            base.Awake();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}