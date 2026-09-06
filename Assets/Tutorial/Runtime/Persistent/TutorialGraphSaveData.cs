using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial.Runtime.Persistence
{
    #region Enums

    /// <summary>
    /// Type of Unity object represented by a saved graph node
    /// </summary>
    public enum ETutorialNodeType
    {
        None,
        Step,
        GameObject
    }

    #endregion

    #region Graph

    /// <summary>
    /// Complete persistent representation of a tutorial graph
    /// </summary>
    [Serializable]
    public sealed class TutorialGraphSaveData
    {
        #region Constants

        /// <summary>
        /// Current version of the tutorial graph save format
        /// </summary>
        public const int CurrentVersion = 1;

        #endregion

        #region Public Fields

        /// <summary>
        /// Version used to serialize this graph
        /// </summary>
        public int Version = CurrentVersion;

        /// <summary>
        /// Nodes contained inside the tutorial graph
        /// </summary>
        public List<TutorialNodeSaveData> Nodes = new List<TutorialNodeSaveData>();

        /// <summary>
        /// StepSO to GameObject binding connections
        /// </summary>
        public List<TutorialBindingSaveData> Bindings = new List<TutorialBindingSaveData>();

        /// <summary>
        /// StepSO to StepSO sequence connections
        /// </summary>
        public List<TutorialSequenceSaveData> Sequences = new List<TutorialSequenceSaveData>();

        /// <summary>
        /// Visual state of the graph canvas
        /// </summary>
        public TutorialGraphViewSaveData View = new TutorialGraphViewSaveData();

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensure that every serialized graph container is available
        /// </summary>
        public void EnsureInitialized()
        {
            if (Nodes == null)
            {
                Nodes = new List<TutorialNodeSaveData>();
            }

            if (Bindings == null)
            {
                Bindings = new List<TutorialBindingSaveData>();
            }

            if (Sequences == null)
            {
                Sequences = new List<TutorialSequenceSaveData>();
            }

            if (View == null)
            {
                View = new TutorialGraphViewSaveData();
            }
        }

        /// <summary>
        /// Remove every saved graph element
        /// </summary>
        public void Clear()
        {
            EnsureInitialized();

            Version = CurrentVersion;

            Nodes.Clear();
            Bindings.Clear();
            Sequences.Clear();

            View.Reset();
        }

        #endregion
    }

    #endregion

    #region Node

    /// <summary>
    /// Persistent representation of a tutorial graph node
    /// </summary>
    [Serializable]
    public sealed class TutorialNodeSaveData
    {
        #region Public Fields

        /// <summary>
        /// Identifier of the visual node inside the tutorial graph
        /// </summary>
        public string NodeGuid = string.Empty;

        /// <summary>
        /// Type of object represented by the node
        /// </summary>
        public ETutorialNodeType NodeType = ETutorialNodeType.None;

        /// <summary>
        /// Unity asset GUID used when the node represents a StepSO
        /// </summary>
        public string AssetGuid = string.Empty;

        /// <summary>
        /// TutoIdentifier GUID used when the node represents a scene GameObject
        /// </summary>
        public string ObjectGuid = string.Empty;

        /// <summary>
        /// Unity asset GUID of the scene containing the target GameObject
        /// </summary>
        public string SceneAssetGuid = string.Empty;

        /// <summary>
        /// Last known path of the scene containing the target GameObject
        /// </summary>
        public string ScenePath = string.Empty;

        /// <summary>
        /// Position of the visual node inside the graph canvas
        /// </summary>
        public Vector2 Position = Vector2.zero;

        #endregion
    }

    #endregion

    #region Binding

    /// <summary>
    /// Persistent representation of a StepSO to GameObject binding
    /// </summary>
    [Serializable]
    public sealed class TutorialBindingSaveData
    {
        #region Public Fields

        /// <summary>
        /// NodeGuid of the source StepSO node
        /// </summary>
        public string SourceNodeGuid = string.Empty;

        /// <summary>
        /// NodeGuid of the target GameObject node
        /// </summary>
        public string TargetNodeGuid = string.Empty;

        #endregion
    }

    #endregion

    #region Sequence

    /// <summary>
    /// Persistent representation of a StepSO sequence connection
    /// </summary>
    [Serializable]
    public sealed class TutorialSequenceSaveData
    {
        #region Public Fields

        /// <summary>
        /// NodeGuid of the source StepSO node
        /// </summary>
        public string SourceNodeGuid = string.Empty;

        /// <summary>
        /// NodeGuid of the target StepSO node
        /// </summary>
        public string TargetNodeGuid = string.Empty;

        /// <summary>
        /// Unity asset GUID of the StepSequenceSO containing the connection
        /// </summary>
        public string SequenceAssetGuid = string.Empty;

        #endregion
    }

    #endregion

    #region View

    /// <summary>
    /// Persistent visual state of the tutorial graph canvas
    /// </summary>
    [Serializable]
    public sealed class TutorialGraphViewSaveData
    {
        #region Public Fields

        /// <summary>
        /// Global displacement of the graph canvas
        /// </summary>
        public Vector2 PanPosition = Vector2.zero;

        /// <summary>
        /// Zoom applied to the graph canvas
        /// </summary>
        public float Zoom = 1f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Restore the default graph view
        /// </summary>
        public void Reset()
        {
            PanPosition = Vector2.zero;
            Zoom = 1f;
        }

        #endregion
    }

    #endregion
}