using UnityEngine;

namespace Crimson.Map
{
    public static class MapEvent
    {
        #region Public Fields
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Event triggered when a reward is granted to the player.
        /// </summary>
        public class OnRewardGranted
        {
            public MapReward Reward { get; private set; }

            public OnRewardGranted(MapReward reward)
            {
                Reward = reward;
            }
        }

        #endregion

        #region Private Methods
        #endregion
    }
}