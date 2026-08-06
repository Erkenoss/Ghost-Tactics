using Tutorial.Editor.Controllers;
using Tutorial.Editor.Core;
using Tutorial.Editor.Persistence;
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

        /// <summary>
        /// Host containing tutorial toolbars
        /// </summary>
        private VisualElement toolbarHost = null;

        /// <summary>
        /// Host containing the graph status bar
        /// </summary>
        private VisualElement statusBarHost = null;

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
        /// Graph currently displayed inside the canvas
        /// </summary>
        private TutorialGraphAsset openedGraph = null;

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
        /// Service responsible for the save runtime resgistry
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
        /// Services to manage save of the tutorial
        /// </summary>
        private TutorialGraphAutosaveService autosaveService = null;

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
            windowRoot.Add(splitView);
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

            toolbarHost.Add(graphToolbarView.Root);
            statusBarHost.Add(graphStatusBarView.Root);

            graphStatusBarView.DisplayNoGraph();
            graphStatusBarView.SetAutosaveEnabled(projectSettings.AutosaveEnabled);

            graphState = new TutorialGraphState();
            graphSession = new TutorialGraphSession();
            runtimeRegistry = new TutorialGraphRuntimeRegistry();

            guidService = new TutorialGuidService();
            methodBindingService = new TutorialMethodBindingService();
            sequenceAssetService = new TutorialSequenceAssetService(projectSettings);

            graphRepository = new TutorialGraphRepository();
            graphReferenceResolver = new TutorialGraphReferenceResolver();
            graphPersistenceService = new TutorialGraphPersistenceService(graphRepository, graphReferenceResolver, runtimeRegistry, graphState, graphSession);

            connectionRenderer = new TutorialConnectionRenderer(canvas, connectionLayer, graphState);
            inspectorView = new TutorialInspectorView(inspectorPanel, guidService, methodBindingService);

            bindingController = new TutorialBindingController(graphState, canvas, guidService, inspectorView, connectionRenderer);
            sequenceController = new TutorialSequenceController(graphState, canvas, sequenceAssetService, connectionRenderer);

            nodeFactory = new TutorialNodeFactory(canvas, bindingController, sequenceController, connectionRenderer);

            sequenceFolderView = new TutorialSequenceFolderView(sequenceAssetService);
            toolbarHost.Insert(0, sequenceFolderView.Root);

            canvasController = new TutorialCanvasController(rootVisualElement, canvas, connectionLayer, dropHint, graphState, runtimeRegistry, nodeFactory, inspectorView, bindingController, sequenceController, connectionRenderer);

            autosaveService = new TutorialGraphAutosaveService(graphSession, graphPersistenceService, projectSettings.AutosaveDelay);

            if (projectSettings.AutosaveEnabled)
            {
                autosaveService.Enable();
            }

            graphToolbarView.GraphSelectionChanged += OnGraphSelectionChanged;
            graphToolbarView.OpenRequested += OnOpenGraphRequested;
            graphToolbarView.SaveRequested += OnSaveGraphRequested;
            graphToolbarView.LocateRequested += OnLocateGraphRequested;

            autosaveService.Saved += OnGraphSaved;
            autosaveService.SaveFailed += OnGraphSaveFailed;

            canvasController.GraphChanged += OnGraphChanged;
            bindingController.BindingChanged += OnGraphChanged;
            sequenceController.SequenceChanged += OnGraphChanged;

            canvasController.GraphChanged += autosaveService.RequestSave;
            bindingController.BindingChanged += autosaveService.RequestSave;
            sequenceController.SequenceChanged += autosaveService.RequestSave;

            connectionRenderer.Enable();
            canvasController.Enable();

            inspectorView.DisplayPlaceholder();
        }

        /// <summary>
        /// Release every component created by the window
        /// </summary>
        private void DisposeTool()
        {
            if (graphToolbarView != null)
            {
                graphToolbarView.GraphSelectionChanged -= OnGraphSelectionChanged;
                graphToolbarView.OpenRequested -= OnOpenGraphRequested;
                graphToolbarView.SaveRequested -= OnSaveGraphRequested;
                graphToolbarView.LocateRequested -= OnLocateGraphRequested;
            }

            if (autosaveService != null)
            {
                autosaveService.Saved -= OnGraphSaved;
                autosaveService.SaveFailed -= OnGraphSaveFailed;
            }

            if (canvasController != null)
            {
                canvasController.GraphChanged -= OnGraphChanged;
            }

            if (bindingController != null)
            {
                bindingController.BindingChanged -= OnGraphChanged;
            }

            if (sequenceController != null)
            {
                sequenceController.SequenceChanged -= OnGraphChanged;
            }

            if (autosaveService != null)
            {
                if (canvasController != null)
                {
                    canvasController.GraphChanged -= autosaveService.RequestSave;
                }

                if (bindingController != null)
                {
                    bindingController.BindingChanged -= autosaveService.RequestSave;
                }

                if (sequenceController != null)
                {
                    sequenceController.SequenceChanged -= autosaveService.RequestSave;
                }

                autosaveService.Dispose();
                autosaveService = null;
            }

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
            graphReferenceResolver = null;
            graphRepository = null;

            sequenceAssetService = null;
            methodBindingService = null;
            guidService = null;
            runtimeRegistry = null;

            graphSession = null;
            graphState = null;

            canvas = null;
            connectionLayer = null;
            dropHint = null;
            inspectorPanel = null;

            graphToolbarView = null;
            graphStatusBarView = null;
            sequenceFolderView = null;

            openedGraph = null;

            toolbarHost = null;
            statusBarHost = null;
        }

        #endregion

        #region Graph Commands

        /// <summary>
        /// Update graph commands when the selected graph changes
        /// </summary>
        /// <param name="selectedGraph"></param>
        private void OnGraphSelectionChanged(UnityEngine.Object selectedGraph)
        {
            bool hasSelection = selectedGraph is TutorialGraphAsset;

            graphToolbarView.SetCommandAvailability(hasSelection, openedGraph != null, hasSelection || openedGraph != null);

            if (!hasSelection)
            {
                graphStatusBarView.SetStatus("Select a TutorialGraphAsset", ETutorialGraphStatus.Normal);

                return;
            }

            if (selectedGraph == openedGraph)
            {
                graphStatusBarView.SetStatus("Active graph selected", ETutorialGraphStatus.Normal);

                return;
            }

            graphStatusBarView.SetStatus("Graph selected - press Open", ETutorialGraphStatus.Normal);
        }

        /// <summary>
        /// Open and reconstruct the selected tutorial graph
        /// </summary>
        private void OnOpenGraphRequested()
        {
            if (graphToolbarView.SelectedGraph is not TutorialGraphAsset graph)
            {
                graphStatusBarView.SetStatus("No valid graph selected", ETutorialGraphStatus.Warning);

                return;
            }

            if (openedGraph == graph)
            {
                graphStatusBarView.SetStatus("Graph already opened", ETutorialGraphStatus.Normal);

                return;
            }

            if (graphSession.IsDirty && !autosaveService.TryFlush(out string saveFailureReason))
            {
                graphStatusBarView.SetStatus(saveFailureReason, ETutorialGraphStatus.Error);

                return;
            }

            graphStatusBarView.SetStatus("Preparing graph...", ETutorialGraphStatus.Normal);

            if (!graphPersistenceService.TryCreateLoadPlan(graph, out TutorialGraphLoadPlan loadPlan, out string failureReason))
            {
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

                return;
            }

            autosaveService.CancelPendingSave();
            canvasController.ClearVisualGraph();

            if (!canvasController.TryRestoreNodes(loadPlan, out failureReason))
            {
                canvasController.ClearVisualGraph();
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

                return;
            }

            if (!canvasController.TryRestoreConnections(loadPlan, out failureReason))
            {
                canvasController.ClearVisualGraph();
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

                return;
            }

            /*
             * This call associates the reconstructed asset with the editing session.
             * Keep the equivalent method name used by your TutorialGraphSession.
             */
            graphSession.SetActiveGraph(graph);
            graphSession.MarkSaved();

            openedGraph = graph;

            graphToolbarView.SetSelectedGraph(graph);
            graphToolbarView.SetCommandAvailability(true, true, true);

            graphStatusBarView.DisplayGraph(graph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Graph loaded", ETutorialGraphStatus.Success);

            Selection.activeObject = graph;
        }

        /// <summary>
        /// Immediately save the active graph
        /// </summary>
        private void OnSaveGraphRequested()
        {
            if (openedGraph == null)
            {
                graphStatusBarView.SetStatus("No active graph to save", ETutorialGraphStatus.Warning);

                return;
            }

            if (!autosaveService.TrySaveNow(out string failureReason))
            {
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

                return;
            }

            graphStatusBarView.DisplayGraph(openedGraph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Graph saved", ETutorialGraphStatus.Success);
        }

        /// <summary>
        /// Locate the selected or active graph inside the Project window
        /// </summary>
        private void OnLocateGraphRequested()
        {
            UnityEngine.Object graph = graphToolbarView.SelectedGraph != null ? graphToolbarView.SelectedGraph : openedGraph;

            if (graph == null)
            {
                graphStatusBarView.SetStatus("No graph to locate", ETutorialGraphStatus.Warning);

                return;
            }

            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);

            graphStatusBarView.SetStatus("Graph located", ETutorialGraphStatus.Success);
        }

        #endregion

        #region Graph Status

        /// <summary>
        /// Display that the active graph contains unsaved changes
        /// </summary>
        private void OnGraphChanged()
        {
            if (openedGraph == null)
            {
                return;
            }

            graphStatusBarView.DisplayGraph(openedGraph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Unsaved changes", ETutorialGraphStatus.Warning);
        }

        /// <summary>
        /// Display a successful graph save
        /// </summary>
        private void OnGraphSaved()
        {
            if (openedGraph == null)
            {
                return;
            }

            graphStatusBarView.DisplayGraph(openedGraph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Saved", ETutorialGraphStatus.Success);
        }

        /// <summary>
        /// Display an automatic graph save failure
        /// </summary>
        /// <param name="failureReason"></param>
        private void OnGraphSaveFailed(string failureReason)
        {
            graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);
        }

        #endregion
    }
}