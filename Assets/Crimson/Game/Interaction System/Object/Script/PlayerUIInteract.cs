using TMPro;
using UnityEngine;
using I2.Loc;

namespace Crimson.Interaction
{
    public class PlayerUIInteract : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Container of the E Image
        /// </summary>
        [SerializeField]
        private GameObject container = null;

        /// <summary>
        /// Ref to the PlayerInteract script
        /// </summary>
        [SerializeField]
        private PlayerInteract interact = null;

        /// <summary>
        /// Text to display the informations of the object
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI interactText = null;

        /// <summary>
        /// Where the text will be translate to display the name of the IIteractable object
        /// </summary>
        [SerializeField]
        private Localize interactTerm = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void OnEnable()
        {
            interact.ShowInteract += ShowAndHide;
        }

        private void OnDisable()
        {
            interact.ShowInteract -= ShowAndHide;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        private void ShowAndHide(IInteractable interactable)
        {
            if (interactable != null)
            {
                container.SetActive(true);
                string key = interactable.GetInteractText();

                interactTerm.SetTerm(key);
                string keyText = LocalizationManager.GetTranslation(key);

                interactText.text = keyText;
            }
            else
            {
                container.SetActive(false);
                interactText.text = "";
            }
        }

        #endregion
    }
}