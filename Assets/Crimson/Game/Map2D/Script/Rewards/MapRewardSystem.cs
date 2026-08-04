using System.Collections.Generic;
using UnityEngine;
using System;

namespace Crimson.Map
{
    public enum ERewardType
    {
        None,
        IntQuantityReward,
        FloatQuantityReward,
        FlagReward,
        RewardCollection,
        IdentifierReward,
        AssetReward
    }

    public enum ERewardCollectionsType
    {
        None,
        All,
        Random,
        Choice
    }

    public class MapRewardSystem : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Dictionayr to centralized the reward by the various handlers
        /// </summary>
        private readonly Dictionary<Type, IMapRewardHandler> handlers = new();

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Register the handler in the dictionary
        /// </summary>
        /// <param name="hnadler"></param>
        public void RegisterRewardHandler(IMapRewardHandler handler)
        {
            handlers[handler.RewardType] = handler;
        }

        /// <summary>
        /// Unregister the handler in the dictionary
        /// </summary>
        /// <param name="handler"></param>
        public void UnRegisterRewardhandler(IMapRewardHandler handler)
        {
            handlers.Remove(handler.RewardType);
        }

        /// <summary>
        /// Grand a reward by it's type
        /// </summary>
        /// <param name="reward"></param>
        public void GrantReward(MapReward reward)
        {
            if (reward == null || !handlers.TryGetValue(reward.GetType(), out IMapRewardHandler handler))
            {
                return;
            }

            if (handler.Apply(reward))
            {
                MapEventBus.Publish(new MapEvent.OnRewardGranted(reward));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sub with the MapEventBus.
        /// </summary>
        private void Subscribe()
        {

        }

        /// <summary>
        /// Unsub with the MapEventBus.
        /// </summary>
        private void Unsubscribe()
        {

        }

        #endregion
    }
}