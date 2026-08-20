using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Tutorial.Editor.Core;
using Tutorial.Runtime.Data;
using Tutorial.Runtime.Components;
using Tutorial.Editor.Services;
using Tutorial.Editor.Views;

namespace Tutorial.Editor.Controllers
{
    /// <summary>
    /// Manage the visual and persistent bindings between StepSO assets and GameObjects containing a TutoIdentifier
    /// </summary>
    internal sealed class TutorialBindingController
    {
        #region Private Fields

        /// <summary>
        /// Temporary state of the tutorial graph
        /// </summary>
        private readonly TutorialGraphState graphState = null;

        /// <summary>
        /// Main tutorial graph canvas
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Service responsible for tutorial GUID validation and generation
        /// </summary>
        private readonly TutorialGuidService guidService = null;

        /// <summary>
        /// View responsible for the inspector panel
        /// </summary>
        private readonly TutorialInspectorView inspectorView = null;

        /// <summary>
        /// Renderer responsible for graph connection drawing
        /// </summary>
        private readonly TutorialConnectionRenderer connectionRenderer = null;

        #endregion

        #region Events

        /// <summary>
        /// Raised after a manual binding modification
        /// </summary>
        public event Action BindingChanged = null;

        #endregion

        #region Constructor

        public TutorialBindingController(TutorialGraphState graphState, VisualElement canvas, TutorialGuidService guidService, TutorialInspectorView inspectorView, TutorialConnectionRenderer connectionRenderer)
        {
            this.graphState = graphState ?? throw new ArgumentNullException(nameof(graphState));
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.guidService = guidService ?? throw new ArgumentNullException(nameof(guidService));
            this.inspectorView = inspectorView ?? throw new ArgumentNullException(nameof(inspectorView));
            this.connectionRenderer = connectionRenderer ?? throw new ArgumentNullException(nameof(connectionRenderer));
        }

        #endregion

        #region Connection Creation

        /// <summary>
        /// Start creating a StepSO to GameObject binding
        /// </summary>
        /// <param name="step"></param>
        /// <param name="sourceNode"></param>
        /// <param name="sourcePort"></param>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool BeginConnection(StepSO step, VisualElement sourceNode, VisualElement sourcePort, Vector2 pointerPosition)
        {
            if (step == null || sourceNode == null || sourcePort == null)
            {
                return false;
            }

            Vector2 localPointerPosition = canvas.WorldToLocal(pointerPosition);
            bool hasStarted = graphState.TryBeginConnectionCreation(EConnectionCreationType.Binding, step, sourceNode, sourcePort, localPointerPosition);

            if (hasStarted)
            {
                connectionRenderer.MarkDirty();
            }

            return hasStarted;
        }

        /// <summary>
        /// Update the temporary binding position
        /// </summary>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool UpdateConnection(Vector2 pointerPosition)
        {
            Vector2 localPointerPosition = canvas.WorldToLocal(pointerPosition);
            bool hasUpdated = graphState.TryUpdateConnectionCreation(EConnectionCreationType.Binding, localPointerPosition);

            if (hasUpdated)
            {
                connectionRenderer.MarkDirty();
            }

            return hasUpdated;
        }

        /// <summary>
        /// Complete the current StepSO to GameObject binding
        /// </summary>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        public bool EndConnection(Vector2 pointerPosition)
        {
            if (!graphState.IsCreatingBinding)
            {
                return false;
            }
                
            bool connectionCreated = false;

            try
            {
                StepSO sourceStep = graphState.ConnectionSourceStep;
                VisualElement sourceNode = graphState.ConnectionSourceNode;
                VisualElement sourcePort = graphState.ConnectionSourcePort;

                if (sourceStep == null || sourceNode == null || sourcePort == null)
                {
                    return false;
                }

                VisualElement targetPort = FindBindingInputPort(pointerPosition);

                if (targetPort == null || targetPort.userData is not VisualElement targetNode)
                {
                    return false;
                }

                if (targetNode.userData is not GameObject targetGameObject)
                {
                    return false;
                }

                if (!targetGameObject.TryGetComponent(out TutoIdentifier identifier))
                {
                    return false;
                }

                BindingConnection connection = new BindingConnection(sourceStep, sourceNode, sourcePort, targetNode, targetPort);

                if (!connection.IsValid)
                {
                    return false;
                }

                if (!TrySetPersistentBinding(sourceStep, identifier))
                {
                    return false;
                }

                RemoveExistingVisualConnections(sourceStep);

                if (!graphState.AddBindingConnection(connection))
                {
                    Debug.LogError($"Unable to register the binding connection between '{sourceStep.name}' and '{targetGameObject.name}'.", sourceStep);
                    return false;
                }

                RefreshSelectedInspector(sourceStep, sourceNode, targetGameObject, targetNode);

                connectionCreated = true;
                BindingChanged?.Invoke();

                return true;
            }
            finally
            {
                graphState.TryResetConnectionCreation(EConnectionCreationType.Binding);
                connectionRenderer.MarkDirty();

                if (!connectionCreated)
                {
                    Debug.Log("Tutorial binding creation cancelled.");
                }
            }
        }

