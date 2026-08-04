using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "IdentifierReward", menuName = "Crimson/Map/IdentifierReward")]
    public class IdentifierReward : MapReward
    {
        #region Public Fields

        public string Identifier => identifier;

        #endregion

        #region Private Fields

        [Tooltip("Identifier for the reward.")]
        [SerializeField]
        private string identifier = string.Empty;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}