using System;
using UnityEngine;

namespace Crimson.Map
{
    public abstract class MapRewardHandler<TReward> : MonoBehaviour, IMapRewardHandler where TReward : MapReward
    {
        #region Public Fields

        public Type RewardType => typeof(TReward);

        #endregion

        #region Private Fields

        [Tooltip("Where the handler will bo stocked")]
        [SerializeField]
        protected MapRewardSystem system = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void OnEnable()
        {
            if (system == null)
            {
                return;
            }

            system.RegisterRewardHandler(this);
        }

        protected virtual void OnDisable()
        {
            if (system == null)
            {
                return;
            }

            system.UnRegisterRewardhandler(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply a reward to the handler. Returns true if the reward was successfully applied, false otherwise.
        /// </summary>
        /// <param name="reward"></param>
        /// <returns></returns>
        public bool Apply(MapReward reward)
        {
            if (reward is not TReward typedReward)
            {
                return false;
            }

            ApplyReward(typedReward);
            return true;
        }

        #endregion

        #region Private Methods

        protected abstract void ApplyReward(TReward reward);

        #endregion
    }
}