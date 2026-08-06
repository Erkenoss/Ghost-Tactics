using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

using Tutorial.Runtime.Data;
using Tutorial.Runtime.Component;
using Tutorial.Editor.Core;
using Tutorial.Editor.Views;
using Tutorial.Editor.Services;

namespace Tutorial.Editor.Controllers
{
    /// <summary>
    /// Handle the global interactions of the tutorial graph canvas
    /// </summary>
    internal sealed class TutorialCanvasController : IDisposable
    {
        #region Constants

        /// <summary>
        /// Offset applied when several objects are dropped simultaneously
        /// </summary>
        private const float MultipleDropOffset = 24f;

        #endregion

        #region Events

        /// <summary>
        /// Raised once after a registered node position has changed
        /// </summary>
        public event Action NodePositionChanged = null;

        /// <summary>
        /// Raised after the structure or layout of the graph has changed
        /// </summary>
        public event Action GraphChanged = null;

        #endregion

        #region Private Fields

        /// <summary>
        /// Root element used to receive drag and drop events
        /// </summary>
        private readonly VisualElement pickingRoot = null;

        /// <summary>
        /// Main tutorial graph canvas
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Layer containing the graph connections
        /// </summary>
        private readonly VisualElement connectionLayer = null;

        /// <summary>
        /// Message displayed when the canvas contains no node
        /// </summary>
        private readonly Label dropHint = null;

        /// <summary>
        /// Temporary tutorial graph state
        /// </summary>
        private readonly TutorialGraphState graphState = null;

        /// <summary>
        /// Registry associating persistent node GUIDs with their runtime visual elements
        /// </summary>
        private readonly TutorialGraphRuntimeRegistry runtimeRegistry = null;

        /// <summary>
        /// Factory responsible for visual node creation
        /// </summary>
        private readonly TutorialNodeFactory nodeFactory = null;

        /// <summary>
        /// View responsible for the inspector panel
        /// </summary>
        private readonly TutorialInspectorView inspectorView = null;

        /// <summary>
        /// Controller responsible for StepSO to GameObject bindings
        /// </summary>
        private readonly TutorialBindingController bindingController = null;

        /// <summary>
        /// Controller responsible for StepSO sequence connections
        /// </summary>
        private readonly TutorialSequenceController sequenceController = null;

        /// <summary>
        /// Renderer responsible for drawing and detecting connections
        /// </summary>
        private readonly TutorialConnectionRenderer connectionRenderer = null;

        /// <summary>
        /// Whether the controller callbacks are currently registered
        /// </summary>
        private bool isEnabled = false;

        #endregion

        #region Constructor

        public TutorialCanvasController(VisualElement pickingRoot, VisualElement canvas, VisualElement connectionLayer, Label dropHint, TutorialGraphState graphState, TutorialGraphRuntimeRegistry runtimeRegistry, TutorialNodeFactory nodeFactory, TutorialInspectorView inspectorView, TutorialBindingController bindingController, TutorialSequenceController sequenceController, TutorialConnectionRenderer connectionRenderer)
        {
            this.pickingRoot = pickingRoot;
            this.canvas = canvas;
            this.connectionLayer = connectionLayer;
            this.dropHint = dropHint;
            this.graphState = graphState;
            this.runtimeRegistry = runtimeRegistry ?? throw new ArgumentNullException(nameof(runtimeRegistry));
            this.nodeFactory = nodeFactory;
            this.inspectorView = inspectorView;
            this.bindingController = bindingController;
            this.sequenceController = sequenceController;
            this.connectionRenderer = connectionRenderer;
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Register the canvas callbacks
        /// </summary>
        public void Enable()
        {
            if (isEnabled || pickingRoot == null || canvas == null)
            {
                return;
            }

            pickingRoot.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            pickingRoot.RegisterCallback<DragPerformEvent>(OnDragPerformed);
            canvas.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);
            canvas.RegisterCallback<KeyDownEvent>(OnCanvasKeyDown);

            isEnabled = true;

            UpdateDropHintVisibility();
        }

