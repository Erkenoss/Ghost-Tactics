using UnityEngine;

namespace Crimson.Map
{
    public enum ENodeType
    {
        None,
        Default,
        Start,
        End,
        Combat,
        Elite,
        Boss,
        Event,
        Choice,
        Reward,
        Shop,
        Rest,
        Checkpoint,
        Secret
    }

    public enum ERouteDirection
    {
        None,
        Bidirectional,
        NodeAToNodeB,
        NodeBToNodeA
    }

    public class MapController : MonoBehaviour
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
        #endregion
    }
}