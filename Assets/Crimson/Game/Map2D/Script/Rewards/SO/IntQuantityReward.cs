using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "IntQuantityReward", menuName = "Crimson/Map/Rewards/IntQuantityReward")]
    public class IntQuantityReward : MapReward
    {
        #region Public Fields

        public int Quantity => quantity;

        #endregion

        #region Private Fields

        [Tooltip("Quantity of the reward to be granted.")]
        [SerializeField]
        private int quantity = 0;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}