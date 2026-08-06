using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial
{
    /// <summary>
    /// Allow a tutorial graph node to be moved inside its canvas
    /// </summary>
    internal sealed class NodeDragManipulator : PointerManipulator
    {
        #region Private Fields

        /// <summary>
        /// Canvas containing the manipulated node
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Callback invoked whenever the node position changes
        /// </summary>
        private readonly Action onMoved = null;

        /// <summary>
        /// Pointer position when the drag started
        /// </summary>
        private Vector2 initialPointerPosition = Vector2.zero;

        /// <summary>
        /// Node position when the drag started
        /// </summary>
        private Vector2 initialNodePosition = Vector2.zero;

        /// <summary>
        /// Identifier of the pointer currently dragging the node
        /// </summary>
        private int activePointerId = -1;

        /// <summary>
        /// Whether the node is currently being dragged
        /// </summary>
        private bool isDragging = false;

        #endregion

        #region Constructor

        public NodeDragManipulator(VisualElement canvas, Action onMoved)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.onMoved = onMoved;
        }

        #endregion

        #region Pointer Manipulator

        /// <summary>
        /// Register pointer callbacks on the manipulated node
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        /// <summary>
        /// Unregister pointer callbacks from the manipulated node
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        #endregion

        #region Pointer Callbacks

        /// <summary>
        /// Start dragging the node
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || isDragging)
            {
                return;
            }

            if (target == null || canvas == null)
            {
                return;
            }

            initialPointerPosition = GetPointerPosition(pointerEvent);
            initialNodePosition = GetNodePosition();

            activePointerId = pointerEvent.pointerId;
            isDragging = true;

            target.CapturePointer(activePointerId);
            target.BringToFront();

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Update the node position during the drag
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

            Vector2 currentPointerPosition = GetPointerPosition(pointerEvent);
            Vector2 pointerDelta = currentPointerPosition - initialPointerPosition;
            Vector2 newPosition = initialNodePosition + pointerDelta;

            newPosition = ClampPositionInsideCanvas(newPosition);

            target.style.left = newPosition.x;
            target.style.top = newPosition.y;

            onMoved?.Invoke();

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Complete the node drag
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (!isDragging || pointerEvent.pointerId != activePointerId || pointerEvent.button != 0)
            {
                return;
            }

            ReleasePointer();
            ResetDragState();

            onMoved?.Invoke();

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Cancel the drag when the pointer capture is lost
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (!isDragging || pointerEvent.pointerId != activePointerId)
            {
                return;
            }

            ResetDragState();
            onMoved?.Invoke();
        }

        #endregion

        #region Position

        /// <summary>
        /// Get the current node position inside the canvas
        /// </summary>
        /// <returns></returns>
        private Vector2 GetNodePosition()
        {
            float nodeX = target.resolvedStyle.left;
            float nodeY = target.resolvedStyle.top;

            if (float.IsNaN(nodeX))
            {
                nodeX = target.layout.x;
            }

            if (float.IsNaN(nodeY))
            {
                nodeY = target.layout.y;
            }

            return new Vector2(nodeX, nodeY);
        }

        /// <summary>
        /// Get a pointer position inside the canvas coordinate system
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <returns></returns>
        private Vector2 GetPointerPosition(IPointerEvent pointerEvent)
        {
            Vector2 worldPosition = new Vector2(pointerEvent.position.x, pointerEvent.position.y);

            return canvas.WorldToLocal(worldPosition);
        }

        /// <summary>
        /// Keep the node inside the visible canvas area
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 ClampPositionInsideCanvas(Vector2 position)
        {
            float canvasWidth = canvas.contentRect.width;
            float canvasHeight = canvas.contentRect.height;
            float nodeWidth = target.resolvedStyle.width;
            float nodeHeight = target.resolvedStyle.height;

            if (float.IsNaN(nodeWidth) || nodeWidth <= 0f)
            {
                nodeWidth = target.layout.width;
            }

            if (float.IsNaN(nodeHeight) || nodeHeight <= 0f)
            {
                nodeHeight = target.layout.height;
            }

            float maximumX = Mathf.Max(0f, canvasWidth - nodeWidth);
            float maximumY = Mathf.Max(0f, canvasHeight - nodeHeight);

            position.x = Mathf.Clamp(position.x, 0f, maximumX);
            position.y = Mathf.Clamp(position.y, 0f, maximumY);

            return position;
        }

        #endregion

        #region Drag State

        /// <summary>
        /// Cancel the current drag
        /// </summary>
        private void CancelDrag()
        {
            ReleasePointer();
            ResetDragState();

            onMoved?.Invoke();
        }

        /// <summary>
        /// Release the currently captured pointer
        /// </summary>
        private void ReleasePointer()
        {
            if (target == null || activePointerId < 0)
            {
                return;
            }

            if (target.HasPointerCapture(activePointerId))
            {
                target.ReleasePointer(activePointerId);
            }
        }

        /// <summary>
        /// Reset the temporary drag values
        /// </summary>
        private void ResetDragState()
        {
            isDragging = false;
            activePointerId = -1;
            initialPointerPosition = Vector2.zero;
            initialNodePosition = Vector2.zero;
        }

        #endregion
    }
}