using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial
{
    public enum EConnectionCreationType
    {
        None,
        Binding,
        Sequence
    }

    public sealed class TutorialTool : EditorWindow
    {
        #region Constants

        private const string WindowMenuPath =
            "Window/Tutorial/Tutorial Tool";

        private const string WindowTitle =
            "Tutorial Tool";

        private const float InspectorWidth =
            320f;

        private static readonly Vector2 MinimumWindowSize =
            new Vector2(900f, 520f);

        #endregion

        #region UI Fields

        /// <summary>
        /// Main area containing the tutorial graph nodes
        /// </summary>
        private VisualElement canvas = null;

        /// <summary>
        /// Layer responsible for displaying graph connections
        /// </summary>
        private VisualElement connectionLayer = null;

        /// <summary>
        /// Message displayed when the canvas contains no node
        /// </summary>
        private Label dropHint = null;

        /// <summary>
        /// Right panel displaying the selected element inspector
        /// </summary>
        private ScrollView inspectorPanel = null;

        #endregion

        #region Tool State

        /// <summary>
        /// Temporary state of the currently edited tutorial graph
        /// </summary>
        private TutorialGraphState graphState = null;

        #endregion

        #region Services

        /// <summary>
        /// Service responsible for tutorial GUID operations
        /// </summary>
        private TutorialGuidService guidService = null;

        /// <summary>
        /// Service responsible for script and method binding
        /// </summary>
        private TutorialMethodBindingService methodBindingService = null;

        #endregion

        #region Views

        /// <summary>
        /// Renderer responsible for graph connections
        /// </summary>
        private TutorialConnectionRenderer connectionRenderer = null;

        /// <summary>
        /// View responsible for the inspector panel
        /// </summary>
        private TutorialInspectorView inspectorView = null;

        /// <summary>
        /// Factory responsible for graph node creation
        /// </summary>
        private TutorialNodeFactory nodeFactory = null;

        #endregion

        #region Controllers

        /// <summary>
        /// Controller responsible for StepSO to GameObject bindings
        /// </summary>
        private TutorialBindingController bindingController = null;

        /// <summary>
        /// Controller responsible for StepSO sequence connections
        /// </summary>
        private TutorialSequenceController sequenceController = null;

        /// <summary>
        /// Controller responsible for canvas interactions
        /// </summary>
        private TutorialCanvasController canvasController = null;

        #endregion

        #region Window Lifecycle

        /// <summary>
        /// Open the tutorial editor window
        /// </summary>
        [MenuItem(WindowMenuPath)]
        public static void Open()
        {
            TutorialTool window = GetWindow<TutorialTool>();
            window.titleContent = new GUIContent(WindowTitle);

            window.minSize = MinimumWindowSize;
            window.Show();
        }

        /// <summary>
        /// Initialize the persistent window properties
        /// </summary>
        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = MinimumWindowSize;
        }

        /// <summary>
        /// Create the editor window interface
        /// </summary>
        public void CreateGUI()
        {
            DisposeTool();

            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;

            CreateWindowLayout();
            InitializeTool();
        }

        /// <summary>
        /// Release the tool when the window is disabled
        /// </summary>
        private void OnDisable()
        {
            DisposeTool();
        }

        #endregion

        #region Layout

        /// <summary>
        /// Create the global window layout
        /// </summary>
        private void CreateWindowLayout()
        {
            CreateCanvasHost();
            CreateInspectorHost();

            TwoPaneSplitView splitView = new TwoPaneSplitView(1, InspectorWidth, TwoPaneSplitViewOrientation.Horizontal);

            splitView.name = "tutorial-split-view";
            splitView.style.flexGrow = 1f;

            splitView.Add(canvas);
            splitView.Add(inspectorPanel);

            rootVisualElement.Add(splitView);
        }

        /// <summary>
        /// Create the main graph canvas
        /// </summary>
        private void CreateCanvasHost()
        {
            canvas = new VisualElement
            {
                name = "tutorial-canvas",
                focusable = true
            };

            canvas.style.flexGrow = 1f;
            canvas.style.minWidth = 0f;
            canvas.style.position = Position.Relative;
            canvas.style.overflow = Overflow.Hidden;
            canvas.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            connectionLayer = new VisualElement
            {
                name = "tutorial-connection-layer",
                pickingMode = PickingMode.Ignore
            };

            connectionLayer.style.position = Position.Absolute;

            connectionLayer.style.left = 0f;
            connectionLayer.style.right = 0f;
            connectionLayer.style.top = 0f;
            connectionLayer.style.bottom = 0f;

            dropHint = new Label("Drop a StepSO or a GameObject with TutoIdentifier here")
            {
                name = "tutorial-drop-hint",
                pickingMode = PickingMode.Ignore
            };

            dropHint.style.position = Position.Absolute;

            dropHint.style.left = 0f;
            dropHint.style.right = 0f;
            dropHint.style.top = 0f;
            dropHint.style.bottom = 0f;

            dropHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            dropHint.style.whiteSpace = WhiteSpace.Normal;
            dropHint.style.color = new Color(0.55f, 0.55f, 0.55f);
            
            canvas.Add(connectionLayer);
            canvas.Add(dropHint);
        }

        /// <summary>
        /// Create the inspector panel container
        /// </summary>
        private void CreateInspectorHost()
        {
            inspectorPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "tutorial-inspector-panel"
            };

            inspectorPanel.style.flexGrow = 1f;
            inspectorPanel.style.minWidth = 0f;

            inspectorPanel.style.paddingLeft = 8f;
            inspectorPanel.style.paddingRight = 8f;
            inspectorPanel.style.paddingTop = 8f;
            inspectorPanel.style.paddingBottom = 8f;

            inspectorPanel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        }

        #endregion

        #region Tool Initialization

        /// <summary>
        /// Create and connect every tool component
        /// </summary>
        private void InitializeTool()
        {
            graphState = new TutorialGraphState();

            guidService = new TutorialGuidService();
            methodBindingService = new TutorialMethodBindingService();
            connectionRenderer = new TutorialConnectionRenderer(canvas, connectionLayer, graphState);
            inspectorView = new TutorialInspectorView(inspectorPanel, guidService, methodBindingService);
            bindingController = new TutorialBindingController(graphState, canvas, guidService, inspectorView, connectionRenderer);
            sequenceController = new TutorialSequenceController(graphState, canvas, connectionRenderer);
            nodeFactory = new TutorialNodeFactory(canvas, bindingController, sequenceController, connectionRenderer);
            canvasController = new TutorialCanvasController(rootVisualElement, canvas, connectionLayer, dropHint, graphState, nodeFactory, inspectorView, bindingController, sequenceController, connectionRenderer);

            connectionRenderer.Enable();
            canvasController.Enable();

            inspectorView.DisplayPlaceholder();
        }

        /// <summary>
        /// Release every component created by the window
        /// </summary>
        private void DisposeTool()
        {
            /*
             * Stop event listeners before releasing the objects
             * used by their callbacks.
             */
            canvasController?.Dispose();
            connectionRenderer?.Dispose();

            canvasController = null;
            bindingController = null;
            sequenceController = null;

            nodeFactory = null;
            inspectorView = null;
            connectionRenderer = null;

            methodBindingService = null;
            guidService = null;

            graphState = null;

            canvas = null;
            connectionLayer = null;
            dropHint = null;
            inspectorPanel = null;
        }

        #endregion
    }
}