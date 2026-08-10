using System;
using System.Collections.Generic;
using Tutorial.Runtime.Core;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Persistence;

namespace Tutorial.Runtime.Progress
{
    /// <summary>
    /// Track, restore and export the persistent progress of one tutorial runtime instance
    /// </summary>
    public sealed class TutorialProgressService
    {
        #region Private Fields

        /// <summary>
        /// Runtime tutorial instance whose progress is managed by this service
        /// </summary>
        private readonly TutorialRuntimeInstance runtimeInstance = null;

        /// <summary>
        /// Runtime nodes successfully completed by the player
        /// </summary>
        private readonly HashSet<string> completedNodeGuids = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Runtime nodes explicitly skipped by the player or tutorial flow
        /// </summary>
        private readonly HashSet<string> skippedNodeGuids = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Next Step index to execute for every partially progressed sequence node
        /// </summary>
        private readonly Dictionary<string, int> sequenceNextStepIndices = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Current persistent progress status of this tutorial
        /// </summary>
        private ETutorialProgressStatus status = ETutorialProgressStatus.NotStarted;

        #endregion

        #region Properties

        public TutorialRuntimeInstance RuntimeInstance => runtimeInstance;
        public ETutorialProgressStatus Status => status;
        public int CompletedNodeCount => completedNodeGuids.Count;
        public int SkippedNodeCount => skippedNodeGuids.Count;
        public bool IsStarted => status != ETutorialProgressStatus.NotStarted;
        public bool IsInProgress => status == ETutorialProgressStatus.InProgress;
        public bool IsCompleted => status == ETutorialProgressStatus.Completed;

        #endregion

        #region Events

        public event Action<TutorialProgressService> Changed = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a progress service associated with one reconstructed tutorial runtime instance
        /// </summary>
        /// <param name="runtimeInstance"></param>
        public TutorialProgressService(TutorialRuntimeInstance runtimeInstance)
        {
            this.runtimeInstance = runtimeInstance != null ? runtimeInstance : throw new ArgumentNullException(nameof(runtimeInstance));

            if (runtimeInstance.IsDisposed)
            {
                throw new ArgumentException("A disposed tutorial runtime instance cannot own a progress service.", nameof(runtimeInstance));
            }
        }

        #endregion

        #region Public Lifecycle Methods

        /// <summary>
        /// Mark this tutorial as started for the first time
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (status != ETutorialProgressStatus.NotStarted)
            {
                return false;
            }

            status = ETutorialProgressStatus.InProgress;

            NotifyChanged();

            return true;
        }

        /// <summary>
        /// Reset every tracked progress value to a fresh tutorial state
        /// </summary>
        public void Reset()
        {
            completedNodeGuids.Clear();
            skippedNodeGuids.Clear();
            sequenceNextStepIndices.Clear();

            status = ETutorialProgressStatus.NotStarted;

            NotifyChanged();
        }

        #endregion

        #region Node Progress

        /// <summary>
        /// Determine whether one runtime node has already reached a terminal progress state
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool IsNodeFinished(string nodeGuid)
        {
            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                return false;
            }

