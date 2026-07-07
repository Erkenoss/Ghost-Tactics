using Crimson.Core;
using GhostTactics.Core;
using UnityEngine;

namespace GhostTactics.Ennemi
{
    public class EnnemiDieEvent
    {

    }

    public class EnnemiHealth : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// When the Ennemi died
        /// </summary>
        [ContextMenu("Die")]
        public void EnnemiDie()
        {
            EventBus.Publish<EnnemiDieEvent>(new EnnemiDieEvent());
        }
        
        #endregion
    }
}