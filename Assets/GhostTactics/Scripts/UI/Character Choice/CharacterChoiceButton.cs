using Crimson.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    public class CharacterChoiceButton : ButtonParent
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("0 for male, 1 for female")]
        [SerializeField]
        private int gender = 0;

        [Tooltip("Image of this button model")]
        [SerializeField]
        private Image modelImage = null;

        [Tooltip("Use to change the value of the alpha changment when selected")]
        [SerializeField]
        private float imageAlphaMax = 0f;

        [Tooltip("use to change the value of the alpha changment when unselected")]
        [SerializeField]
        private float imageAlphaMin = 0f;

        [Tooltip("The up scale when selected")]
        [SerializeField]
        private float scaleUpgrader = 0f;

        [Tooltip("The down of the scale when not selected")]
        [SerializeField]
        private float scaleDowngrader = 0f;

        /// <summary>
        /// True if selected, else false
        /// </summary>
        private bool selected = false;

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
        #endregion

        #region Private Methods

        /// <summary>
        /// Manager the toogle of the button gender selection
        /// </summary>
        /// <param name="g"></param>
        private void ToogleSelected(OnGenderChoice g)
        {
            if (modelImage == null)
            {
                return;
            }

            selected = g.Button == this;

            float scale = selected ? scaleUpgrader : scaleDowngrader;
            transform.localScale = new Vector3(scale, scale, 1f);

            Color color = modelImage.color;
            color.a = selected ? imageAlphaMax : imageAlphaMin;
            modelImage.color = color;
        }

        protected override void OnClick()
        {
            if (selected)
            {
                base.OnClick();
                EventBus.Publish<OnConfirmGender>(new OnConfirmGender());
            }
            else
            {
                EventBus.Publish<OnGenderChoice>(new OnGenderChoice(gender, this));
            }
        }

        private void Subscribe()
        {
            EventBus.Subscribe<OnGenderChoice>(ToogleSelected);
        }

        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnGenderChoice>(ToogleSelected);
        }

        #endregion
    }
}