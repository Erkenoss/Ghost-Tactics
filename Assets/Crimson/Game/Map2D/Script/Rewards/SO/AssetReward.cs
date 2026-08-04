using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace Crimson.Map
{
    [CreateAssetMenu(fileName = "AssetReward", menuName = "Crimson/Map/AssetReward")]
    public class AssetReward : MapReward
    {
        #region Public Fields

        public List<AssetReference> Rewards => rewards;

        #endregion

        #region Private Fields

        [Tooltip("List of rewards that can be granted.")]
        [SerializeField]
        private List<AssetReference> rewards = new List<AssetReference>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