        /// <summary>
        /// Unregister the canvas callbacks
        /// </summary>
        public void Dispose()
        {
            if (!isEnabled)
            {
                return;
            }

            pickingRoot?.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            pickingRoot?.UnregisterCallback<DragPerformEvent>(OnDragPerformed);
            canvas?.UnregisterCallback<PointerDownEvent>(OnCanvasPointerDown);
            canvas?.UnregisterCallback<KeyDownEvent>(OnCanvasKeyDown);
            graphState?.ResetConnectionCreation();

            isEnabled = false;
        }

        #endregion

        #region Drag And Drop

        /// <summary>
        /// Update the drag and drop visual state
        /// </summary>
        /// <param name="dragEvent"></param>
        private void OnDragUpdated(DragUpdatedEvent dragEvent)
        {
            if (!IsPointerInsideCanvas(dragEvent.mousePosition))
            {
                return;
            }

            DragAndDrop.visualMode = HasSupportedDraggedObject() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            dragEvent.StopPropagation();
        }

        /// <summary>
        /// Create graph nodes from the dropped Unity objects
        /// </summary>
        /// <param name="dragEvent"></param>
        private void OnDragPerformed(DragPerformEvent dragEvent)
        {
            if (!IsPointerInsideCanvas(dragEvent.mousePosition))
            {
                return;
            }

            List<UnityObject> supportedObjects = GetSupportedDraggedObjects();

            if (supportedObjects.Count == 0)
            {
                return;
            }

            DragAndDrop.AcceptDrag();
            Vector2 basePosition = canvas.WorldToLocal(dragEvent.mousePosition);
            bool graphChanged = false;

            for (int i = 0; i < supportedObjects.Count; i++)
            {
                UnityObject target = supportedObjects[i];
                Vector2 offset = Vector2.one * MultipleDropOffset * i;
                Vector2 dropPosition = basePosition + offset;

                if (!TryCreateRegisteredNode(Guid.NewGuid().ToString("N"), target, dropPosition, false, out _, out string failureReason))
                {
                    Debug.LogError(failureReason, target);

                    continue;
                }

                graphChanged = true;
            }

            if (graphChanged)
            {
                GraphChanged?.Invoke();
            }
        }

