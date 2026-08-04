using UnityEngine;
using System.Collections.Generic;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "RewardCollection", menuName = "Crimson/Map/RewardCollection")]
    public class RewardCollection : MapReward
    {
        #region Public Fields

        public ERewardCollectionsType CollectionType => collectionType;
        public List<MapReward> Collections => collection;

        #endregion

        #region Private Fields

        [Tooltip("Type of collection to be granted.")]
        [SerializeField]
        private ERewardCollectionsType collectionType = ERewardCollectionsType.None;    

        [Tooltip("Collection of rewards that can be granted.")]
        [SerializeField]
        private List<MapReward> collection = new List<MapReward>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}