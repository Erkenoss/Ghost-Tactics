using System;
using Tutorial.Runtime.Core;
using Tutorial.Runtime.Persistence;
using Tutorial.Runtime.Progress;

namespace Tutorial.Runtime.Replay
{
    /// <summary>
    /// Manage temporary tutorial replay sessions without modifying persistent completed progress
    /// </summary>
    public sealed class TutorialReplayService : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Runtime tutorial instance currently used by the active replay session
        /// </summary>
        private TutorialRuntimeInstance replayRuntimeInstance = null;

        /// <summary>
        /// Temporary progress service used exclusively by the active replay session
        /// </summary>
        private TutorialProgressService replayProgress = null;

        /// <summary>
        /// Preserved copy of the completed persistent progress that existed before the replay
        /// </summary>
        private TutorialProgressSaveData preservedProgress = null;

        /// <summary>
        /// Whether this replay service has been disposed
        /// </summary>
        private bool isDisposed = false;

        #endregion

        #region Properties

        public TutorialRuntimeInstance ReplayRuntimeInstance => replayRuntimeInstance;
        public TutorialProgressService ReplayProgress => replayProgress;
        public bool IsReplaying => replayProgress != null;
        public bool HasPreservedProgress => preservedProgress != null;
        public bool IsDisposed => isDisposed;

        #endregion

        #region Events

        public event Action<TutorialReplayService> ReplayStarted = null;
        public event Action<TutorialReplayService> ReplayCompleted = null;
        public event Action<TutorialReplayService> ReplayCancelled = null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Start a temporary replay from a fresh runtime instance while preserving completed persistent progress
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="persistentProgress"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryBeginReplay(TutorialRuntimeInstance runtimeInstance, TutorialProgressSaveData persistentProgress, out string error)
        {
            error = string.Empty;

            if (isDisposed)
            {
                error = "A disposed TutorialReplayService cannot start a replay.";

                return false;
            }

            if (IsReplaying)
            {
                error = "A tutorial replay session is already running.";

                return false;
            }

            if (runtimeInstance == null)
            {
                error = "The replay runtime instance is null.";

                return false;
            }

            if (runtimeInstance.IsDisposed)
            {
                error = "A disposed tutorial runtime instance cannot be used for replay.";

                return false;
            }

            if (runtimeInstance.Status != ETutorialRuntimeInstanceStatus.Ready)
            {
                error =
                    $"Tutorial runtime instance '{runtimeInstance.TutorialGuid}' cannot start a replay " +
                    $"from status '{runtimeInstance.Status}'.";

                return false;
            }

            if (runtimeInstance.ReplayPolicy != ETutorialReplayPolicy.Allowed)
            {
                error = $"Tutorial '{runtimeInstance.TutorialGuid}' does not allow replay.";

                return false;
            }

            if (!TryValidateCompletedProgress(runtimeInstance, persistentProgress, out error))
            {
                return false;
            }

            preservedProgress = CloneProgressData(persistentProgress);
            replayRuntimeInstance = runtimeInstance;
            replayProgress = new TutorialProgressService(runtimeInstance);

            if (!replayProgress.Start())
            {
                replayRuntimeInstance = null;
                replayProgress = null;
                preservedProgress = null;

                error = $"Temporary replay progress for tutorial '{runtimeInstance.TutorialGuid}' could not be started.";

                return false;
            }

            ReplayStarted?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Complete the active replay after its temporary progress reaches the Completed state
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryCompleteReplay(out string error)
        {
            error = string.Empty;

            if (isDisposed)
            {
                error = "A disposed TutorialReplayService cannot complete a replay.";

                return false;
            }

            if (!IsReplaying)
            {
                error = "No tutorial replay session is currently running.";

                return false;
            }

            if (!replayProgress.IsCompleted)
            {
                error = "The active tutorial replay cannot complete because its temporary progress is not completed.";

                return false;
            }

            replayRuntimeInstance = null;
            replayProgress = null;

            ReplayCompleted?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Cancel the active replay without modifying the preserved persistent progress
        /// </summary>
        /// <returns></returns>
        public bool CancelReplay()
        {
            if (isDisposed || !IsReplaying)
            {
                return false;
            }

            replayRuntimeInstance = null;
            replayProgress = null;

            ReplayCancelled?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Determine whether one progress service belongs to the active temporary replay session
        /// </summary>
        /// <param name="progressService"></param>
        /// <returns></returns>
        public bool IsReplayProgress(TutorialProgressService progressService)
        {
            return progressService != null && progressService == replayProgress;
        }

        /// <summary>
        /// Create an independent copy of the persistent progress preserved before the current or previous replay
        /// </summary>
        /// <returns></returns>
        public TutorialProgressSaveData CreatePreservedProgressCopy()
        {
            return preservedProgress != null ? CloneProgressData(preservedProgress) : null;
        }

        /// <summary>
        /// Release the active replay session and every event owned by this service
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            replayRuntimeInstance = null;
            replayProgress = null;
            preservedProgress = null;

            ReplayStarted = null;
            ReplayCompleted = null;
            ReplayCancelled = null;

            isDisposed = true;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate that persistent progress represents a completed version of the tutorial being replayed
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <param name="progressData"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool TryValidateCompletedProgress(TutorialRuntimeInstance runtimeInstance, TutorialProgressSaveData progressData, out string error)
        {
            error = string.Empty;

            if (runtimeInstance == null)
            {
                error = "The replay runtime instance is null.";

                return false;
            }

            if (progressData == null)
            {
                error = "Persistent tutorial progress is null.";

                return false;
            }

            if (progressData.Status != ETutorialProgressStatus.Completed)
            {
                error = $"Tutorial '{runtimeInstance.TutorialGuid}' cannot be replayed because its persistent progress status is '{progressData.Status}' instead of Completed.";

                return false;
            }

            TutorialProgressService validationProgress = new TutorialProgressService(runtimeInstance);

            if (!validationProgress.TryRestore(progressData, out error))
            {
                return false;
            }

            if (!validationProgress.IsCompleted)
            {
                error = $"Tutorial '{runtimeInstance.TutorialGuid}' persistent progress could be restored but is not completed.";

                return false;
            }

            return true;
        }

        #endregion

        #region Copy

        /// <summary>
        /// Create a deep copy of one serialized tutorial progress snapshot
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static TutorialProgressSaveData CloneProgressData(TutorialProgressSaveData source)
        {
            if (source == null)
            {
                return null;
            }

            source.EnsureInitialized();

            TutorialProgressSaveData clone = new TutorialProgressSaveData
            {
                Version = source.Version,
                TutorialGuid = source.TutorialGuid,
                GraphVersion = source.GraphVersion,
                Status = source.Status
            };

            foreach (TutorialProgressUnitSaveData completedUnit in source.CompletedUnits)
            {
                if (completedUnit == null)
                {
                    continue;
                }

                clone.CompletedUnits.Add(new TutorialProgressUnitSaveData
                {
                    UnitGuid = completedUnit.UnitGuid,
                    UnitType = completedUnit.UnitType
                });
            }

            return clone;
        }

        #endregion
    }
}