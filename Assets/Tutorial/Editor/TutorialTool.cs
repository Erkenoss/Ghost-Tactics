using Tutorial.Editor.Controllers;
using Tutorial.Editor.Core;
using Tutorial.Runtime.Persistence;
using Tutorial.Editor.Services;
using Tutorial.Editor.Settings;
using Tutorial.Editor.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial.Editor
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

        private const string WindowMenuPath = "Window/Tutorial/Tutorial Tool";
        private const string WindowTitle = "Tutorial Tool";
        private const float InspectorWidth = 320f;
        private static readonly Vector2 MinimumWindowSize = new Vector2(900f, 520f);

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

        /// <summary>
        /// Host containing tutorial toolbars
        /// </summary>
        private VisualElement toolbarHost = null;

        /// <summary>
        /// Host containing the graph status bar
        /// </summary>
        private VisualElement statusBarHost = null;

        /// <summary>
        /// Host displaying the current central tool screen
        /// </summary>
        private VisualElement contentHost = null;

        /// <summary>
        /// Host containing the graph canvas and inspector
        /// </summary>
        private VisualElement editorHost = null;

        #endregion

        #region Tool State

        /// <summary>
        /// Temporary state of the currently edited tutorial graph
        /// </summary>
        private TutorialGraphState graphState = null;

        /// <summary>
        /// Session associated with the currently edited tutorial graph
        /// </summary>
        private TutorialGraphSession graphSession = null;

        /// <summary>
        /// Tutorial graph to restore after an Editor domain reload
        /// </summary>
        [SerializeField]
        private TutorialGraphAsset graphToRestore = null;

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

        /// <summary>
        /// Service responsible for StepSequenceSO asset creation
        /// </summary>
        private TutorialSequenceAssetService sequenceAssetService = null;

        /// <summary>
        /// Service responsible for the save runtime registry
        /// </summary>
        private TutorialGraphRuntimeRegistry runtimeRegistry = null;

        /// <summary>
        /// View displaying the current StepSequenceSO folder
        /// </summary>
        private TutorialSequenceFolderView sequenceFolderView = null;

        /// <summary>
        /// Repository responsible for TutorialGraphAsset operations
        /// </summary>
        private TutorialGraphRepository graphRepository = null;

        /// <summary>
        /// Service responsible for resolving graph asset references
        /// </summary>
        private TutorialGraphReferenceResolver graphReferenceResolver = null;

        /// <summary>
        /// Service responsible for graph serialization and restoration preparation
        /// </summary>
        private TutorialGraphPersistenceService graphPersistenceService = null;

        /// <summary>
        /// Service responsible for rebuilding tutorial IL instrumentation data
        /// </summary>
        private TutorialInjectionManifestService injectionManifestService = null;

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

        /// <summary>
        /// View displaying graph commands
        /// </summary>
        private TutorialGraphToolbarView graphToolbarView = null;

        /// <summary>
        /// View displaying active graph status
        /// </summary>
        private TutorialGraphStatusBarView graphStatusBarView = null;

        /// <summary>
        /// View displayed when no tutorial graph is currently opened
        /// </summary>
        private TutorialGraphLauncherView graphLauncherView = null;

        /// <summary>
        /// View used to browse existing tutorial graphs
        /// </summary>
        private TutorialGraphBrowserView graphBrowserView = null;

        /// <summary>
        /// View used to create a new tutorial graph
        /// </summary>
        private TutorialGraphCreationView graphCreationView = null;

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

        /// <summary>
        /// Controller responsible for the tutorial graph editing session
        /// </summary>
        private TutorialSessionController sessionController = null;

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
            if (sessionController != null && sessionController.ActiveGraph != null)
            {
                graphToRestore = sessionController.ActiveGraph;
            }

            if (graphSession != null && graphSession.IsDirty && sessionController != null)
            {
                sessionController.TrySaveActiveGraph();
            }

            DisposeTool();
        }

        #endregion

        #region Layout

        /// <summary>
        /// Create the global window layout
        /// </summary>
        private void CreateWindowLayout()
        {
            toolbarHost = new VisualElement
            {
                name = "tutorial-toolbar-host"
            };

            toolbarHost.style.flexShrink = 0f;
            toolbarHost.style.flexDirection = FlexDirection.Column;

            CreateCanvasHost();
            CreateInspectorHost();

            TwoPaneSplitView splitView = new TwoPaneSplitView(1, InspectorWidth, TwoPaneSplitViewOrientation.Horizontal);
            splitView.name = "tutorial-split-view";
            splitView.style.flexGrow = 1f;

            splitView.Add(canvas);
            splitView.Add(inspectorPanel);

            editorHost = splitView;

            contentHost = new VisualElement
            {
                name = "tutorial-content-host"
            };

            contentHost.style.flexGrow = 1f;
            contentHost.style.position = Position.Relative;
            contentHost.Add(editorHost);

            statusBarHost = new VisualElement
            {
                name = "tutorial-status-bar-host"
            };

            statusBarHost.style.flexShrink = 0f;

            VisualElement windowRoot = new VisualElement
            {
                name = "tutorial-window-root"
            };

            windowRoot.style.flexGrow = 1f;
            windowRoot.style.flexDirection = FlexDirection.Column;

            windowRoot.Add(toolbarHost);
            windowRoot.Add(contentHost);
            windowRoot.Add(statusBarHost);

            rootVisualElement.Add(windowRoot);
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
            TutorialToolProjectSettings projectSettings = TutorialToolProjectSettings.instance;

            graphToolbarView = new TutorialGraphToolbarView(typeof(TutorialGraphAsset));
            graphStatusBarView = new TutorialGraphStatusBarView();
            graphLauncherView = new TutorialGraphLauncherView();
            graphBrowserView = new TutorialGraphBrowserView();
            graphCreationView = new TutorialGraphCreationView();
            injectionManifestService = new TutorialInjectionManifestService();

            toolbarHost.Add(graphToolbarView.Root);
            statusBarHost.Add(graphStatusBarView.Root);

            contentHost.Insert(0, graphLauncherView.Root);
            contentHost.Insert(1, graphBrowserView.Root);
            contentHost.Insert(2, graphCreationView.Root);

            graphStatusBarView.DisplayNoGraph();

            graphState = new TutorialGraphState();
            graphSession = new TutorialGraphSession();
            runtimeRegistry = new TutorialGraphRuntimeRegistry();

            guidService = new TutorialGuidService();
            methodBindingService = new TutorialMethodBindingService();
            sequenceAssetService = new TutorialSequenceAssetService(projectSettings);

            graphRepository = new TutorialGraphRepository();
            graphReferenceResolver = new TutorialGraphReferenceResolver();
            graphPersistenceService = new TutorialGraphPersistenceService(graphRepository, graphReferenceResolver, runtimeRegistry, graphState, graphSession, injectionManifestService);

            connectionRenderer = new TutorialConnectionRenderer(canvas, connectionLayer, graphState);
            inspectorView = new TutorialInspectorView(inspectorPanel, guidService, methodBindingService);

            bindingController = new TutorialBindingController(graphState, canvas, guidService, inspectorView, connectionRenderer);
            sequenceController = new TutorialSequenceController(graphState, canvas, sequenceAssetService, connectionRenderer);
            nodeFactory = new TutorialNodeFactory(canvas, bindingController, sequenceController, connectionRenderer);

            sequenceFolderView = new TutorialSequenceFolderView(sequenceAssetService);
            toolbarHost.Insert(0, sequenceFolderView.Root);

            canvasController = new TutorialCanvasController(editorHost, canvas, connectionLayer, dropHint, graphState, runtimeRegistry, nodeFactory, inspectorView, bindingController, sequenceController, connectionRenderer);

            sessionController = new TutorialSessionController(graphSession, runtimeRegistry, graphRepository, graphPersistenceService, editorHost, graphLauncherView, 
                                                              graphBrowserView, graphCreationView, graphToolbarView, graphStatusBarView, canvasController, bindingController, sequenceController);

            connectionRenderer.Enable();
            canvasController.Enable();
            inspectorView.DisplayPlaceholder();
            sessionController.Enable();

            if (graphToRestore != null)
            {
                sessionController.TryOpenGraph(graphToRestore);
            }
        }

        /// <summary>
        /// Release every component created by the window
        /// </summary>
        private void DisposeTool()
        {
            sessionController?.Dispose();
            sessionController = null;

            canvasController?.Dispose();
            connectionRenderer?.Dispose();

            runtimeRegistry?.Clear();

            canvasController = null;
            bindingController = null;
            sequenceController = null;

            nodeFactory = null;
            inspectorView = null;
            connectionRenderer = null;
            sequenceFolderView = null;

            graphPersistenceService = null;
            injectionManifestService = null;
            graphReferenceResolver = null;
            graphRepository = null;

            sequenceAssetService = null;
            methodBindingService = null;
            guidService = null;
            runtimeRegistry = null;

            graphSession = null;
            graphState = null;

            graphLauncherView = null;
            graphBrowserView = null;
            graphCreationView = null;
            graphToolbarView = null;
            graphStatusBarView = null;

            canvas = null;
            connectionLayer = null;
            dropHint = null;
            inspectorPanel = null;

            contentHost = null;
            editorHost = null;
            toolbarHost = null;
            statusBarHost = null;
        }

        #endregion
    }
}
