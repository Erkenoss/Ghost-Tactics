using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Crimson.Core.Scenes
{
    public class ButtonChangeScene : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Button of this script")]
        [SerializeField]
        protected Button btn = null;

        [Tooltip("Scene name in string we want to load")]
        [SerializeField]
        protected string sceneName = null;

        [Tooltip("Scene we want to load")]
        [SerializeField]
        protected SceneGroupSO group = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void OnEnable()
        {
            if (btn == null)
            {
                return;
            }
            
            btn.onClick.AddListener(OnCLick);
        }

        protected virtual void OnDisable()
        {
            if (btn == null)
            {
                return;
            }
         
            btn.onClick.RemoveListener(OnCLick);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// When the player click on the button
        /// </summary>
        protected virtual void OnCLick()
        {
            _ = ChangeSceneAsync();
        }

        protected async virtual Task ChangeSceneAsync()
        {
            if (CrimsonSceneManager.Instance == null)
            {
                return;
            }

            await CrimsonSceneManager.Instance.UnloadCurrentGroup();

            if (string.IsNullOrEmpty(sceneName))
            {
                await CrimsonSceneManager.Instance.LoadGroupAsync(group);
            }
            else
            {
                await CrimsonSceneManager.Instance.LoadGroupAsync(sceneName);
            }
        }

        #endregion
    }
}
