using UnityEngine;
using TMPro;
using I2.Loc;
using System.Collections.Generic;
using System;

namespace Crimson.Core
{
    public class DropDownTMPComponent : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Drop down of the script
        /// </summary>
        [SerializeField]
        protected TMP_Dropdown drop = null;

        /// <summary>
        /// List of the key defined the languages options of the dropdown
        /// </summary>
        protected List<string> keys = new List<string>();

        /// <summary>
        /// Page where the traduction of the option in the dropdown are containing
        /// </summary>
        [SerializeField]
        protected string pageKeys = string.Empty;

        /// <summary>
        /// Acton to set on the OnValueChange of the DD
        /// </summary>
        protected Action<int> changeValue = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            keys.Clear();

            foreach (var option in drop.options)
            {
                keys.Add(option.text);
            }
        }

        protected virtual void Start()
        {
            changeValue = value => ChangeDDSetting(value);
        }

        protected virtual void OnEnable()
        {
            UpdateLanguage();

            if (drop != null)
            {
                drop.onValueChanged.AddListener(changeValue.Invoke);
            }
        }

        protected virtual void OnDisable()
        {
            if (drop != null)
            {
                drop.onValueChanged.RemoveListener(changeValue.Invoke);
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Update the language of the option
        /// </summary>
        protected virtual void UpdateLanguage()
        {
            if (LanguagesManager.Instance == null)
            {
                return;
            }

            if (drop != null && !string.IsNullOrEmpty(pageKeys))
            {
                for (int i = 0; i < drop.options.Count; i++)
                {
                    drop.options[i].text = LanguagesManager.Instance.GetTranslation($"{pageKeys}/{keys[i]}");

                }

                drop.RefreshShownValue();
            }
        }

        /// <summary>
        /// Action when the DropDoan change the value
        /// </summary>
        /// <param name="value"></param>
        protected virtual void ChangeDDSetting(int value)
        {

        }

        #endregion
    }
}