        /// <summary>
        /// Cancel the current binding creation
        /// </summary>
        public void CancelConnection()
        {
            if (!graphState.TryResetConnectionCreation(EConnectionCreationType.Binding))
            {
                return;
            }

            connectionRenderer.MarkDirty();
        }

        #endregion

        #region Connection Restoration

        /// <summary>
        /// Restore a visual binding without modifying the persistent StepSO data
        /// </summary>
        /// <param name="bindingData"></param>
        /// <param name="runtimeRegistry"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryRestoreConnection(TutorialResolvedBindingData bindingData, TutorialGraphRuntimeRegistry runtimeRegistry, out string failureReason)
        {
            failureReason = string.Empty;

            if (bindingData == null)
            {
                failureReason = "The resolved binding data is missing.";

                return false;
            }

            if (runtimeRegistry == null)
            {
                failureReason = "The tutorial graph runtime registry is missing.";

                return false;
            }

            if (!runtimeRegistry.TryGetElement(bindingData.SourceNodeGuid, out VisualElement sourceNode))
            {
                failureReason = $"Unable to find binding source node '{bindingData.SourceNodeGuid}'.";

                return false;
            }

            if (!runtimeRegistry.TryGetElement(bindingData.TargetNodeGuid, out VisualElement targetNode))
            {
                failureReason = $"Unable to find binding target node '{bindingData.TargetNodeGuid}'.";

                return false;
            }

            if (sourceNode.userData is not StepSO sourceStep)
            {
                failureReason = $"Binding source node '{bindingData.SourceNodeGuid}' does not contain a StepSO.";

                return false;
            }

            if (targetNode.userData is not GameObject targetGameObject)
            {
                failureReason = $"Binding target node '{bindingData.TargetNodeGuid}' does not contain a GameObject.";

                return false;
            }

            if (!targetGameObject.TryGetComponent(out TutoIdentifier identifier))
            {
                failureReason = $"The GameObject '{targetGameObject.name}' does not contain a TutoIdentifier.";

                return false;
            }

            if (!string.Equals(sourceStep.TutoGUID, identifier.ObjectGUID, StringComparison.Ordinal))
            {
                failureReason = $"The persistent binding of StepSO '{sourceStep.name}' does not match GameObject '{targetGameObject.name}'.";

                return false;
            }

            if (HasVisualBinding(sourceStep))
            {
                failureReason = $"The StepSO '{sourceStep.name}' already has a restored visual binding.";

                return false;
            }

            VisualElement sourcePort = sourceNode.Q<VisualElement>(className: TutorialNodeFactory.BindingOutputPortClass);
            VisualElement targetPort = targetNode.Q<VisualElement>(className: TutorialNodeFactory.BindingInputPortClass);

            if (sourcePort == null)
            {
                failureReason = $"The binding output port of StepSO '{sourceStep.name}' could not be found.";

                return false;
            }

            if (targetPort == null)
            {
                failureReason = $"The binding input port of GameObject '{targetGameObject.name}' could not be found.";

                return false;
            }

            BindingConnection connection = new BindingConnection(sourceStep, sourceNode, sourcePort, targetNode, targetPort);

            if (!connection.IsValid)
            {
                failureReason = $"The restored binding between '{sourceStep.name}' and '{targetGameObject.name}' is invalid.";

                return false;
            }

            if (!graphState.AddBindingConnection(connection))
            {
                failureReason = $"Unable to register the restored binding between '{sourceStep.name}' and '{targetGameObject.name}'.";

                return false;
            }

            connectionRenderer.MarkDirty();

            return true;
        }

