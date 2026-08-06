using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial.Editor.Manipulator
{
    /// <summary>
    /// Allow a visual graph node to be moved inside its canvas
    /// </summary>
    internal sealed class NodeDragManipulator : PointerManipulator
    {
        #region Private Fields

        /// <summary>
        /// Canvas containing the manipulated node
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Callback invoked continuously while the node is moving
        /// </summary>
        private readonly Action onMoved = null;

        /// <summary>
        /// Callback invoked once after a node position has effectively changed
        /// </summary>
        private readonly Action<VisualElement> onMoveCompleted = null;

        /// <summary>
        /// Pointer position when the drag started
        /// </summary>
        private Vector2 initialPointerPosition = Vector2.zero;

        /// <summary>
        /// Node position when the drag started
        /// </summary>
        private Vector2 initialNodePosition = Vector2.zero;

        /// <summary>
        /// Last position applied to the node
        /// </summary>
        private Vector2 currentNodePosition = Vector2.zero;

        /// <summary>
        /// Whether a node drag is currently active
        /// </summary>
        private bool isDragging = false;

        #endregion

        #region Constructor

        public NodeDragManipulator(VisualElement canvas, Action onMoved, Action<VisualElement> onMoveCompleted = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.onMoved = onMoved;
            this.onMoveCompleted = onMoveCompleted;
        }

        #endregion

        #region Pointer Manipulator

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        #endregion

        #region Pointer Events

        /// <summary>
        /// Start moving the manipulated node
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || target == null)
            {
                return;
            }

            initialPointerPosition = GetPointerPosition(pointerEvent);
            initialNodePosition = GetNodePosition();
            currentNodePosition = initialNodePosition;
            isDragging = true;

            target.CapturePointer(pointerEvent.pointerId);
            target.BringToFront();

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Update the manipulated node position
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (!isDragging || !target.HasPointerCapture(pointerEvent.pointerId))
            {
                return;
            }

            Vector2 currentPointerPosition = GetPointerPosition(pointerEvent);
            Vector2 pointerDelta = currentPointerPosition - initialPointerPosition;
            Vector2 newPosition = initialNodePosition + pointerDelta;

            float maximumX = Mathf.Max(0f, canvas.resolvedStyle.width - target.resolvedStyle.width);
            float maximumY = Mathf.Max(0f, canvas.resolvedStyle.height - target.resolvedStyle.height);

            newPosition.x = Mathf.Clamp(newPosition.x, 0f, maximumX);
            newPosition.y = Mathf.Clamp(newPosition.y, 0f, maximumY);

            if (ArePositionsEqual(currentNodePosition, newPosition))
            {
                pointerEvent.StopPropagation();

                return;
            }

            currentNodePosition = newPosition;

            target.style.left = currentNodePosition.x;
            target.style.top = currentNodePosition.y;

            onMoved?.Invoke();

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Complete the current node movement
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (!isDragging || pointerEvent.button != 0)
            {
                return;
            }

            CompleteDrag();

            if (target.HasPointerCapture(pointerEvent.pointerId))
            {
                target.ReleasePointer(pointerEvent.pointerId);
            }

            pointerEvent.StopPropagation();
        }

        /// <summary>
        /// Complete the movement when pointer capture is interrupted
        /// </summary>
        /// <param name="pointerEvent"></param>
        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            CompleteDrag();
        }

        #endregion

        #region Drag Completion

        /// <summary>
        /// Complete the current drag and notify an effective position change
        /// </summary>
        private void CompleteDrag()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;

            if (ArePositionsEqual(initialNodePosition, currentNodePosition))
            {
                return;
            }

            onMoveCompleted?.Invoke(target);
        }

        #endregion

        #region Position

        /// <summary>
        /// Get the pointer position inside the graph canvas
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <returns></returns>
        private Vector2 GetPointerPosition(IPointerEvent pointerEvent)
        {
            Vector2 worldPosition = new Vector2(pointerEvent.position.x, pointerEvent.position.y);

            return canvas.WorldToLocal(worldPosition);
        }

        /// <summary>
        /// Get the current node position
        /// </summary>
        /// <returns></returns>
        private Vector2 GetNodePosition()
        {
            float positionX = target.resolvedStyle.left;
            float positionY = target.resolvedStyle.top;

            if (float.IsNaN(positionX) || float.IsInfinity(positionX))
            {
                positionX = target.layout.x;
            }

            if (float.IsNaN(positionY) || float.IsInfinity(positionY))
            {
                positionY = target.layout.y;
            }

            return new Vector2(positionX, positionY);
        }

        /// <summary>
        /// Check whether two graph positions are equivalent
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static bool ArePositionsEqual(Vector2 first, Vector2 second)
        {
            return Mathf.Approximately(first.x, second.x) && Mathf.Approximately(first.y, second.y);
        }

        #endregion
    }
}