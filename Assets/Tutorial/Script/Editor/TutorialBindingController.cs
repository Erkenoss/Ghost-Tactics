using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial
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