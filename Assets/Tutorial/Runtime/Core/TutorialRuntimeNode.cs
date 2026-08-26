using System;
using System.Collections.Generic;
using Tutorial.Runtime.Data;

namespace Tutorial.Runtime.Core
{
    /// <summary>
    /// Represents one StepSO node inside a reconstructed runtime tutorial graph
    /// </summary>
    public sealed class TutorialRuntimeNode
    {
        #region Private Fields

        /// <summary>
        /// Unique identifier of the persistent visual node
        /// </summary>
        private readonly string nodeGuid = string.Empty;

        /// <summary>
        /// Persistent GUID of the StepSO represented by this node
        /// </summary>
        private readonly string stepGuid = string.Empty;

        /// <summary>
        /// Runtime StepSO clone executed by this node
        /// </summary>
        private readonly StepSO runtimeStep = null;

        /// <summary>
        /// Runtime nodes reachable after this node
        /// </summary>
        private readonly List<string> nextNodeGuids = new List<string>();

        /// <summary>
        /// Persistent Unity asset GUID of the StepSequenceSO owning this runtime node
        /// </summary>
        private string sequenceGuid = string.Empty;

        /// <summary>
        /// Whether this runtime node is the first Step of its owning sequence
        /// </summary>
        private bool isSequenceStart = false;

        /// <summary>
        /// Whether this runtime node is the last Step of its owning sequence
        /// </summary>
        private bool isSequenceEnd = false;

        #endregion

        #region Properties

        public string NodeGuid => nodeGuid;
        public string StepGuid => stepGuid;
        public StepSO RuntimeStep => runtimeStep;
        public IReadOnlyList<string> NextNodeGuids => nextNodeGuids;
        public bool IsSequence => runtimeStep is StepSequenceSO;
        public bool IsTerminal => nextNodeGuids.Count == 0;
        public string SequenceGuid => sequenceGuid;
        public bool IsSequenceMember => !string.IsNullOrWhiteSpace(sequenceGuid);
        public bool IsSequenceStart => isSequenceStart;
        public bool IsSequenceEnd => isSequenceEnd;

        #endregion

        #region Constructor

        public TutorialRuntimeNode(string nodeGuid, string stepGuid, StepSO runtimeStep)
        {
            this.nodeGuid = !string.IsNullOrWhiteSpace(nodeGuid) ? nodeGuid : throw new ArgumentException("The runtime node GUID cannot be empty.", nameof(nodeGuid));
            this.stepGuid = !string.IsNullOrWhiteSpace(stepGuid) ? stepGuid : throw new ArgumentException("The runtime Step GUID cannot be empty.", nameof(stepGuid));
            this.runtimeStep = runtimeStep != null ? runtimeStep : throw new ArgumentNullException(nameof(runtimeStep));
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Register one outgoing runtime transition
        /// </summary>
        /// <param name="targetNodeGuid"></param>
        /// <returns></returns>
        internal bool AddTransition(string targetNodeGuid)
        {
            if (string.IsNullOrWhiteSpace(targetNodeGuid) || nextNodeGuids.Contains(targetNodeGuid))
            {
                return false;
            }

            nextNodeGuids.Add(targetNodeGuid);

            return true;
        }

        /// <summary>
        /// Remove every outgoing runtime transition
        /// </summary>
        internal void ClearTransitions()
        {
            nextNodeGuids.Clear();
        }

        /// <summary>
        /// Configure the runtime sequence membership of this node
        /// </summary>
        /// <param name="sequenceGuid"></param>
        /// <param name="isSequenceStart"></param>
        /// <param name="isSequenceEnd"></param>
        /// <returns></returns>
        internal bool ConfigureSequenceMembership(string sequenceGuid, bool isSequenceStart, bool isSequenceEnd)
        {
            if (string.IsNullOrWhiteSpace(sequenceGuid))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(this.sequenceGuid) && !string.Equals(this.sequenceGuid, sequenceGuid, StringComparison.Ordinal))
            {
                return false;
            }

            this.sequenceGuid = sequenceGuid;
            this.isSequenceStart = isSequenceStart;
            this.isSequenceEnd = isSequenceEnd;

            return true;
        }

        #endregion
    }
}
