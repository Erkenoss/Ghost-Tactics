using UnityEngine;

namespace Crimson.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Public Fields

        public static T Instance { get { return instance; } set { instance = value; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Instance of all script. One instance by script
        /// </summary>
        private static T instance = null;

        /// <summary>
        /// Use to set if the manager persist in the different scene
        /// </summary>
        [SerializeField]
        private bool isPersistant = false;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;

            if (isPersistant)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Destroy the instance
        /// </summary>
        public static void DestroyInstance()
        {
            if (instance == null)
            {
                return;
            }

            Destroy(instance.gameObject);
            instance = null;
        }

        #endregion

        #region Private Methods
        #endregion
    }
}