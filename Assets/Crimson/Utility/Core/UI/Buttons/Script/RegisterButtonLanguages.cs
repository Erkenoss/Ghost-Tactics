using UnityEngine;

namespace Crimson.Core.Settings.Languages
{
    public class RegisterButtonLanguages : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Enum to set languages of the button")]
        [SerializeField]
        private ELangs language = ELangs.None;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        protected override void OnClick()
        {
            EventBus.Publish<OnChangeLanguage>(new OnChangeLanguage(language));   
        }

        #endregion
    }
}