        /// <summary>
        /// Check whether at least one dragged object is supported
        /// </summary>
        /// <returns></returns>
        private static bool HasSupportedDraggedObject()
        {
            foreach (UnityObject target in DragAndDrop.objectReferences)
            {
                if (IsSupportedObject(target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get every unique supported object from the current drag operation
        /// </summary>
        /// <returns></returns>
        private static List<UnityObject>GetSupportedDraggedObjects()
        {
            List<UnityObject> supportedObjects = new List<UnityObject>();
            HashSet<UnityObject> registeredObjects = new HashSet<UnityObject>();

            foreach (UnityObject target in DragAndDrop.objectReferences)
            {
                if (!IsSupportedObject(target) || !registeredObjects.Add(target))
                {
                    continue;
                }

                supportedObjects.Add(target);
            }

            return supportedObjects;
        }

        /// <summary>
        /// Check whether a Unity object can be represented by the tool
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static bool IsSupportedObject(UnityObject target)
        {
            if (target is StepSO)
            {
                return true;
            }

            if (target is not GameObject gameObject)
            {
                return false;
            }

            return gameObject.TryGetComponent<TutoIdentifier>(out _);
        }

        /// <summary>
        /// Check whether a world position is located inside the canvas
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private bool IsPointerInsideCanvas(Vector2 worldPosition)
        {
            return canvas != null && canvas.worldBound.Contains(worldPosition);
        }

        #endregion

        #region Node Registration

        /// <summary>
        /// Clear every visual graph element without modifying persistent assets
        /// </summary>
        public void ClearVisualGraph()
        {
            graphState.ResetConnectionCreation();
            graphState.ClearSelection();

            bindingController.ClearVisualConnections();
            sequenceController.ClearVisualConnections();

            List<VisualElement> nodes = canvas.Query<VisualElement>(className: TutorialNodeFactory.NodeClass).ToList();

            foreach (VisualElement node in nodes)
            {
                if (node == null)
                {
                    continue;
                }

                runtimeRegistry.TryUnregister(node);
                node.RemoveFromHierarchy();
            }

            runtimeRegistry.Clear();

            inspectorView.DisplayPlaceholder();

            UpdateDropHintVisibility();
            connectionRenderer.MarkDirty();
        }

        /// <summary>
        /// Restore every visual binding and sequence connection contained inside a load plan
        /// </summary>
        /// <param name="loadPlan"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryRestoreConnections(TutorialGraphLoadPlan loadPlan, out string failureReason)
        {
            failureReason = string.Empty;

            if (loadPlan == null)
            {
                failureReason = "The tutorial graph load plan is missing.";

                return false;
            }

            if (graphState.BindingConnections.Count > 0 || graphState.SequenceConnections.Count > 0)
            {
                failureReason = "The current graph connections must be empty before restoration.";

                return false;
            }

            if (loadPlan.Bindings != null)
            {
                foreach (TutorialResolvedBindingData bindingData in loadPlan.Bindings)
                {
                    if (bindingController.TryRestoreConnection(bindingData, runtimeRegistry, out failureReason))
                    {
                        continue;
                    }

                    RollbackRestoredConnections();

                    return false;
                }
            }

            if (loadPlan.Sequences != null)
            {
                foreach (TutorialResolvedSequenceData sequenceData in loadPlan.Sequences)
                {
                    if (sequenceController.TryRestoreConnection(sequenceData, runtimeRegistry, out failureReason))
                    {
                        continue;
                    }

                    RollbackRestoredConnections();

                    return false;
                }
            }

            connectionRenderer.MarkDirty();

            return true;
        }

        /// <summary>
        /// Remove every connection created during a failed graph restoration
        /// </summary>
        private void RollbackRestoredConnections()
        {
            bindingController.ClearVisualConnections();
            sequenceController.ClearVisualConnections();

            graphState.ClearSelection();
            inspectorView.DisplayPlaceholder();
            connectionRenderer.MarkDirty();
        }

        /// <summary>
        /// Create and register a visual node
        /// </summary>
        /// <param name="nodeGuid"></param>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <param name="useExactPosition"></param>
        /// <param name="node"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        private bool TryCreateRegisteredNode(string nodeGuid, UnityObject target, Vector2 position, bool useExactPosition, out VisualElement node, out string failureReason)
        {
            node = null;
            failureReason = string.Empty;

            if (string.IsNullOrWhiteSpace(nodeGuid))
            {
                failureReason = "A graph node cannot be created without a NodeGuid.";

                return false;
            }

            nodeGuid = nodeGuid.Trim();

            if (target == null)
            {
                failureReason = $"The target associated with node '{nodeGuid}' is missing.";

                return false;
            }

            if (runtimeRegistry.ContainsGuid(nodeGuid))
            {
                failureReason = $"The NodeGuid '{nodeGuid}' is already registered.";

                return false;
            }

            node = useExactPosition
                ? nodeFactory.CreateNodeAtPosition(target, position, OnNodeClicked, OnNodeMoveCompleted)
                : nodeFactory.CreateNode(target, position, OnNodeClicked, OnNodeMoveCompleted);

            if (node == null)
            {
                failureReason = $"Unable to create a graph node for '{target.name}'.";

                return false;
            }

            if (!runtimeRegistry.TryRegister(nodeGuid, target, node))
            {
                node.RemoveFromHierarchy();
                node = null;
                failureReason = $"Unable to register graph node '{nodeGuid}' for '{target.name}'.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Notify that the position of a registered node has changed
        /// </summary>
        /// <param name="node"></param>
        private void OnNodeMoveCompleted(VisualElement node)
        {
            if (node == null)
            {
                return;
            }

            if (!runtimeRegistry.TryGetNodeGuid(node, out _))
            {
                return;
            }

            NodePositionChanged?.Invoke();
            GraphChanged?.Invoke();
        }

        /// <summary>
        /// Restore every resolved node contained inside a tutorial graph load plan
        /// </summary>
        /// <param name="loadPlan"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryRestoreNodes(TutorialGraphLoadPlan loadPlan, out string failureReason)
        {
            failureReason = string.Empty;

            if (loadPlan == null)
            {
                failureReason = "The tutorial graph load plan is missing.";

                return false;
            }

            if (runtimeRegistry.Count > 0 || nodeFactory.HasAnyNode())
            {
                failureReason = "The canvas must be empty before restoring tutorial graph nodes.";

                return false;
            }

            graphState.ResetConnectionCreation();
            graphState.ClearSelection();
            inspectorView.DisplayPlaceholder();

            List<VisualElement> createdNodes = new List<VisualElement>();

            if (loadPlan.Nodes != null)
            {
                foreach (TutorialResolvedNodeData nodeData in loadPlan.Nodes)
                {
                    if (nodeData == null)
                    {
                        RollbackRestoredNodes(createdNodes);
                        failureReason = "The load plan contains an invalid node entry.";

                        return false;
                    }

                    if (!TryCreateRegisteredNode(nodeData.NodeGuid, nodeData.Target, nodeData.Position, true, out VisualElement node, out failureReason))
                    {
                        RollbackRestoredNodes(createdNodes);

                        return false;
                    }

                    createdNodes.Add(node);
                }
            }

            UpdateDropHintVisibility();
            connectionRenderer.MarkDirty();

            return true;
        }

        /// <summary>
        /// Remove nodes created during a failed restoration
        /// </summary>
        /// <param name="createdNodes"></param>
        private void RollbackRestoredNodes(IReadOnlyList<VisualElement> createdNodes)
        {
            if (createdNodes == null)
            {
                return;
            }

            for (int i = createdNodes.Count - 1; i >= 0; i--)
            {
                VisualElement node = createdNodes[i];

                if (node == null)
                {
                    continue;
                }

                runtimeRegistry.TryUnregister(node);
                node.RemoveFromHierarchy();
            }

            UpdateDropHintVisibility();
            connectionRenderer.MarkDirty();
        }

        #endregion

        #region Canvas Selection

        /// <summary>
        /// Handle a left click on the empty canvas or a connection
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnCanvasPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || graphState.IsCreatingConnection)
            {
                return;
            }

            Vector2 worldPosition = new Vector2(pointerEvent.position.x, pointerEvent.position.y);

            Vector2 localPosition = connectionLayer.WorldToLocal(worldPosition);
            SequenceConnection sequenceConnection = connectionRenderer.FindSequenceConnectionAt(localPosition);

            if (sequenceConnection != null)
            {
                SelectSequenceConnection(sequenceConnection);
                pointerEvent.StopPropagation();

                return;
            }

            BindingConnection bindingConnection = connectionRenderer.FindBindingConnectionAt(localPosition);

            if (bindingConnection != null)
            {
                SelectBindingConnection(bindingConnection);
                pointerEvent.StopPropagation();

                return;
            }

            ClearSelection();
        }

        /// <summary>
        /// Handle the selection of a visual node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="target"></param>
        private void OnNodeClicked(VisualElement node, UnityObject target)
        {
            if (node == null || target == null || graphState.IsCreatingConnection)
            {
                return;
            }

            VisualElement previousNode = graphState.SelectedNode;

            if (previousNode != null && previousNode != node)
            {
                nodeFactory.SetNodeSelected(previousNode, false);
            }

            graphState.SelectNode(node);
            nodeFactory.SetNodeSelected(node, true);

            node.Focus();

            switch (target)
            {
                case StepSO step:
                    inspectorView.DisplayStep(step);
                    break;

                case GameObject gameObject:
                    inspectorView.DisplayGameObject(gameObject, bindingController.GetLinkedSteps(gameObject));
                    break;

                default:
                    inspectorView.DisplayUnsupported(target);
                    break;
            }

            connectionRenderer.MarkDirty();
        }

        /// <summary>
        /// Select a StepSO to GameObject connection
        /// </summary>
        /// <param name="connection"></param>
        private void SelectBindingConnection(BindingConnection connection)
        {
            if (!graphState.SelectBindingConnection(connection))
            {
                return;
            }

            ClearSelectedNodeVisual();

            inspectorView.DisplayBindingConnection(connection);

            canvas.Focus();
            connectionRenderer.MarkDirty();
        }

        /// <summary>
        /// Select a StepSO to StepSO connection
        /// </summary>
        /// <param name="connection"></param>
        private void SelectSequenceConnection(SequenceConnection connection)
        {
            if (!graphState.SelectSequenceConnection(connection))
            {
                return;
            }

            ClearSelectedNodeVisual();

            inspectorView.DisplaySequenceConnection(connection);

            canvas.Focus();
            connectionRenderer.MarkDirty();
        }

        /// <summary>
        /// Clear every selected graph element
        /// </summary>
        private void ClearSelection()
        {
            ClearSelectedNodeVisual();

            graphState.ClearSelection();

            inspectorView.DisplayPlaceholder();
            connectionRenderer.MarkDirty();
        }

        /// <summary>
        /// Remove the visual selection of the selected node
        /// </summary>
        private void ClearSelectedNodeVisual()
        {
            VisualElement selectedNode = graphState.SelectedNode;

            if (selectedNode == null)
            {
                return;
            }

            nodeFactory.SetNodeSelected(selectedNode,false);
        }

        #endregion

        #region Deletion

        /// <summary>
        /// Delete the currently selected graph element
        /// </summary>
        /// <param name="keyEvent"></param>
        private void OnCanvasKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode != KeyCode.Delete && keyEvent.keyCode != KeyCode.Backspace)
            {
                return;
            }

            if (graphState.SelectedSequenceConnection != null)
            {
                sequenceController.DeleteConnection(graphState.SelectedSequenceConnection);

                inspectorView.DisplayPlaceholder();
                connectionRenderer.MarkDirty();

                keyEvent.StopPropagation();

                return;
            }

            if (graphState.SelectedBindingConnection != null)
            {
                bindingController.DeleteConnection(graphState.SelectedBindingConnection);

                inspectorView.DisplayPlaceholder();
                connectionRenderer.MarkDirty();

                keyEvent.StopPropagation();

                return;
            }

            if (graphState.SelectedNode == null)
            {
                return;
            }

            DeleteNode(graphState.SelectedNode);

            keyEvent.StopPropagation();
        }

        /// <summary>
        /// Remove a visual node and its associated graph connections
        /// </summary>
        /// <param name="node"></param>
        private void DeleteNode(VisualElement node)
        {
            if (node == null)
            {
                return;
            }

            CancelConnectionCreationFromNode(node);

            bool selectionAffected = IsSelectionAssociatedWithNode(node);

            bindingController.RemoveVisualConnectionsForNode(node);
            sequenceController.RemoveConnectionsForNode(node);
            
            if (selectionAffected)
            {
                graphState.ClearSelection();
                inspectorView.DisplayPlaceholder();
            }

            runtimeRegistry.TryUnregister(node);
            node.RemoveFromHierarchy();

            UpdateDropHintVisibility();
            connectionRenderer.MarkDirty();
            GraphChanged?.Invoke();
        }

        /// <summary>
        /// Cancel an active connection starting from a deleted node
        /// </summary>
        /// <param name="node"></param>
        private void CancelConnectionCreationFromNode(VisualElement node)
        {
            if (graphState.ConnectionSourceNode != node)
            {
                return;
            }

            switch (graphState.ActiveConnectionType)
            {
                case EConnectionCreationType.Binding:
                    bindingController.CancelConnection();
                    break;

                case EConnectionCreationType.Sequence:
                    sequenceController.CancelConnection();
                    break;

                default:
                    graphState.ResetConnectionCreation();
                    break;
            }
        }

        /// <summary>
        /// Check whether the current selection is linked to a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool IsSelectionAssociatedWithNode(VisualElement node)
        {
            if (graphState.SelectedNode == node)
            {
                return true;
            }

            BindingConnection bindingConnection = graphState.SelectedBindingConnection;

            if (bindingConnection != null && (bindingConnection.SourceNode == node || bindingConnection.TargetNode == node))
            {
                return true;
            }

            SequenceConnection sequenceConnection = graphState.SelectedSequenceConnection;

            return sequenceConnection != null && (sequenceConnection.SourceNode == node || sequenceConnection.TargetNode == node);
        }

        #endregion

        #region Drop Hint

        /// <summary>
        /// Display the drop hint only when no node is present
        /// </summary>
        private void UpdateDropHintVisibility()
        {
            if (dropHint == null || nodeFactory == null)
            {
                return;
            }

            dropHint.style.display = nodeFactory.HasAnyNode() ? DisplayStyle.None : DisplayStyle.Flex;
        }

        #endregion
    }
}