        /// <summary>
        /// Check whether a StepSO already has a visual binding
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private bool HasVisualBinding(StepSO step)
        {
            if (step == null)
            {
                return false;
            }

            foreach (BindingConnection connection in graphState.BindingConnections)
            {
                if (connection == null || !connection.IsValid)
                {
                    continue;
                }

                if (connection.Step == step)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove every visual binding without modifying persistent StepSO data
        /// </summary>
        /// <returns></returns>
        public int ClearVisualConnections()
        {
            int removedCount = graphState.RemoveBindingConnections(connection => true);

            if (removedCount > 0)
            {
                connectionRenderer.MarkDirty();
            }

            return removedCount;
        }

        #endregion

        #region Connection Deletion

        /// <summary>
        /// Delete a binding connection and clear its persistent StepSO data
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool DeleteConnection(BindingConnection connection)
        {
            if (connection == null || !connection.IsValid)
            {
                return false;
            }
            
            if (!ClearPersistentBinding(connection.Step)) 
            {
                return false;
            }

            if (!graphState.RemoveBindingConnection(connection))
            {
                return false;
            }

            connectionRenderer.MarkDirty();
            BindingChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Remove every visual binding associated with a node without modifying the StepSO data
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int RemoveVisualConnectionsForNode(VisualElement node)
        {
            if (node == null)
            {
                return 0;
            }

            int removedCount = graphState.RemoveBindingConnections(connection => connection.SourceNode == node || connection.TargetNode == node);

            if (removedCount > 0)
            {
                connectionRenderer.MarkDirty();
            }

            return removedCount;
        }

        /// <summary>
        /// Remove every visual binding associated with a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private int RemoveExistingVisualConnections(StepSO step)
        {
            if (step == null)
            {
                return 0;
            }

            return graphState.RemoveBindingConnections(connection => connection.Step == step);
        }

        #endregion

        #region Binding Queries

        /// <summary>
        /// Get every StepSO visually connected to a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public IReadOnlyList<StepSO> GetLinkedSteps(GameObject gameObject)
        {
            List<StepSO> linkedSteps = new List<StepSO>();
            HashSet<StepSO> registeredSteps = new HashSet<StepSO>();

            if (gameObject == null)
            {
                return linkedSteps;
            }

            foreach (BindingConnection connection in graphState.BindingConnections)
            {
                if (connection == null || !connection.IsValid || connection.TargetGameObject != gameObject)
                {
                    continue;
                }

                if (!registeredSteps.Add(connection.Step))
                {
                    continue;
                }

                linkedSteps.Add(connection.Step);
            }

            linkedSteps.Sort((first, second) => string.Compare(first.name, second.name, StringComparison.Ordinal));

            return linkedSteps;
        }

        /// <summary>
        /// Find the binding input port below the pointer
        /// </summary>
        /// <param name="pointerPosition"></param>
        /// <returns></returns>
        private VisualElement FindBindingInputPort(Vector2 pointerPosition)
        {
            if (canvas.panel == null || !canvas.worldBound.Contains(pointerPosition))
            {
                return null;
            }

            VisualElement pickedElement = canvas.panel.Pick(pointerPosition);

            while (pickedElement != null)
            {
                if (pickedElement.ClassListContains(TutorialNodeFactory.BindingInputPortClass)) return pickedElement;

                pickedElement = pickedElement.parent;
            }

            return null;
        }

        #endregion

        #region Persistent Binding

        /// <summary>
        /// Store the GameObject tutorial GUID inside a StepSO
        /// </summary>
        /// <param name="step"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private bool TrySetPersistentBinding(StepSO step, TutoIdentifier identifier)
        {
            if (step == null || identifier == null)
            {
                return false;
            }

            if (!guidService.TryPrepareBinding(step, identifier, out string objectGuid))
            {
                return false;
            }

            bool targetChanged = !string.Equals(step.TutoGUID, objectGuid, StringComparison.Ordinal);

            Undo.RecordObject(step, "Link tutorial StepSO to GameObject");

            step.TutoGUID = objectGuid;

            if (targetChanged)
            {
                step.ScriptName = string.Empty;
                step.MethodNameToCall = string.Empty;
            }

            EditorUtility.SetDirty(step);
            AssetDatabase.SaveAssetIfDirty(step);

            return true;
        }

        /// <summary>
        /// Clear every persistent value associated with a StepSO binding
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private static bool ClearPersistentBinding(StepSO step)
        {
            if (step == null)
            {
                return false;
            }

            Undo.RecordObject(step, "Delete tutorial binding");

            step.TutoGUID = string.Empty;
            step.ScriptName = string.Empty;
            step.MethodNameToCall = string.Empty;

            EditorUtility.SetDirty(step);
            AssetDatabase.SaveAssetIfDirty(step);

            return true;
        }

        #endregion

        #region Inspector

        /// <summary>
        /// Refresh the inspector when one of the connected nodes is currently selected
        /// </summary>
        /// <param name="sourceStep"></param>
        /// <param name="sourceNode"></param>
        /// <param name="targetGameObject"></param>
        /// <param name="targetNode"></param>
        private void RefreshSelectedInspector(StepSO sourceStep, VisualElement sourceNode, GameObject targetGameObject, VisualElement targetNode)
        {
            if (graphState.SelectedNode == sourceNode)
            {
                inspectorView.DisplayStep(sourceStep);
                return;
            }

            if (graphState.SelectedNode == targetNode)
            {
                inspectorView.DisplayGameObject(targetGameObject, GetLinkedSteps(targetGameObject));
            }
        }
        #endregion
    }
}