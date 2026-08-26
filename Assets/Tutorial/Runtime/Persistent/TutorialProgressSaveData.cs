using System;
using System.Collections.Generic;

namespace Tutorial.Runtime.Persistence
{
    /// <summary>
    /// Type of persistent tutorial unit stored inside runtime progress
    /// </summary>
    public enum ETutorialProgressUnitType
    {
        None = 0,
        Step = 1,
        Sequence = 2
    }

    /// <summary>
    /// Serializable completed persistent unit associated with one tutorial runtime graph
    /// </summary>
    [Serializable]
    public sealed class TutorialProgressUnitSaveData
    {
        /// <summary>
        /// Persistent identifier of the completed unit
        /// </summary>
        public string UnitGuid = string.Empty;

        /// <summary>
        /// Type of persistent tutorial unit represented by this entry
        /// </summary>
        public ETutorialProgressUnitType UnitType = ETutorialProgressUnitType.None;
    }

    /// <summary>
    /// Serializable progress data associated with one tutorial runtime graph
    /// </summary>
    [Serializable]
    public sealed class TutorialProgressSaveData
    {
        /// <summary>
        /// Current supported tutorial progress save format version
        /// </summary>
        public const int CurrentVersion = 2;

        /// <summary>
        /// Serialized tutorial progress format version
        /// </summary>
        public int Version = CurrentVersion;

        /// <summary>
        /// Persistent identifier of the tutorial graph
        /// </summary>
        public string TutorialGuid = string.Empty;

        /// <summary>
        /// Version of the tutorial graph used to create this progress
        /// </summary>
        public int GraphVersion = 0;

        /// <summary>
        /// Current persistent lifecycle status of the tutorial
        /// </summary>
        public ETutorialProgressStatus Status = ETutorialProgressStatus.NotStarted;

        /// <summary>
        /// Persistent tutorial units already completed by the player
        /// </summary>
        public List<TutorialProgressUnitSaveData> CompletedUnits = new List<TutorialProgressUnitSaveData>();

        /// <summary>
        /// Ensure every serialized progress collection is initialized
        /// </summary>
        public void EnsureInitialized()
        {
            if (CompletedUnits == null)
            {
                CompletedUnits = new List<TutorialProgressUnitSaveData>();
            }
        }
    }
}