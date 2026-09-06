using System;
using UnityEngine;
using UnityEngine.UIElements;

using Tutorial.Editor.Core;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Draw and detect the visual connections of the tutorial graph
    /// </summary>
    public sealed class TutorialConnectionRenderer : IDisposable
    {
        #region Constants

        private const float DefaultConnectionWidth = 3f;
        private const float SelectedConnectionWidth = 5f;
        private const float TemporaryConnectionWidth = 3f;
        private const float ConnectionSelectionDistance = 8f;
        private const float MinimumBezierTangentLength = 60f;
        private const int ConnectionHitTestSegments = 24;

        #endregion

        #region Colors

        private static readonly Color BindingConnectionColor = new Color(0.25f, 0.65f, 1f);
        private static readonly Color SequenceConnectionColor = new Color(0.65f, 0.4f, 0.9f);
        private static readonly Color SelectedConnectionColor = new Color(1f, 0.65f, 0.2f);
        private static readonly Color TemporaryBindingColor = new Color(0.25f, 0.65f, 1f, 0.8f);
        private static readonly Color TemporarySequenceColor = new Color(0.65f, 0.4f, 0.9f, 0.8f);

        #endregion

        #region Private Fields

        /// <summary>
        /// Main tutorial graph canvas
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Layer used to draw the tutorial graph connections
        /// </summary>
        private readonly VisualElement connectionLayer = null;

        /// <summary>
        /// Temporary state of the tutorial graph
        /// </summary>
        private readonly TutorialGraphState graphState = null;

        /// <summary>
        /// Whether the renderer is currently registered
        /// </summary>
        private bool isEnabled = false;

        #endregion

        #region Constructor

        public TutorialConnectionRenderer(VisualElement canvas, VisualElement connectionLayer, TutorialGraphState graphState)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.connectionLayer = connectionLayer ?? throw new ArgumentNullException(nameof(connectionLayer));
            this.graphState = graphState ?? throw new ArgumentNullException(nameof(graphState));
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Register the connection drawing callback
        /// </summary>
        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            connectionLayer.generateVisualContent += DrawConnections;

            isEnabled = true;

            MarkDirty();
        }

        /// <summary>
        /// Unregister the connection drawing callback
        /// </summary>
        public void Dispose()
        {
            if (!isEnabled)
            {
                return;
            }

            connectionLayer.generateVisualContent -= DrawConnections;

            isEnabled = false;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draw every permanent and temporary graph connection
        /// </summary>
        /// <param name="context"></param>
        private void DrawConnections(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;

            DrawBindingConnections(painter);
            DrawSequenceConnections(painter);
            DrawTemporaryConnection(painter);
        }

        /// <summary>
        /// Draw every StepSO to GameObject binding
        /// </summary>
        /// <param name="painter"></param>
        private void DrawBindingConnections(Painter2D painter)
        {
            foreach (BindingConnection connection in graphState.BindingConnections)
            {
                if (!TryGetConnectionCurve(connection, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end))
                {
                    continue;
                }

                bool isSelected = connection == graphState.SelectedBindingConnection;
                Color color = isSelected ? SelectedConnectionColor : BindingConnectionColor;
                float width = isSelected ? SelectedConnectionWidth : DefaultConnectionWidth;

                DrawBezier(painter, start, startTangent, endTangent, end, color, width);
            }
        }

        /// <summary>
        /// Draw every StepSO sequence connection
        /// </summary>
        /// <param name="painter"></param>
        private void DrawSequenceConnections(Painter2D painter)
        {
            foreach (SequenceConnection connection in graphState.SequenceConnections)
            {
                if (!TryGetConnectionCurve(connection, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end))
                {
                    continue;
                }

                bool isSelected = connection == graphState.SelectedSequenceConnection;
                Color color = isSelected ? SelectedConnectionColor : SequenceConnectionColor;
                float width = isSelected ? SelectedConnectionWidth : DefaultConnectionWidth;

                DrawBezier(painter, start, startTangent, endTangent, end, color, width);
            }
        }

        /// <summary>
        /// Draw the connection currently being created
        /// </summary>
        /// <param name="painter"></param>
        private void DrawTemporaryConnection(Painter2D painter)
        {
            if (!graphState.IsCreatingConnection)
            {
                return;
            }

            VisualElement sourcePort = graphState.ConnectionSourcePort;

            if (sourcePort == null || sourcePort.panel == null || canvas.panel == null || connectionLayer.panel == null)
            {
                return;
            }

            Vector2 start = connectionLayer.WorldToLocal(sourcePort.worldBound.center);
            Vector2 pointerWorldPosition = canvas.LocalToWorld(graphState.ConnectionPointerPosition);
            Vector2 end = connectionLayer.WorldToLocal(pointerWorldPosition);

            GetBezierTangents(start, end, out Vector2 startTangent, out Vector2 endTangent);

            Color color;

            switch (graphState.ActiveConnectionType)
            {
                case EConnectionCreationType.Binding:
                    color = TemporaryBindingColor;
                    break;

                case EConnectionCreationType.Sequence:
                    color = TemporarySequenceColor;
                    break;

                default:
                    return;
            }

            DrawBezier(painter, start, startTangent, endTangent, end, color, TemporaryConnectionWidth);
        }

        /// <summary>
        /// Draw a Bezier connection
        /// </summary>
        /// <param name="painter"></param>
        /// <param name="start"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        /// <param name="end"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        private static void DrawBezier(Painter2D painter, Vector2 start, Vector2 startTangent, Vector2 endTangent, Vector2 end, Color color, float width)
        {
            painter.lineWidth = width;
            painter.strokeColor = color;

            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(startTangent, endTangent, end);
            painter.Stroke();
        }

        #endregion

        #region Binding Hit Test

        /// <summary>
        /// Find the binding connection located near a connection-layer position
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public BindingConnection FindBindingConnectionAt(Vector2 localPosition)
        {
            BindingConnection closestConnection = null;
            float closestDistance = ConnectionSelectionDistance;

            for (int i = graphState.BindingConnections.Count - 1; i >= 0; i--)
            {
                BindingConnection connection = graphState.BindingConnections[i];

                if (!TryGetConnectionCurve(connection, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end))
                {
                    continue;
                }

                float distance = GetDistanceToBezier(localPosition, start, startTangent, endTangent, end);

                if (distance > closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closestConnection = connection;
            }

            return closestConnection;
        }

        #endregion

        #region Sequence Hit Test

        /// <summary>
        /// Find the sequence connection located near a connection-layer position
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public SequenceConnection FindSequenceConnectionAt(Vector2 localPosition)
        {
            SequenceConnection closestConnection = null;
            float closestDistance = ConnectionSelectionDistance;

            for (int i = graphState.SequenceConnections.Count - 1; i >= 0; i--)
            {
                SequenceConnection connection = graphState.SequenceConnections[i];

                if (!TryGetConnectionCurve(connection, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end))
                {
                    continue;
                }

                float distance = GetDistanceToBezier(localPosition, start, startTangent, endTangent, end);

                if (distance > closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closestConnection = connection;
            }

            return closestConnection;
        }

        #endregion

        #region Connection Curves

        /// <summary>
        /// Get the curve of a binding connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="start"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool TryGetConnectionCurve(BindingConnection connection, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end)
        {
            start = Vector2.zero;
            startTangent = Vector2.zero;
            endTangent = Vector2.zero;
            end = Vector2.zero;

            if (connection == null || !connection.IsValid)
            {
                return false;
            }

            return TryGetConnectionCurve(connection.SourcePort, connection.TargetPort, out start, out startTangent, out endTangent, out end);
        }

        /// <summary>
        /// Get the curve of a sequence connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="start"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool TryGetConnectionCurve(SequenceConnection connection, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end)
        {
            start = Vector2.zero;
            startTangent = Vector2.zero;
            endTangent = Vector2.zero;
            end = Vector2.zero;

            if (connection == null || !connection.IsValid)
            {
                return false;
            }

            return TryGetConnectionCurve(connection.SourcePort, connection.TargetPort, out start, out startTangent, out endTangent, out end);
        }

        /// <summary>
        /// Get a Bezier curve from two connection ports
        /// </summary>
        /// <param name="sourcePort"></param>
        /// <param name="targetPort"></param>
        /// <param name="start"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool TryGetConnectionCurve(VisualElement sourcePort, VisualElement targetPort, out Vector2 start, out Vector2 startTangent, out Vector2 endTangent, out Vector2 end)
        {
            start = Vector2.zero;
            startTangent = Vector2.zero;
            endTangent = Vector2.zero;
            end = Vector2.zero;

            if (connectionLayer.panel == null || sourcePort == null || targetPort == null)
            {
                return false;
            }

            if (sourcePort.panel == null || targetPort.panel == null)
            {
                return false;
            }

            start = connectionLayer.WorldToLocal(sourcePort.worldBound.center);
            end = connectionLayer.WorldToLocal(targetPort.worldBound.center);

            GetBezierTangents(start, end, out startTangent, out endTangent);

            return true;
        }

        /// <summary>
        /// Calculate the control points of a connection curve
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        private static void GetBezierTangents(Vector2 start, Vector2 end, out Vector2 startTangent, out Vector2 endTangent)
        {
            float horizontalDistance = Mathf.Abs(end.x - start.x);
            float tangentLength = Mathf.Max(horizontalDistance * 0.5f, MinimumBezierTangentLength);

            startTangent = start + Vector2.right * tangentLength;
            endTangent = end + Vector2.left * tangentLength;
        }

        #endregion

        #region Bezier Hit Test

        /// <summary>
        /// Calculate the shortest sampled distance between a point and a Bezier curve
        /// </summary>
        /// <param name="point"></param>
        /// <param name="start"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static float GetDistanceToBezier(Vector2 point, Vector2 start, Vector2 startTangent, Vector2 endTangent, Vector2 end)
        {
            float closestDistance = float.MaxValue;
            Vector2 previousPoint = start;

            for (int i = 1; i <= ConnectionHitTestSegments; i++)
            {
                float time = i / (float)ConnectionHitTestSegments;
                Vector2 currentPoint = EvaluateBezier(start, startTangent, endTangent, end, time);
                float distance = GetDistanceToSegment(point, previousPoint, currentPoint);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }

                previousPoint = currentPoint;
            }

            return closestDistance;
        }

        /// <summary>
        /// Evaluate a cubic Bezier curve
        /// </summary>
        /// <param name="start"></param>
        /// <param name="startTangent"></param>
        /// <param name="endTangent"></param>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static Vector2 EvaluateBezier(Vector2 start, Vector2 startTangent, Vector2 endTangent, Vector2 end, float time)
        {
            float inverseTime = 1f - time;
            float inverseTimeSquared = inverseTime * inverseTime;
            float timeSquared = time * time;

            return inverseTimeSquared * inverseTime * start +
                   3f * inverseTimeSquared * time * startTangent +
                   3f * inverseTime * timeSquared * endTangent +
                   timeSquared * time * end;
        }

        /// <summary>
        /// Calculate the distance between a point and a line segment
        /// </summary>
        /// <param name="point"></param>
        /// <param name="segmentStart"></param>
        /// <param name="segmentEnd"></param>
        /// <returns></returns>
        private static float GetDistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float segmentSquaredLength = segment.sqrMagnitude;

            if (segmentSquaredLength <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, segmentStart);
            }

            float projection = Vector2.Dot(point - segmentStart, segment) / segmentSquaredLength;
            projection = Mathf.Clamp01(projection);

            Vector2 projectedPoint = segmentStart + segment * projection;

            return Vector2.Distance(point, projectedPoint);
        }

        #endregion

        #region Repaint

        /// <summary>
        /// Force the connection layer to repaint
        /// </summary>
        public void MarkDirty()
        {
            connectionLayer.MarkDirtyRepaint();
        }

        #endregion
    }
}