            return completedNodeGuids.Contains(nodeGuid) || skippedNodeGuids.Contains(nodeGuid);
        }

        /// <summary>
        /// Determine whether one runtime node has been successfully completed
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool IsNodeCompleted(string nodeGuid)
        {
            return !string.IsNullOrWhiteSpace(nodeGuid) && completedNodeGuids.Contains(nodeGuid);
        }

        /// <summary>
        /// Determine whether one runtime node has been skipped
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool IsNodeSkipped(string nodeGuid)
        {
            return !string.IsNullOrWhiteSpace(nodeGuid) && skippedNodeGuids.Contains(nodeGuid);
        }

        /// <summary>
        /// Mark one runtime node as successfully completed
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool MarkNodeCompleted(string nodeGuid)
        {
            if (!CanUpdateNode(nodeGuid))
            {
                return false;
            }

            if (skippedNodeGuids.Contains(nodeGuid))
            {
                return false;
            }

            if (!completedNodeGuids.Add(nodeGuid))
            {
                return true;
            }

            sequenceNextStepIndices.Remove(nodeGuid);

            EvaluateCompletion();
            NotifyChanged();

            return true;
        }

        /// <summary>
        /// Mark one runtime node as skipped
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        public bool MarkNodeSkipped(string nodeGuid)
        {
            if (!CanUpdateNode(nodeGuid))
            {
                return false;
            }

            if (completedNodeGuids.Contains(nodeGuid))
            {
                return false;
            }

            if (!skippedNodeGuids.Add(nodeGuid))
            {
                return true;
            }

            sequenceNextStepIndices.Remove(nodeGuid);

            EvaluateCompletion();
            NotifyChanged();

            return true;
        }

        #endregion

        #region Sequence Progress

        /// <summary>
        /// Store the next Step index to execute inside one partially progressed runtime sequence
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="nextStepIndex"></param>
        /// <returns></returns>
        public bool SetSequenceNextStepIndex(string nodeGuid, int nextStepIndex)
        {
            if (!IsInProgress || IsNodeFinished(nodeGuid))
            {
                return false;
            }

            if (!TryGetRuntimeSequence(nodeGuid, out StepSequenceSO runtimeSequence))
            {
                return false;
            }

            if (runtimeSequence.SequenceSOList == null || nextStepIndex < 0 || nextStepIndex > runtimeSequence.SequenceSOList.Count)
            {
                return false;
            }

            if (sequenceNextStepIndices.TryGetValue(nodeGuid, out int registeredIndex) && registeredIndex == nextStepIndex)
            {
                return true;
            }

            sequenceNextStepIndices[nodeGuid] = nextStepIndex;

            NotifyChanged();

            return true;
        }

        /// <summary>
        /// Retrieve the next Step index saved for one partially progressed runtime sequence
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="nextStepIndex"></param>
        /// <returns></returns>
        public bool TryGetSequenceNextStepIndex(string nodeGuid, out int nextStepIndex)
        {
            nextStepIndex = 0;

            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                return false;
            }

            return sequenceNextStepIndices.TryGetValue(nodeGuid, out nextStepIndex);
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Create a serializable snapshot of the current tutorial progress
        /// </summary>
        /// <returns></returns>
        public TutorialProgressSaveData CreateSaveData()
        {
            TutorialProgressSaveData saveData = new TutorialProgressSaveData
            {
                Version = TutorialProgressSaveData.CurrentVersion,
                TutorialGuid = runtimeInstance.TutorialGuid,
                GraphVersion = runtimeInstance.SourceGraph.Version,
                Status = status
            };

            List<string> completedNodes = new List<string>(completedNodeGuids);
            completedNodes.Sort(StringComparer.Ordinal);
            saveData.CompletedNodeGuids.AddRange(completedNodes);

            List<string> skippedNodes = new List<string>(skippedNodeGuids);
            skippedNodes.Sort(StringComparer.Ordinal);
            saveData.SkippedNodeGuids.AddRange(skippedNodes);

            List<string> sequenceNodeGuids = new List<string>(sequenceNextStepIndices.Keys);
            sequenceNodeGuids.Sort(StringComparer.Ordinal);

            foreach (string nodeGuid in sequenceNodeGuids)
            {
                saveData.Sequences.Add(
                    new TutorialSequenceProgressSaveData
                    {
                        NodeGuid = nodeGuid,
                        NextStepIndex = sequenceNextStepIndices[nodeGuid]
                    }
                );
            }

            return saveData;
        }

        /// <summary>
        /// Restore persistent tutorial progress from previously serialized data
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryRestore(TutorialProgressSaveData saveData, out string error)
        {
            error = string.Empty;

            if (!TryValidateSaveData(
                saveData,
                out HashSet<string> restoredCompletedNodes,
                out HashSet<string> restoredSkippedNodes,
                out Dictionary<string, int> restoredSequenceIndices,
                out error
            ))
            {
                return false;
            }

            completedNodeGuids.Clear();
            skippedNodeGuids.Clear();
            sequenceNextStepIndices.Clear();

            completedNodeGuids.UnionWith(restoredCompletedNodes);
            skippedNodeGuids.UnionWith(restoredSkippedNodes);

            foreach (KeyValuePair<string, int> sequenceProgress in restoredSequenceIndices)
            {
                sequenceNextStepIndices.Add(sequenceProgress.Key, sequenceProgress.Value);
            }

            status = saveData.Status;

            NotifyChanged();

            return true;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Determine whether one runtime node can currently receive a terminal progress state
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <returns></returns>
        private bool CanUpdateNode(string nodeGuid)
        {
            if (!IsInProgress || string.IsNullOrWhiteSpace(nodeGuid))
            {
                return false;
            }

            return runtimeInstance.RuntimeNodes.ContainsKey(nodeGuid);
        }

        /// <summary>
        /// Resolve one runtime StepSequenceSO from its persistent runtime node GUID
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="runtimeSequence"></param>
        /// <returns></returns>
        private bool TryGetRuntimeSequence(string nodeGuid, out StepSequenceSO runtimeSequence)
        {
            runtimeSequence = null;

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                return false;
            }

            runtimeSequence = runtimeNode.RuntimeStep as StepSequenceSO;

            return runtimeSequence != null;
        }

        /// <summary>
        /// Validate and reconstruct every collection contained by persistent tutorial progress data
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="restoredCompletedNodes"></param>
        /// <param name="restoredSkippedNodes"></param>
        /// <param name="restoredSequenceIndices"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryValidateSaveData(
            TutorialProgressSaveData saveData,
            out HashSet<string> restoredCompletedNodes,
            out HashSet<string> restoredSkippedNodes,
            out Dictionary<string, int> restoredSequenceIndices,
            out string error
        )
        {
            restoredCompletedNodes = new HashSet<string>(StringComparer.Ordinal);
            restoredSkippedNodes = new HashSet<string>(StringComparer.Ordinal);
            restoredSequenceIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            error = string.Empty;

            if (saveData == null)
            {
                error = "Tutorial progress save data is null.";

                return false;
            }

            saveData.EnsureInitialized();

            if (saveData.Version != TutorialProgressSaveData.CurrentVersion)
            {
                error = $"Unsupported tutorial progress save version '{saveData.Version}'.";

                return false;
            }

            if (!string.Equals(saveData.TutorialGuid, runtimeInstance.TutorialGuid, StringComparison.Ordinal))
            {
                error =
                    $"Tutorial progress GUID '{saveData.TutorialGuid}' does not match runtime tutorial " +
                    $"'{runtimeInstance.TutorialGuid}'.";

                return false;
            }

            if (saveData.GraphVersion != runtimeInstance.SourceGraph.Version)
            {
                error =
                    $"Tutorial progress graph version '{saveData.GraphVersion}' does not match runtime graph version " +
                    $"'{runtimeInstance.SourceGraph.Version}'.";

                return false;
            }

            foreach (string nodeGuid in saveData.CompletedNodeGuids)
            {
                if (!TryRegisterRestoredNode(nodeGuid, restoredCompletedNodes, "completed", out error))
                {
                    return false;
                }
            }

            foreach (string nodeGuid in saveData.SkippedNodeGuids)
            {
                if (!TryRegisterRestoredNode(nodeGuid, restoredSkippedNodes, "skipped", out error))
                {
                    return false;
                }

                if (restoredCompletedNodes.Contains(nodeGuid))
                {
                    error = $"Runtime node '{nodeGuid}' is both completed and skipped inside tutorial progress.";

                    return false;
                }
            }

            foreach (TutorialSequenceProgressSaveData sequenceProgress in saveData.Sequences)
            {
                if (!TryValidateSequenceProgress(sequenceProgress, restoredCompletedNodes, restoredSkippedNodes, restoredSequenceIndices, out error))
                {
                    return false;
                }
            }

            int finishedNodeCount = restoredCompletedNodes.Count + restoredSkippedNodes.Count;

            if (saveData.Status == ETutorialProgressStatus.NotStarted && (finishedNodeCount > 0 || restoredSequenceIndices.Count > 0))
            {
                error = "A NotStarted tutorial cannot contain saved node or sequence progress.";

                return false;
            }

            if (saveData.Status == ETutorialProgressStatus.Completed && finishedNodeCount != runtimeInstance.RuntimeNodes.Count)
            {
                error = "A Completed tutorial must contain a terminal progress state for every runtime node.";

                return false;
            }

            if (saveData.Status == ETutorialProgressStatus.InProgress && finishedNodeCount == runtimeInstance.RuntimeNodes.Count)
            {
                error = "An InProgress tutorial cannot already contain a terminal state for every runtime node.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate and register one restored runtime node GUID
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="destination"></param>
        /// <param name="stateName"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryRegisterRestoredNode(string nodeGuid, HashSet<string> destination, string stateName, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                error = $"Tutorial progress contains an empty {stateName} node GUID.";

                return false;
            }

            if (!runtimeInstance.RuntimeNodes.ContainsKey(nodeGuid))
            {
                error = $"Tutorial progress references unknown {stateName} runtime node '{nodeGuid}'.";

                return false;
            }

            if (!destination.Add(nodeGuid))
            {
                error = $"Tutorial progress contains duplicated {stateName} runtime node '{nodeGuid}'.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate one restored partial sequence progress entry
        /// </summary>
        /// <param name="sequenceProgress"></param>
        /// <param name="restoredCompletedNodes"></param>
        /// <param name="restoredSkippedNodes"></param>
        /// <param name="restoredSequenceIndices"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryValidateSequenceProgress(
            TutorialSequenceProgressSaveData sequenceProgress,
            HashSet<string> restoredCompletedNodes,
            HashSet<string> restoredSkippedNodes,
            Dictionary<string, int> restoredSequenceIndices,
            out string error
        )
        {
            error = string.Empty;

            if (sequenceProgress == null)
            {
                error = "Tutorial progress contains a null sequence progress entry.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(sequenceProgress.NodeGuid))
            {
                error = "Tutorial progress contains a sequence entry with no runtime node GUID.";

                return false;
            }

            if (restoredCompletedNodes.Contains(sequenceProgress.NodeGuid) || restoredSkippedNodes.Contains(sequenceProgress.NodeGuid))
            {
                error = $"Finished runtime node '{sequenceProgress.NodeGuid}' cannot contain partial sequence progress.";

                return false;
            }

            if (!TryGetRuntimeSequence(sequenceProgress.NodeGuid, out StepSequenceSO runtimeSequence))
            {
                error = $"Runtime node '{sequenceProgress.NodeGuid}' does not reference a StepSequenceSO.";

                return false;
            }

            if (runtimeSequence.SequenceSOList == null ||
                sequenceProgress.NextStepIndex < 0 ||
                sequenceProgress.NextStepIndex > runtimeSequence.SequenceSOList.Count)
            {
                error =
                    $"Sequence progress index '{sequenceProgress.NextStepIndex}' is invalid for runtime node " +
                    $"'{sequenceProgress.NodeGuid}'.";

                return false;
            }

            if (!restoredSequenceIndices.TryAdd(sequenceProgress.NodeGuid, sequenceProgress.NextStepIndex))
            {
                error = $"Tutorial progress contains duplicated sequence node '{sequenceProgress.NodeGuid}'.";

                return false;
            }

            return true;
        }

        #endregion

        #region Completion

        /// <summary>
        /// Complete tutorial progress automatically when every runtime node has reached a terminal state
        /// </summary>
        private void EvaluateCompletion()
        {
            if (completedNodeGuids.Count + skippedNodeGuids.Count != runtimeInstance.RuntimeNodes.Count)
            {
                return;
            }

            status = ETutorialProgressStatus.Completed;
            sequenceNextStepIndices.Clear();
        }

        /// <summary>
        /// Notify external persistence systems that tutorial progress has changed
        /// </summary>
        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }

        #endregion
    }
}