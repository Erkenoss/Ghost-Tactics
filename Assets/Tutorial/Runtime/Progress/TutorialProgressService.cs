using System;
using System.Collections.Generic;
using Tutorial.Runtime.Core;
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
        /// Persistent standalone Step units already completed
        /// </summary>
        private readonly HashSet<string> completedStepUnitGuids = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Persistent Sequence units already completed
        /// </summary>
        private readonly HashSet<string> completedSequenceUnitGuids = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Total number of persistent units required to complete this tutorial
        /// </summary>
        private readonly int expectedPersistentUnitCount = 0;

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
        public int CompletedUnitCount => completedStepUnitGuids.Count + completedSequenceUnitGuids.Count;
        public int CompletedStepUnitCount => completedStepUnitGuids.Count;
        public int CompletedSequenceUnitCount => completedSequenceUnitGuids.Count;
        public int ExpectedPersistentUnitCount => expectedPersistentUnitCount;
        public int FinishedNodeCount => completedNodeGuids.Count + skippedNodeGuids.Count;

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

            expectedPersistentUnitCount = CountPersistentUnits(runtimeInstance);
        }

        /// <summary>
        /// Count every logical persistent unit represented by one runtime tutorial graph
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <returns></returns>
        private static int CountPersistentUnits(TutorialRuntimeInstance runtimeInstance)
        {
            if (runtimeInstance == null)
            {
                return 0;
            }

            int standaloneStepCount = 0;
            HashSet<string> sequenceGuids = new HashSet<string>(StringComparer.Ordinal);

            foreach (TutorialRuntimeNode runtimeNode in runtimeInstance.RuntimeNodes.Values)
            {
                if (runtimeNode == null)
                {
                    continue;
                }

                if (runtimeNode.IsSequenceMember)
                {
                    if (!string.IsNullOrWhiteSpace(runtimeNode.SequenceGuid))
                    {
                        sequenceGuids.Add(runtimeNode.SequenceGuid);
                    }

                    continue;
                }

                if (runtimeNode.IsSequence)
                {
                    if (!string.IsNullOrWhiteSpace(runtimeNode.StepGuid))
                    {
                        sequenceGuids.Add(runtimeNode.StepGuid);
                    }

                    continue;
                }

                standaloneStepCount++;
            }

            return standaloneStepCount + sequenceGuids.Count;
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
            completedStepUnitGuids.Clear();
            completedSequenceUnitGuids.Clear();

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

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
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

            return FinalizePersistentNodeProgress(runtimeNode);
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

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
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

            return FinalizePersistentNodeProgress(runtimeNode);
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Create a serializable snapshot containing every completed persistent tutorial unit
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

            foreach (string stepUnitGuid in completedStepUnitGuids)
            {
                saveData.CompletedUnits.Add(new TutorialProgressUnitSaveData
                {
                    UnitGuid = stepUnitGuid,
                    UnitType = ETutorialProgressUnitType.Step
                });
            }

            foreach (string sequenceUnitGuid in completedSequenceUnitGuids)
            {
                saveData.CompletedUnits.Add(new TutorialProgressUnitSaveData
                {
                    UnitGuid = sequenceUnitGuid,
                    UnitType = ETutorialProgressUnitType.Sequence
                });
            }

            saveData.CompletedUnits.Sort(CompareProgressUnits);

            return saveData;
        }

        /// <summary>
        /// Compare two persistent tutorial units to produce deterministic save ordering
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static int CompareProgressUnits(TutorialProgressUnitSaveData left, TutorialProgressUnitSaveData right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int typeComparison = left.UnitType.CompareTo(right.UnitType);

            if (typeComparison != 0)
            {
                return typeComparison;
            }

            return string.Compare(left.UnitGuid, right.UnitGuid, StringComparison.Ordinal);
        }

        /// <summary>
        /// Restore persistent tutorial progress from previously serialized completed units
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryRestore(TutorialProgressSaveData saveData, out string error)
        {
            error = string.Empty;

            if (!TryValidateSaveData(saveData, out HashSet<string> restoredFinishedNodes, out error))
            {
                return false;
            }

            completedNodeGuids.Clear();
            skippedNodeGuids.Clear();
            completedStepUnitGuids.Clear();
            completedSequenceUnitGuids.Clear();

            completedNodeGuids.UnionWith(restoredFinishedNodes);

            foreach (TutorialProgressUnitSaveData completedUnit in saveData.CompletedUnits)
            {
                if (completedUnit.UnitType == ETutorialProgressUnitType.Step)
                {
                    completedStepUnitGuids.Add(completedUnit.UnitGuid);
                    continue;
                }

                if (completedUnit.UnitType == ETutorialProgressUnitType.Sequence)
                {
                    completedSequenceUnitGuids.Add(completedUnit.UnitGuid);
                }
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
        /// Validate and reconstruct every completed runtime node represented by persistent tutorial units
        /// </summary>
        /// <param name="saveData"></param>
        /// <param name="restoredFinishedNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryValidateSaveData(TutorialProgressSaveData saveData, out HashSet<string> restoredFinishedNodes, out string error)
        {
            restoredFinishedNodes = new HashSet<string>(StringComparer.Ordinal);
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
                error = $"Tutorial progress GUID '{saveData.TutorialGuid}' does not match runtime tutorial '{runtimeInstance.TutorialGuid}'.";

                return false;
            }

            if (saveData.GraphVersion != runtimeInstance.SourceGraph.Version)
            {
                error = $"Tutorial progress graph version '{saveData.GraphVersion}' does not match runtime graph version '{runtimeInstance.SourceGraph.Version}'.";

                return false;
            }

            HashSet<string> registeredUnitKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (TutorialProgressUnitSaveData completedUnit in saveData.CompletedUnits)
            {
                if (!TryRegisterRestoredUnit(completedUnit, registeredUnitKeys, restoredFinishedNodes, out error))
                {
                    return false;
                }
            }

            if (saveData.Status == ETutorialProgressStatus.NotStarted && restoredFinishedNodes.Count > 0)
            {
                error = "A NotStarted tutorial cannot contain completed persistent units.";

                return false;
            }

            if (saveData.Status == ETutorialProgressStatus.Completed && restoredFinishedNodes.Count != runtimeInstance.RuntimeNodes.Count)
            {
                error = "A Completed tutorial must resolve every runtime node from its completed persistent units.";

                return false;
            }

            if (saveData.Status == ETutorialProgressStatus.InProgress && restoredFinishedNodes.Count == runtimeInstance.RuntimeNodes.Count)
            {
                error = "An InProgress tutorial cannot already resolve every runtime node as completed.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate one persistent completed unit and reconstruct its finished runtime nodes
        /// </summary>
        /// <param name="completedUnit"></param>
        /// <param name="registeredUnitKeys"></param>
        /// <param name="restoredFinishedNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryRegisterRestoredUnit(TutorialProgressUnitSaveData completedUnit, HashSet<string> registeredUnitKeys, HashSet<string> restoredFinishedNodes, out string error)
        {
            error = string.Empty;

            if (completedUnit == null)
            {
                error = "Tutorial progress contains a null completed unit.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(completedUnit.UnitGuid))
            {
                error = "Tutorial progress contains a completed unit with no GUID.";

                return false;
            }

            if (completedUnit.UnitType == ETutorialProgressUnitType.None)
            {
                error = $"Tutorial progress unit '{completedUnit.UnitGuid}' contains no valid unit type.";

                return false;
            }

            string unitKey = $"{completedUnit.UnitType}|{completedUnit.UnitGuid}";

            if (!registeredUnitKeys.Add(unitKey))
            {
                error = $"Tutorial progress contains duplicated completed unit '{unitKey}'.";

                return false;
            }

            if (completedUnit.UnitType == ETutorialProgressUnitType.Step)
            {
                return TryRestoreStandaloneStep(completedUnit.UnitGuid, restoredFinishedNodes, out error);
            }

            if (completedUnit.UnitType == ETutorialProgressUnitType.Sequence)
            {
                return TryRestoreSequence(completedUnit.UnitGuid, restoredFinishedNodes, out error);
            }

            error = $"Tutorial progress unit '{completedUnit.UnitGuid}' uses unsupported type '{completedUnit.UnitType}'.";

            return false;
        }

        /// <summary>
        /// Restore one completed standalone runtime Step
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="restoredFinishedNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryRestoreStandaloneStep(string nodeGuid, HashSet<string> restoredFinishedNodes, out string error)
        {
            error = string.Empty;

            if (!runtimeInstance.TryGetRuntimeNode(nodeGuid, out TutorialRuntimeNode runtimeNode))
            {
                error = $"Tutorial progress references unknown standalone runtime node '{nodeGuid}'.";

                return false;
            }

            if (runtimeNode.IsSequenceMember || runtimeNode.IsSequence)
            {
                error = $"Tutorial progress references sequence runtime node '{nodeGuid}' as a standalone completed Step.";

                return false;
            }

            if (!restoredFinishedNodes.Add(nodeGuid))
            {
                error = $"Runtime node '{nodeGuid}' was restored more than once.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Restore one completed persistent sequence by marking all of its runtime nodes as finished
        /// </summary>
        /// <param name="sequenceGuid"></param>
        /// <param name="restoredFinishedNodes"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryRestoreSequence(string sequenceGuid, HashSet<string> restoredFinishedNodes, out string error)
        {
            error = string.Empty;
            bool sequenceFound = false;

            foreach (TutorialRuntimeNode runtimeNode in runtimeInstance.RuntimeNodes.Values)
            {
                if (runtimeNode == null)
                {
                    continue;
                }

                bool isSequenceMember = runtimeNode.IsSequenceMember && string.Equals(runtimeNode.SequenceGuid, sequenceGuid, StringComparison.Ordinal);
                bool isSequenceNode = runtimeNode.IsSequence && string.Equals(runtimeNode.StepGuid, sequenceGuid, StringComparison.Ordinal);

                if (!isSequenceMember && !isSequenceNode)
                {
                    continue;
                }

                sequenceFound = true;

                if (!restoredFinishedNodes.Add(runtimeNode.NodeGuid))
                {
                    error = $"Runtime node '{runtimeNode.NodeGuid}' was restored by multiple persistent units.";

                    return false;
                }
            }

            if (!sequenceFound)
            {
                error = $"Tutorial progress references unknown runtime sequence '{sequenceGuid}'.";

                return false;
            }

            return true;
        }

        #endregion

        #region Completion

        /// <summary>
        /// Complete tutorial progress automatically when every persistent unit has been acquired
        /// </summary>
        private void EvaluateCompletion()
        {
            int completedUnitCount = completedStepUnitGuids.Count + completedSequenceUnitGuids.Count;

            if (completedUnitCount != expectedPersistentUnitCount)
            {
                return;
            }

            status = ETutorialProgressStatus.Completed;
        }

        /// <summary>
        /// Notify external persistence systems that tutorial progress has changed
        /// </summary>
        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Convert one finished runtime node into persistent progress when its logical unit is complete
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <returns></returns>
        private bool FinalizePersistentNodeProgress(TutorialRuntimeNode runtimeNode)
        {
            if (!TryCompletePersistentUnit(runtimeNode, out bool persistentChanged))
            {
                return false;
            }

            if (!persistentChanged)
            {
                return true;
            }

            EvaluateCompletion();
            NotifyChanged();

            return true;
        }

        /// <summary>
        /// Complete the persistent unit represented by one finished runtime node when possible
        /// </summary>
        /// <param name="runtimeNode"></param>
        /// <param name="persistentChanged"></param>
        /// <returns></returns>
        private bool TryCompletePersistentUnit(TutorialRuntimeNode runtimeNode, out bool persistentChanged)
        {
            persistentChanged = false;

            if (runtimeNode == null)
            {
                return false;
            }

            if (runtimeNode.IsSequenceMember)
            {
                if (!IsRuntimeSequenceFinished(runtimeNode.SequenceGuid))
                {
                    return true;
                }

                persistentChanged = completedSequenceUnitGuids.Add(runtimeNode.SequenceGuid);

                return true;
            }

            if (runtimeNode.IsSequence)
            {
                if (string.IsNullOrWhiteSpace(runtimeNode.StepGuid))
                {
                    return false;
                }

                persistentChanged = completedSequenceUnitGuids.Add(runtimeNode.StepGuid);

                return true;
            }

            persistentChanged = completedStepUnitGuids.Add(runtimeNode.NodeGuid);

            return true;
        }

        /// <summary>
        /// Determine whether every runtime node belonging to one sequence has reached a terminal runtime state
        /// </summary>
        /// <param name="sequenceGuid"></param>
        /// <returns></returns>
        private bool IsRuntimeSequenceFinished(string sequenceGuid)
        {
            if (string.IsNullOrWhiteSpace(sequenceGuid))
            {
                return false;
            }

            bool sequenceFound = false;

            foreach (TutorialRuntimeNode runtimeNode in runtimeInstance.RuntimeNodes.Values)
            {
                if (runtimeNode == null || !runtimeNode.IsSequenceMember)
                {
                    continue;
                }

                if (!string.Equals(runtimeNode.SequenceGuid, sequenceGuid, StringComparison.Ordinal))
                {
                    continue;
                }

                sequenceFound = true;

                if (!IsNodeFinished(runtimeNode.NodeGuid))
                {
                    return false;
                }
            }

            return sequenceFound;
        }

        #endregion

        #region Runtime Progress Snapshot

        /// <summary>
        /// Create an independent snapshot containing every runtime node already finished by persistent or current progress
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<string> CreateFinishedNodeGuidSnapshot()
        {
            HashSet<string> finishedNodeGuids = new HashSet<string>(completedNodeGuids, StringComparer.Ordinal);

            finishedNodeGuids.UnionWith(skippedNodeGuids);

            return finishedNodeGuids;
        }

        #endregion
    }
}