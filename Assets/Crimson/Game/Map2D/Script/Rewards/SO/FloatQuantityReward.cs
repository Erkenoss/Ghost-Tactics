using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "FloatQuantityReward", menuName = "Crimson/Map/Reward/FloatQuantityReward")]
    public class FloatQuantityReward : MapReward
    {
        #region Public Fields

        public float Quantity => Quantity;

        #endregion

        #region Private Fields

        [Tooltip("Quantity of the reward")]
        [SerializeField]
        private float quantity = 0f;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}