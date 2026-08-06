using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial
{
    /// <summary>
    /// Store the temporary state of the tutorial graph editor
    /// </summary>
    internal sealed class TutorialGraphState
    {
        #region Properties

        /// <summary>
        /// Every StepSO to GameObject connection currently displayed
        /// </summary>
        public IReadOnlyList<BindingConnection> BindingConnections => bindingConnections;

        /// <summary>
        /// Every StepSO to StepSO connection currently displayed
        /// </summary>
        public IReadOnlyList<SequenceConnection> SequenceConnections => sequenceConnections;

        /// <summary>
        /// Currently selected visual node
        /// </summary>
        public VisualElement SelectedNode => selectedNode;

        /// <summary>
        /// Currently selected StepSO to GameObject connection
        /// </summary>
        public BindingConnection SelectedBindingConnection => selectedBindingConnection;

        /// <summary>
        /// Currently selected StepSO to StepSO connection
        /// </summary>
        public SequenceConnection SelectedSequenceConnection => selectedSequenceConnection;

        /// <summary>
        /// Type of connection currently being created
        /// </summary>
        public EConnectionCreationType ActiveConnectionType => activeConnectionType;

        /// <summary>
        /// StepSO used as the source of the connection currently being created
        /// </summary>
        public StepSO ConnectionSourceStep => connectionSourceStep;

        /// <summary>
        /// Visual node used as the source of the connection currently being created
        /// </summary>
        public VisualElement ConnectionSourceNode => connectionSourceNode;

        /// <summary>
        /// Port used as the source of the connection currently being created
        /// </summary>
        public VisualElement ConnectionSourcePort => connectionSourcePort;

        /// <summary>
        /// Current pointer position inside the canvas
        /// </summary>
        public Vector2 ConnectionPointerPosition => connectionPointerPosition;

        /// <summary>
        /// Whether a connection is currently being created
        /// </summary>
        public bool IsCreatingConnection => activeConnectionType != EConnectionCreationType.None;

        /// <summary>
        /// Whether a binding connection is currently being created
        /// </summary>
        public bool IsCreatingBinding => activeConnectionType == EConnectionCreationType.Binding;

        /// <summary>
        /// Whether a sequence connection is currently being created
        /// </summary>
        public bool IsCreatingSequence => activeConnectionType == EConnectionCreationType.Sequence;

        #endregion

        #region Private Fields

        /// <summary>
        /// StepSO to GameObject connections
        /// </summary>
        private readonly List<BindingConnection> bindingConnections = new List<BindingConnection>();

        /// <summary>
        /// StepSO to StepSO connections
        /// </summary>
        private readonly List<SequenceConnection> sequenceConnections = new List<SequenceConnection>();

        /// <summary>
        /// Currently selected visual node
        /// </summary>
        private VisualElement selectedNode = null;

        /// <summary>
        /// Currently selected binding connection
        /// </summary>
        private BindingConnection selectedBindingConnection = null;

        /// <summary>
        /// Currently selected sequence connection
        /// </summary>
        private SequenceConnection selectedSequenceConnection = null;

        /// <summary>
        /// Type of connection currently being created
        /// </summary>
        private EConnectionCreationType activeConnectionType = EConnectionCreationType.None;

        /// <summary>
        /// StepSO used as the source of the current connection
        /// </summary>
        private StepSO connectionSourceStep = null;

        /// <summary>
        /// Visual node used as the source of the current connection
        /// </summary>
        private VisualElement connectionSourceNode = null;

        /// <summary>
        /// Port used as the source of the current connection
        /// </summary>
        private VisualElement connectionSourcePort = null;

        /// <summary>
        /// Current connection pointer position inside the canvas
        /// </summary>
        private Vector2 connectionPointerPosition =
            Vector2.zero;

        #endregion

        #region Binding Connections

        /// <summary>
        /// Add a StepSO to GameObject connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool AddBindingConnection(BindingConnection connection)
        {
            if (connection == null || !connection.IsValid || bindingConnections.Contains(connection))
            {
                return false;
            }

            bindingConnections.Add(connection);
            return true;
        }

        /// <summary>
        /// Remove a StepSO to GameObject connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool RemoveBindingConnection(BindingConnection connection)
        {
            if (connection == null || !bindingConnections.Remove(connection))
            {
                return false;
            }

            if (selectedBindingConnection == connection)
            {
                selectedBindingConnection = null;
            }

            return true;
        }

        /// <summary>
        /// Remove every binding connection matching a condition
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int RemoveBindingConnections(Predicate<BindingConnection> predicate)
        {
            if (predicate == null)
            {
                return 0;
            }

            int removedCount = bindingConnections.RemoveAll(predicate);

            if (selectedBindingConnection != null && !bindingConnections.Contains(selectedBindingConnection))
            {
                selectedBindingConnection = null;
            }

            return removedCount;
        }

        #endregion

        #region Sequence Connections

        /// <summary>
        /// Add a StepSO to StepSO connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool AddSequenceConnection(SequenceConnection connection)
        {
            if (connection == null || !connection.IsValid || sequenceConnections.Contains(connection))
            {
                return false;
            }

            sequenceConnections.Add(connection);

            return true;
        }

        /// <summary>
        /// Remove a StepSO to StepSO connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool RemoveSequenceConnection(SequenceConnection connection)
        {
            if (connection == null || !sequenceConnections.Remove(connection))
            {
                return false;
            }

            if (selectedSequenceConnection == connection)
            {
                selectedSequenceConnection = null;
            }

            return true;
        }

        /// <summary>
        /// Remove every sequence connection matching a condition
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int RemoveSequenceConnections(Predicate<SequenceConnection> predicate)
        {
            if (predicate == null)
            {
                return 0;
            }

            int removedCount = sequenceConnections.RemoveAll(predicate);

            if (selectedSequenceConnection != null && !sequenceConnections.Contains(selectedSequenceConnection))
            {
                selectedSequenceConnection = null;
            }

            return removedCount;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Select a visual node
        /// </summary>
        /// <param name="node"></param>
        public void SelectNode(VisualElement node)
        {
            selectedNode = node;
            selectedBindingConnection = null;
            selectedSequenceConnection = null;
        }

        /// <summary>
        /// Select a binding connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool SelectBindingConnection(BindingConnection connection)
        {
            if (connection == null || !bindingConnections.Contains(connection))
            {
                return false;
            }

            selectedNode = null;
            selectedSequenceConnection = null;
            selectedBindingConnection = connection;

            return true;
        }

        /// <summary>
        /// Select a sequence connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool SelectSequenceConnection(SequenceConnection connection)
        {
            if (connection == null || !sequenceConnections.Contains(connection))
            {
                return false;
            }

            selectedNode = null;
            selectedBindingConnection = null;
            selectedSequenceConnection = connection;

            return true;
        }

        /// <summary>
        /// Clear the selected connections while preserving the selected node
        /// </summary>
        public void ClearConnectionSelection()
        {
            selectedBindingConnection = null;
            selectedSequenceConnection = null;
        }

        /// <summary>
        /// Clear every current selection
        /// </summary>
        public void ClearSelection()
        {
            selectedNode = null;
            selectedBindingConnection = null;
            selectedSequenceConnection = null;
        }

        #endregion

        #region Connection Creation

        /// <summary>
        /// Start creating a new graph connection
        /// </summary>
        /// <param name="connectionType"></param>
        /// <param name="sourceStep"></param>
        /// <param name="sourceNode"></param>
        /// <param name="sourcePort"></param>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool TryBeginConnectionCreation(EConnectionCreationType connectionType, StepSO sourceStep, VisualElement sourceNode, VisualElement sourcePort, Vector2 pointerPosition)
        {
            if (connectionType == EConnectionCreationType.None || IsCreatingConnection || sourceStep == null || sourceNode == null || sourcePort == null)
            {
                return false;
            }

            activeConnectionType = connectionType;
            connectionSourceStep = sourceStep;
            connectionSourceNode = sourceNode;
            connectionSourcePort = sourcePort;
            connectionPointerPosition = pointerPosition;

            return true;
        }

        /// <summary>
        /// Update the position of the connection currently being created
        /// </summary>
        /// <param name="connectionType"></param>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool TryUpdateConnectionCreation(EConnectionCreationType connectionType, Vector2 pointerPosition)
        {
            if (activeConnectionType != connectionType)
            {
                return false;
            }

            connectionPointerPosition = pointerPosition;

            return true;
        }

        /// <summary>
        /// Reset the connection currently being created when its type matches
        /// </summary>
        /// <param name="connectionType"></param>
        /// <returns></returns>
        public bool TryResetConnectionCreation(EConnectionCreationType connectionType)
        {
            if (activeConnectionType != connectionType)
            {
                return false;
            }

            ResetConnectionCreation();

            return true;
        }

        /// <summary>
        /// Reset the connection currently being created
        /// </summary>
        public void ResetConnectionCreation()
        {
            activeConnectionType = EConnectionCreationType.None;

            connectionSourceStep = null;
            connectionSourceNode = null;
            connectionSourcePort = null;

            connectionPointerPosition = Vector2.zero;
        }

        #endregion

        #region Reset

        /// <summary>
        /// Clear every temporary graph value
        /// </summary>
        public void Clear()
        {
            bindingConnections.Clear();
            sequenceConnections.Clear();

            ClearSelection();
            ResetConnectionCreation();
        }

        #endregion
    }
}