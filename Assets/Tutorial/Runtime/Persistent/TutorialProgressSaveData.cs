using System;
using System.Collections.Generic;

namespace Tutorial.Runtime.Persistence
{
    /// <summary>
    /// Serializable progress data associated with one tutorial runtime graph
    /// </summary>
    [Serializable]
    public sealed class TutorialProgressSaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string TutorialGuid = string.Empty;
        public int GraphVersion = 0;
        public ETutorialProgressStatus Status = ETutorialProgressStatus.NotStarted;
        public List<string> CompletedNodeGuids = new List<string>();
        public List<string> SkippedNodeGuids = new List<string>();
        public List<TutorialSequenceProgressSaveData> Sequences = new List<TutorialSequenceProgressSaveData>();

        /// <summary>
        /// Ensure every serialized progress collection is initialized
        /// </summary>
        public void EnsureInitialized()
        {
            if (CompletedNodeGuids == null)
            {
                CompletedNodeGuids = new List<string>();
            }

            if (SkippedNodeGuids == null)
            {
                SkippedNodeGuids = new List<string>();
            }

            if (Sequences == null)
            {
                Sequences = new List<TutorialSequenceProgressSaveData>();
            }
        }
    }

    /// <summary>
    /// Serializable progress information associated with one runtime sequence node
    /// </summary>
    [Serializable]
    public sealed class TutorialSequenceProgressSaveData
    {
        public string NodeGuid = string.Empty;
        public int NextStepIndex = 0;
    }
}