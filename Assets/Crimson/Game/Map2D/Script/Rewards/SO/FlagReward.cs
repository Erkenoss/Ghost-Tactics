using UnityEngine;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "FlagReward", menuName = "Crimson/Map/Reward/FlagReward")]
    public class FlagReward : MapReward
    {
        #region Public Fields

        public bool Flag => flag;

        #endregion

        #region Private Fields

        [Tooltip("Flag value of the reward")]
        [SerializeField]
        private bool flag = false;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}