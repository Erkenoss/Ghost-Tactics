using UnityEngine;
using UnityEngine.UI;
using Crimson.Core;

namespace Crimson.Utilities
{
    public class RegisterButtonLanguages : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Button to set language
        /// </summary>
        [SerializeField]
        private Button button = null;

        /// <summary>
        /// Enum to set languages of the button
        /// </summary>
        [SerializeField]
        private ELangs language = ELangs.None;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            if (LanguagesManager.Instance != null)
            {
                button.onClick.AddListener(() => LanguagesManager.Instance.ChangeLanguages(language));
            }
        }

        private void OnDisable()
        {
            if (LanguagesManager.Instance != null)
            {
                button.onClick.RemoveListener(() => LanguagesManager.Instance.ChangeLanguages(language));
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}