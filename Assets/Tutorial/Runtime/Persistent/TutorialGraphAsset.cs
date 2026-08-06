using System;
using UnityEngine;

namespace Tutorial.Runtime.Persistence
{
    /// <summary>
    /// Persistent asset containing the complete saved state of a tutorial graph
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialGraph", menuName = "Tutorial/Graph")]
    public sealed class TutorialGraphAsset : ScriptableObject
    {
        #region Private Fields

        /// <summary>
        /// Unique identifier of this tutorial graph asset
        /// </summary>
        [SerializeField]
        private string graphGuid = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Persistent data contained inside the tutorial graph
        /// </summary>
        [SerializeField]
        private TutorialGraphSaveData saveData = new TutorialGraphSaveData();

        /// <summary>
        /// Use to know if we can replay the tutorial or not
        /// </summary>
        [SerializeField]
        private ETutorialReplayPolicy replayPolicy = ETutorialReplayPolicy.Disabled;

        #endregion

        #region Properties

        public string GraphGuid => graphGuid;
        public TutorialGraphSaveData SaveData => saveData;
        public int Version => saveData != null ? saveData.Version : 0;
        public bool IsInitialized => !string.IsNullOrWhiteSpace(graphGuid) && saveData != null;
        public ETutorialReplayPolicy ReplayPolicy => replayPolicy;

        #endregion

        #region ScriptableObject Callbacks

        /// <summary>
        /// Ensure that the graph contains every required persistent value
        /// </summary>
        private void OnEnable()
        {
            EnsureInitialized();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate the serialized graph data inside the Unity Editor
        /// </summary>
        private void OnValidate()
        {
            EnsureInitialized();
        }
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize this asset as a completely new tutorial graph
        /// </summary>
        public void InitializeNewGraph()
        {
            graphGuid = GenerateGraphGuid();
            saveData = new TutorialGraphSaveData();

            saveData.EnsureInitialized();
        }

        /// <summary>
        /// Ensure that the graph identity and its data containers are available
        /// </summary>
        public void EnsureInitialized()
        {
            if (string.IsNullOrWhiteSpace(graphGuid))
            {
                graphGuid = GenerateGraphGuid();
            }

            if (saveData == null)
            {
                saveData = new TutorialGraphSaveData();
            }

            saveData.EnsureInitialized();
        }

        /// <summary>
        /// Remove every saved graph element while preserving the graph identity
        /// </summary>
        public void ClearGraph()
        {
            EnsureInitialized();
            saveData.Clear();
        }

        /// <summary>
        /// Generate a new identity for this graph
        /// </summary>
        public void RegenerateGraphGuid()
        {
            graphGuid = GenerateGraphGuid();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generate a unique tutorial graph identifier
        /// </summary>
        /// <returns></returns>
        private static string GenerateGraphGuid()
        {
            return Guid.NewGuid().ToString("N");
        }

        #endregion
    }
}