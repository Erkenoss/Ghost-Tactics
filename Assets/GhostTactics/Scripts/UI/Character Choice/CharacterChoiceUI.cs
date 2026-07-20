using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.UI
{
    public class OnGenderChoice
    {
        public int Gender = 0;

        public CharacterChoiceButton Button = null;

        public OnGenderChoice(int g, CharacterChoiceButton but)
        {
            Gender = g;
            Button = but;
        }
    }

    public class OnNeedCharacterGender
    {

    }

    public class OnConfirmGender
    {

    }

    public class CharacterChoiceUI : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Panel to display the gendre choice")]
        [SerializeField]
        private GameObject panel = null;
        
        /// <summary>
        /// use to knwo which gender the player take
        /// </summary>
        private int characterGender = 0;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Display the cahracter gender panel when needed
        /// </summary>
        /// <param name="n"></param>
        private void CharacterChoice(OnNeedCharacterGender n)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(true);
        }

        /// <summary>
        /// Switch the gender of the player
        /// </summary>
        /// <param name="g"></param>
        private void SwitchGender(OnGenderChoice g)
        {
            characterGender = g.Gender;
        }

        /// <summary>
        /// COnfirm the choice of the player
        /// </summary>
        private void ConfirmGender(OnConfirmGender g)
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.Player.UpdateGender(characterGender);
            GameManager.Instance.Player.UpdateHasBeenAlreadyCreated(true);
            GameManager.Instance.Player.Save();

            EventBus.Publish<StartGameEvent>(new StartGameEvent());
        }

        /// <summary>
        /// Sub in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnGenderChoice>(SwitchGender);
            EventBus.Subscribe<OnConfirmGender>(ConfirmGender);
            EventBus.Subscribe<OnNeedCharacterGender>(CharacterChoice);
        }

        /// <summary>
        /// Unsub in the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnGenderChoice>(SwitchGender);
            EventBus.Unsubscribe<OnConfirmGender>(ConfirmGender);
            EventBus.Unsubscribe<OnNeedCharacterGender>(CharacterChoice);
        }

        #endregion
    }
}