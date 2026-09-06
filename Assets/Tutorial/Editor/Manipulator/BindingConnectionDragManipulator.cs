using System;
using UnityEngine;
using UnityEngine.UIElements;

using Tutorial.Runtime.Data;
using Tutorial.Editor.Controllers;

namespace Tutorial.Editor.Manipulator
{
    /// <summary>
    /// Allow a binding connection to be dragged from a StepSO output port
    /// </summary>
    internal sealed class BindingConnectionDragManipulator : PointerManipulator
    {
        #region Private Fields

        /// <summary>
        /// Controller responsible for StepSO to GameObject bindings
        /// </summary>
        private readonly TutorialBindingController bindingController = null;

        /// <summary>
        /// Visual node representing the source StepSO
        /// </summary>
        private readonly VisualElement sourceNode = null;

        /// <summary>
        /// Binding output port of the source node
        /// </summary>
        private readonly VisualElement sourcePort = null;

        /// <summary>
        /// StepSO associated with the source node
        /// </summary>
        private readonly StepSO step = null;

        /// <summary>
        /// Identifier of the pointer currently creating the connection
        /// </summary>
        private int activePointerId = -1;

        /// <summary>
        /// Whether a binding connection is currently being dragged
        /// </summary>
        private bool isDragging = false;

        #endregion

        #region Constructor

        public BindingConnectionDragManipulator(TutorialBindingController bindingController, VisualElement sourceNode, VisualElement sourcePort, StepSO step)
        {
            this.bindingController = bindingController ?? throw new ArgumentNullException(nameof(bindingController));
            this.sourceNode = sourceNode ?? throw new ArgumentNullException(nameof(sourceNode));
            this.sourcePort = sourcePort ?? throw new ArgumentNullException(nameof(sourcePort));
            this.step = step != null ? step : throw new ArgumentNullException(nameof(step));
        }

        #endregion

        #region Pointer Manipulator

        /// <summary>
        /// Register pointer callbacks on the binding output port
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        /// <summary>
        /// Unregister pointer callbacks from the binding output port
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            if (isDragging)
            {
                CancelDrag();
            }

            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        #endregion

        #region Pointer Callbacks

        /// <summary>
        /// Start creating a binding connection
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || isDragging)
            {
                return;
            }

            Vector2 pointerPosition = GetPointerPosition(pointerEvent);

            if (!bindingController.BeginConnection(step, sourceNode, sourcePort, pointerPosition))
            {
                return;
            }

            activePointerId = pointerEvent.pointerId;
            isDragging = true;

            target.CapturePointer(activePointerId);

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Update the temporary binding connection
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (!isDragging || pointerEvent.pointerId != activePointerId)
            {
                return;
            }

            if (!target.HasPointerCapture(activePointerId))
            {
                CancelDrag();

                return;
            }

            bindingController.UpdateConnection(GetPointerPosition(pointerEvent));

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Complete the binding connection
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (!isDragging || pointerEvent.pointerId != activePointerId || pointerEvent.button != 0)
            {
                return;
            }

            Vector2 pointerPosition = GetPointerPosition(pointerEvent);
            int pointerId = activePointerId;

            ResetDragState();

            bindingController.EndConnection(pointerPosition);

            if (target.HasPointerCapture(pointerId))
            {
                target.ReleasePointer(pointerId);
            }

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Cancel the binding connection when pointer capture is lost
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (!isDragging || pointerEvent.pointerId != activePointerId)
            {
                return;
            }

            ResetDragState();
            bindingController.CancelConnection();
        }

        #endregion

        #region Pointer Position

        /// <summary>
        /// Get the pointer position in world coordinates
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <returns></returns>
        private static Vector2 GetPointerPosition(IPointerEvent pointerEvent)
        {
            return new Vector2(pointerEvent.position.x, pointerEvent.position.y);
        }

        #endregion

        #region Drag State

        /// <summary>
        /// Cancel the current binding drag
        /// </summary>
        private void CancelDrag()
        {
            int pointerId = activePointerId;

            ResetDragState();
            bindingController.CancelConnection();

            if (target != null && pointerId >= 0 && target.HasPointerCapture(pointerId))
            {
                target.ReleasePointer(pointerId);
            }
        }

        /// <summary>
        /// Reset the temporary drag state
        /// </summary>
        private void ResetDragState()
        {
            isDragging = false;
            activePointerId = -1;
        }

        #endregion
    }
}