using System;
using System.Collections.Generic;
using Tutorial.Runtime.Persistence;
using UnityEngine;

namespace Tutorial.Runtime.Catalogue
{
    /// <summary>
    /// Catalogue containing every tutorial graph available at runtime
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialRuntimeCatalogue", menuName = "Tutorial/Runtime Catalogue")]
    public sealed class TutorialRuntimeCatalogue : ScriptableObject
    {
        #region Serialized Fields

        /// <summary>
        /// Tutorial graph entries available through this catalogue
        /// </summary>
        [Tooltip("Tutorial graphs available at runtime")]
        [SerializeField]
        private List<TutorialRuntimeGraphEntry> graphs = new List<TutorialRuntimeGraphEntry>();

        #endregion

        #region Properties

        public IReadOnlyList<TutorialRuntimeGraphEntry> Graphs => graphs;

        #endregion

        #region Public Methods

        /// <summary>
        /// Try to retrieve one runtime catalogue graph entry from its associated scene path
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="graphEntry"></param>
        /// <returns></returns>
        public bool TryGetGraphEntryByScenePath(string scenePath, out TutorialRuntimeGraphEntry graphEntry)
        {
            graphEntry = null;

            if (string.IsNullOrWhiteSpace(scenePath) || graphs == null)
            {
                return false;
            }

            foreach (TutorialRuntimeGraphEntry candidate in graphs)
            {
                if (candidate == null || candidate.Graph == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.ScenePath, scenePath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (graphEntry != null)
                {
                    return false;
                }

                graphEntry = candidate;
            }

            return graphEntry != null;
        }


        /// <summary>
        /// Try to retrieve one runtime catalogue graph entry from its persistent tutorial GUID
        /// </summary>
        /// <param name="graphGuid"></param>
        /// <param name="graphEntry"></param>
        /// <returns></returns>
        public bool TryGetGraphEntry(string graphGuid, out TutorialRuntimeGraphEntry graphEntry)
        {
            graphEntry = null;

            if (string.IsNullOrWhiteSpace(graphGuid) || graphs == null)
            {
                return false;
            }

            foreach (TutorialRuntimeGraphEntry candidate in graphs)
            {
                if (candidate == null || candidate.Graph == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.GraphGuid, graphGuid, StringComparison.Ordinal))
                {
                    continue;
                }

                if (graphEntry != null)
                {
                    return false;
                }

                graphEntry = candidate;
            }

            return graphEntry != null;
        }

        #endregion
    }

    /// <summary>
    /// Runtime catalogue entry representing one TutorialGraphAsset
    /// </summary>
    [Serializable]
    public sealed class TutorialRuntimeGraphEntry
    {
        #region Serialized Fields

        /// <summary>
        /// Tutorial graph represented by this runtime catalogue entry
        /// </summary>
        [Tooltip("TutorialGraphAsset represented by this runtime catalogue entry")]
        [SerializeField]
        private TutorialGraphAsset graph = null;

        /// <summary>
        /// Scene path associated with this tutorial graph
        /// </summary>
        [Tooltip("Scene path associated with this tutorial graph")]
        [SerializeField]
        private string scenePath = string.Empty;

        #endregion

        #region Properties
        public string ScenePath => scenePath;
        public TutorialGraphAsset Graph => graph;
        public string GraphGuid => graph != null ? graph.GraphGuid : string.Empty;

        #endregion
    }
}