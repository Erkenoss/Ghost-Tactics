using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

using Tutorial.Runtime.Data;
using Tutorial.Runtime.Components;

using Tutorial.Editor.Controllers;
using Tutorial.Editor.Manipulator;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Create and configure the visual nodes and ports of the tutorial graph
    /// </summary>
    internal sealed class TutorialNodeFactory
    {
        #region Constants

        /// <summary>
        /// USS class applied to every tutorial node
        /// </summary>
        internal const string NodeClass = "tutorial-node";

        /// <summary>
        /// USS class applied to StepSO binding output ports
        /// </summary>
        internal const string BindingOutputPortClass = "tutorial-binding-output-port";

        /// <summary>
        /// USS class applied to GameObject binding input ports
        /// </summary>
        internal const string BindingInputPortClass = "tutorial-binding-input-port";

        /// <summary>
        /// USS class applied to StepSO sequence input ports
        /// </summary>
        internal const string SequenceInputPortClass = "tutorial-sequence-input-port";

        /// <summary>
        /// USS class applied to StepSO sequence output ports
        /// </summary>
        internal const string SequenceOutputPortClass = "tutorial-sequence-output-port";

        /// <summary>
        /// Width of a tutorial graph node
        /// </summary>
        private const float NodeWidth = 180f;

        /// <summary>
        /// Height of a tutorial graph node
        /// </summary>
        private const float NodeHeight = 72f;

        /// <summary>
        /// Diameter of a tutorial graph port
        /// </summary>
        private const float PortSize = 16f;

        /// <summary>
        /// Distance between a port and the closest node border
        /// </summary>
        private const float PortVerticalMargin = 6f;

        #endregion

        #region Colors

        /// <summary>
        /// Default node background color
        /// </summary>
        private static readonly Color NodeBackgroundColor = new Color(0.22f, 0.22f, 0.22f);

        /// <summary>
        /// Default node border color
        /// </summary>
        private static readonly Color NodeBorderColor = new Color(0.45f, 0.45f, 0.45f);

        /// <summary>
        /// Selected node border color
        /// </summary>
        private static readonly Color SelectedNodeBorderColor = new Color(0.25f, 0.65f, 1f);

        /// <summary>
        /// StepSO to GameObject binding color
        /// </summary>
        private static readonly Color BindingOutputColor = new Color(0.25f, 0.65f, 1f);

        /// <summary>
        /// GameObject binding input color
        /// </summary>
        private static readonly Color BindingInputColor = new Color(0.3f, 0.85f, 0.45f);

        /// <summary>
        /// StepSO sequence connection color
        /// </summary>
        private static readonly Color SequencePortColor = new Color(0.65f, 0.4f, 0.9f);

        /// <summary>
        /// Port border color
        /// </summary>
        private static readonly Color PortBorderColor = new Color(0.85f, 0.85f, 0.85f);

        #endregion

        #region Private Fields

        /// <summary>
        /// Main tutorial graph canvas
        /// </summary>
        private readonly VisualElement canvas = null;

        /// <summary>
        /// Controller responsible for StepSO to GameObject bindings
        /// </summary>
        private readonly TutorialBindingController bindingController = null;

        /// <summary>
        /// Controller responsible for StepSO sequence connections
        /// </summary>
        private readonly TutorialSequenceController sequenceController = null;

        /// <summary>
        /// Renderer responsible for graph connection drawing
        /// </summary>
        private readonly TutorialConnectionRenderer connectionRenderer = null;

        #endregion

        #region Constructor

        public TutorialNodeFactory(VisualElement canvas, TutorialBindingController bindingController, TutorialSequenceController sequenceController, TutorialConnectionRenderer connectionRenderer)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.bindingController = bindingController ?? throw new ArgumentNullException(nameof(bindingController));
            this.sequenceController = sequenceController ?? throw new ArgumentNullException(nameof(sequenceController));
            this.connectionRenderer = connectionRenderer ?? throw new ArgumentNullException(nameof(connectionRenderer));
        }

        #endregion

        #region Node Creation

        /// <summary>
        /// Create a new visual graph node from a canvas drop position
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dropPosition"></param>
        /// <param name="onClicked"></param>
        /// <returns></returns>
        public VisualElement CreateNode(UnityObject target, Vector2 dropPosition, Action<VisualElement, UnityObject> onClicked, Action<VisualElement> onMoveCompleted = null)
        {
            Vector2 nodePosition = CalculateNodePosition(dropPosition);

            return CreateNodeAtPosition(target, nodePosition, onClicked, onMoveCompleted);
        }

        /// <summary>
        /// Create a visual graph node at an exact canvas position
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <param name="onClicked"></param>
        /// <returns></returns>
        public VisualElement CreateNodeAtPosition(UnityObject target, Vector2 position, Action<VisualElement, UnityObject> onClicked, Action<VisualElement> onMoveCompleted = null)
        {
            if (!IsSupportedTarget(target))
            {
                return null;
            }

            VisualElement node = CreateNodeContainer(target, position);

            node.Add(CreateNodeNameLabel(target));

            if (target is StepSO step)
            {
                CreateStepPorts(node, step);
            }
            else if (target is GameObject gameObject)
            {
                CreateGameObjectPorts(node, gameObject);
            }

            RegisterNodeInteractions(node, target, onClicked, onMoveCompleted);

            canvas.Add(node);

            return node;
        }

        /// <summary>
        /// Calculate the position of a newly dropped graph node
        /// </summary>
        /// <param name="requestedPosition"></param>
        /// <returns></returns>
        public Vector2 CalculateNewNodePosition(Vector2 requestedPosition)
        {
            return CalculateNodePosition(requestedPosition);
        }

        /// <summary>
        /// Create the main visual container of a graph node
        /// </summary>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private VisualElement CreateNodeContainer(UnityObject target, Vector2 position)
        {
            VisualElement node = new VisualElement
            {
                name = $"tutorial-node-{Guid.NewGuid():N}",
                userData = target,
                focusable = true
            };

            node.AddToClassList(NodeClass);

            node.style.position = Position.Absolute;
            node.style.left = position.x;
            node.style.top = position.y;
            node.style.width = NodeWidth;
            node.style.height = NodeHeight;
            node.style.overflow = Overflow.Visible;
            node.style.backgroundColor = NodeBackgroundColor;

            node.style.borderLeftWidth = 1f;
            node.style.borderRightWidth = 1f;
            node.style.borderTopWidth = 1f;
            node.style.borderBottomWidth = 1f;

            SetNodeBorderColor(node, NodeBorderColor);

            node.style.borderTopLeftRadius = 4f;
            node.style.borderTopRightRadius = 4f;
            node.style.borderBottomLeftRadius = 4f;
            node.style.borderBottomRightRadius = 4f;

            return node;
        }

        /// <summary>
        /// Create the name displayed inside a graph node
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static Label CreateNodeNameLabel(UnityObject target)
        {
            Label nameLabel = new Label(target.name)
            {
                name = "tutorial-node-name",
                pickingMode = PickingMode.Ignore
            };

            nameLabel.style.flexGrow = 1f;
            nameLabel.style.paddingLeft = 12f;
            nameLabel.style.paddingRight = 12f;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.whiteSpace = WhiteSpace.Normal;
            nameLabel.style.color = Color.white;

            return nameLabel;
        }

        /// <summary>
        /// Register the interactions attached to a graph node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="target"></param>
        /// <param name="onClicked"></param>
        /// <param name="onMoveCompleted"></param>
        private void RegisterNodeInteractions(VisualElement node, UnityObject target, Action<VisualElement, UnityObject> onClicked, Action<VisualElement> onMoveCompleted)
        {
            if (node == null || target == null)
            {
                return;
            }

            node.RegisterCallback<ClickEvent>(clickEvent =>
            {
                onClicked?.Invoke(node, target);
                clickEvent.StopPropagation();
            });

            node.AddManipulator(new NodeDragManipulator(canvas, connectionRenderer.MarkDirty, onMoveCompleted));
        }

        #endregion

        #region Step Ports

        /// <summary>
        /// Create every port available on a StepSO node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="step"></param>
        private void CreateStepPorts(VisualElement node, StepSO step)
        {
            if (node == null || step == null)
            {
                return;
            }

            CreateBindingOutputPort(node, step);
            CreateSequenceInputPort(node);
            CreateSequenceOutputPort(node, step);
        }

        /// <summary>
        /// Create the StepSO to GameObject binding output port
        /// </summary>
        /// <param name="node"></param>
        /// <param name="step"></param>
        private void CreateBindingOutputPort(VisualElement node, StepSO step)
        {
            VisualElement port = CreatePort("tutorial-binding-output-port", "Drag to a GameObject containing a TutoIdentifier", BindingOutputColor);

            port.userData = node;
            port.AddToClassList(BindingOutputPortClass);

            port.style.right = -PortSize * 0.5f;
            port.style.top = NodeHeight - PortSize - PortVerticalMargin;

            port.AddManipulator(new BindingConnectionDragManipulator(bindingController, node, port, step));

            node.Add(port);
        }

        /// <summary>
        /// Create the StepSO sequence input port
        /// </summary>
        /// <param name="node"></param>
        private static void CreateSequenceInputPort(VisualElement node)
        {
            VisualElement port = CreatePort("tutorial-sequence-input-port", "Previous StepSO", SequencePortColor);

            port.userData = node;
            port.AddToClassList(SequenceInputPortClass);

            port.style.left = -PortSize * 0.5f;
            port.style.top = PortVerticalMargin;

            node.Add(port);
        }

        /// <summary>
        /// Create the StepSO sequence output port
        /// </summary>
        /// <param name="node"></param>
        /// <param name="step"></param>
        private void CreateSequenceOutputPort(VisualElement node, StepSO step)
        {
            VisualElement port = CreatePort("tutorial-sequence-output-port", "Drag to the next StepSO", SequencePortColor);

            port.userData = node;
            port.AddToClassList(SequenceOutputPortClass);

            port.style.right = -PortSize * 0.5f;
            port.style.top = PortVerticalMargin;

            port.AddManipulator(new SequenceConnectionDragManipulator(sequenceController, node, port, step));

            node.Add(port);
        }

        #endregion

        #region GameObject Ports

        /// <summary>
        /// Create every port available on a GameObject node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="gameObject"></param>
        private static void CreateGameObjectPorts(VisualElement node, GameObject gameObject)
        {
            if (node == null || gameObject == null)
            {
                return;
            }

            if (!gameObject.TryGetComponent(out TutoIdentifier _))
            {
                return;
            }

            CreateBindingInputPort(node);
        }

        /// <summary>
        /// Create the GameObject binding input port
        /// </summary>
        /// <param name="node"></param>
        private static void CreateBindingInputPort(VisualElement node)
        {
            VisualElement port = CreatePort("tutorial-binding-input-port", "TutoIdentifier input", BindingInputColor);

            port.userData = node;
            port.AddToClassList(BindingInputPortClass);

            port.style.left = -PortSize * 0.5f;
            port.style.top = (NodeHeight - PortSize) * 0.5f;

            node.Add(port);
        }

        #endregion

        #region Port Creation

        /// <summary>
        /// Create and configure a generic graph connection port
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="tooltip"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        private static VisualElement CreatePort(string portName, string tooltip, Color backgroundColor)
        {
            VisualElement port = new VisualElement
            {
                name = portName,
                tooltip = tooltip
            };

            port.style.position = Position.Absolute;
            port.style.width = PortSize;
            port.style.height = PortSize;
            port.style.backgroundColor = backgroundColor;

            port.style.borderTopLeftRadius = PortSize;
            port.style.borderTopRightRadius = PortSize;
            port.style.borderBottomLeftRadius = PortSize;
            port.style.borderBottomRightRadius = PortSize;

            port.style.borderLeftWidth = 2f;
            port.style.borderRightWidth = 2f;
            port.style.borderTopWidth = 2f;
            port.style.borderBottomWidth = 2f;

            port.style.borderLeftColor = PortBorderColor;
            port.style.borderRightColor = PortBorderColor;
            port.style.borderTopColor = PortBorderColor;
            port.style.borderBottomColor = PortBorderColor;

            return port;
        }

        #endregion

        #region Node Selection

        /// <summary>
        /// Update the visual selection state of a graph node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isSelected"></param>
        public void SetNodeSelected(VisualElement node, bool isSelected)
        {
            if (node == null)
            {
                return;
            }

            SetNodeBorderColor(node, isSelected ? SelectedNodeBorderColor : NodeBorderColor);
        }

        /// <summary>
        /// Apply the same border color to every side of a node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="color"></param>
        private static void SetNodeBorderColor(VisualElement node, Color color)
        {
            if (node == null)
            {
                return;
            }

            node.style.borderLeftColor = color;
            node.style.borderRightColor = color;
            node.style.borderTopColor = color;
            node.style.borderBottomColor = color;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Check whether the canvas currently contains at least one graph node
        /// </summary>
        /// <returns></returns>
        public bool HasAnyNode()
        {
            foreach (VisualElement child in canvas.Children())
            {
                if (child.ClassListContains(NodeClass))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether a Unity object can be represented by a tutorial graph node
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static bool IsSupportedTarget(UnityObject target)
        {
            if (target is StepSO)
            {
                return true;
            }

            if (target is not GameObject gameObject)
            {
                return false;
            }

            return gameObject.TryGetComponent(out TutoIdentifier _);
        }

        #endregion

        #region Position

        /// <summary>
        /// Calculate the initial position of a new graph node
        /// </summary>
        /// <param name="dropPosition"></param>
        /// <returns></returns>
        private Vector2 CalculateNodePosition(Vector2 dropPosition)
        {
            Vector2 position = new Vector2(dropPosition.x - NodeWidth * 0.5f, dropPosition.y - NodeHeight * 0.5f);

            position.x = Mathf.Max(0f, position.x);
            position.y = Mathf.Max(0f, position.y);

            float canvasWidth = canvas.resolvedStyle.width;
            float canvasHeight = canvas.resolvedStyle.height;

            if (canvasWidth > NodeWidth)
            {
                position.x = Mathf.Min(position.x, canvasWidth - NodeWidth);
            }

            if (canvasHeight > NodeHeight)
            {
                position.y = Mathf.Min(position.y, canvasHeight - NodeHeight);
            }

            return position;
        }

        #endregion
    }
}