using System;
using System.IO;

using Tutorial.Editor.Core;
using Tutorial.Runtime.Persistence;
using Tutorial.Editor.Services;
using Tutorial.Editor.Views;

using UnityEditor;
using UnityEngine.UIElements;

using UnityObject = UnityEngine.Object;

namespace Tutorial.Editor.Controllers
{
    /// <summary>
    /// Orchestrate the tutorial graph editing session, graph lifecycle and central tool screens
    /// </summary>
    internal sealed class TutorialSessionController : IDisposable
    {
        #region Session

        /// <summary>
        /// Current tutorial graph editing session
        /// </summary>
        private readonly TutorialGraphSession graphSession = null;

        /// <summary>
        /// Graph currently opened inside the editor
        /// </summary>
        private TutorialGraphAsset activeGraph = null;

        /// <summary>
        /// Whether controller callbacks are currently registered
        /// </summary>
        private bool isEnabled = false;

        #endregion

        #region Services

        /// <summary>
        /// Runtime registry containing the visual graph nodes
        /// </summary>
        private readonly TutorialGraphRuntimeRegistry runtimeRegistry = null;

        /// <summary>
        /// Repository responsible for graph asset operations
        /// </summary>
        private readonly TutorialGraphRepository graphRepository = null;

        /// <summary>
        /// Service responsible for graph persistence and load plans
        /// </summary>
        private readonly TutorialGraphPersistenceService graphPersistenceService = null;

        /// <summary>
        /// Service responsible for delayed and immediate graph saves
        /// </summary>
        private readonly TutorialGraphAutosaveService autosaveService = null;

        #endregion

        #region Views

        /// <summary>
        /// Visual container displaying the graph canvas and inspector
        /// </summary>
        private readonly VisualElement editorHost = null;

        /// <summary>
        /// Main tutorial graph launcher
        /// </summary>
        private readonly TutorialGraphLauncherView graphLauncherView = null;

        /// <summary>
        /// Existing tutorial graph browser
        /// </summary>
        private readonly TutorialGraphBrowserView graphBrowserView = null;

        /// <summary>
        /// New tutorial graph creation screen
        /// </summary>
        private readonly TutorialGraphCreationView graphCreationView = null;

        /// <summary>
        /// Toolbar containing graph selection and commands
        /// </summary>
        private readonly TutorialGraphToolbarView graphToolbarView = null;

        /// <summary>
        /// Bottom status bar
        /// </summary>
        private readonly TutorialGraphStatusBarView graphStatusBarView = null;

        #endregion

        #region Controllers

        /// <summary>
        /// Controller responsible for canvas interactions and graph reconstruction
        /// </summary>
        private readonly TutorialCanvasController canvasController = null;

        /// <summary>
        /// Controller responsible for StepSO bindings
        /// </summary>
        private readonly TutorialBindingController bindingController = null;

        /// <summary>
        /// Controller responsible for StepSO sequences
        /// </summary>
        private readonly TutorialSequenceController sequenceController = null;

        #endregion

        #region Properties

        /// <summary>
        /// Graph currently opened inside the editor
        /// </summary>
        public TutorialGraphAsset ActiveGraph => activeGraph;

        /// <summary>
        /// Whether a tutorial graph is currently opened
        /// </summary>
        public bool HasActiveGraph => activeGraph != null;

        #endregion

        #region Constructor

        public TutorialSessionController(TutorialGraphSession graphSession, TutorialGraphRuntimeRegistry runtimeRegistry, TutorialGraphRepository graphRepository, TutorialGraphPersistenceService graphPersistenceService, TutorialGraphAutosaveService autosaveService, VisualElement editorHost, TutorialGraphLauncherView graphLauncherView, TutorialGraphBrowserView graphBrowserView, TutorialGraphCreationView graphCreationView, TutorialGraphToolbarView graphToolbarView, TutorialGraphStatusBarView graphStatusBarView, TutorialCanvasController canvasController, TutorialBindingController bindingController, TutorialSequenceController sequenceController)
        {
            this.graphSession = graphSession ?? throw new ArgumentNullException(nameof(graphSession));
            this.runtimeRegistry = runtimeRegistry ?? throw new ArgumentNullException(nameof(runtimeRegistry));
            this.graphRepository = graphRepository ?? throw new ArgumentNullException(nameof(graphRepository));
            this.graphPersistenceService = graphPersistenceService ?? throw new ArgumentNullException(nameof(graphPersistenceService));
            this.autosaveService = autosaveService ?? throw new ArgumentNullException(nameof(autosaveService));

            this.editorHost = editorHost ?? throw new ArgumentNullException(nameof(editorHost));

            this.graphLauncherView = graphLauncherView ?? throw new ArgumentNullException(nameof(graphLauncherView));
            this.graphBrowserView = graphBrowserView ?? throw new ArgumentNullException(nameof(graphBrowserView));
            this.graphCreationView = graphCreationView ?? throw new ArgumentNullException(nameof(graphCreationView));
            this.graphToolbarView = graphToolbarView ?? throw new ArgumentNullException(nameof(graphToolbarView));
            this.graphStatusBarView = graphStatusBarView ?? throw new ArgumentNullException(nameof(graphStatusBarView));

            this.canvasController = canvasController ?? throw new ArgumentNullException(nameof(canvasController));
            this.bindingController = bindingController ?? throw new ArgumentNullException(nameof(bindingController));
            this.sequenceController = sequenceController ?? throw new ArgumentNullException(nameof(sequenceController));
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Register every session callback and display the launcher
        /// </summary>
        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            graphToolbarView.GraphSelectionChanged += OnGraphSelectionChanged;
            graphToolbarView.OpenRequested += OnToolbarOpenRequested;
            graphToolbarView.SaveRequested += OnToolbarSaveRequested;
            graphToolbarView.LocateRequested += OnToolbarLocateRequested;

            graphLauncherView.CreateRequested += OnLauncherCreateRequested;
            graphLauncherView.BrowseRequested += OnLauncherBrowseRequested;

            graphBrowserView.OpenRequested += OnBrowserOpenRequested;
            graphBrowserView.LocateRequested += OnBrowserLocateRequested;
            graphBrowserView.RefreshRequested += OnBrowserRefreshRequested;
            graphBrowserView.BackRequested += OnBrowserBackRequested;

            graphCreationView.CreateRequested += OnCreationCreateRequested;
            graphCreationView.BackRequested += OnCreationBackRequested;

            autosaveService.Saved += OnGraphSaved;
            autosaveService.SaveFailed += OnGraphSaveFailed;

            canvasController.GraphChanged += OnGraphChanged;
            bindingController.BindingChanged += OnGraphChanged;
            sequenceController.SequenceChanged += OnGraphChanged;

            isEnabled = true;

            ShowLauncher();
        }

        /// <summary>
        /// Unregister every session callback
        /// </summary>
        public void Dispose()
        {
            if (!isEnabled)
            {
                return;
            }

            graphToolbarView.GraphSelectionChanged -= OnGraphSelectionChanged;
            graphToolbarView.OpenRequested -= OnToolbarOpenRequested;
            graphToolbarView.SaveRequested -= OnToolbarSaveRequested;
            graphToolbarView.LocateRequested -= OnToolbarLocateRequested;

            graphLauncherView.CreateRequested -= OnLauncherCreateRequested;
            graphLauncherView.BrowseRequested -= OnLauncherBrowseRequested;

            graphBrowserView.OpenRequested -= OnBrowserOpenRequested;
            graphBrowserView.LocateRequested -= OnBrowserLocateRequested;
            graphBrowserView.RefreshRequested -= OnBrowserRefreshRequested;
            graphBrowserView.BackRequested -= OnBrowserBackRequested;

            graphCreationView.CreateRequested -= OnCreationCreateRequested;
            graphCreationView.BackRequested -= OnCreationBackRequested;

            autosaveService.Saved -= OnGraphSaved;
            autosaveService.SaveFailed -= OnGraphSaveFailed;

            canvasController.GraphChanged -= OnGraphChanged;
            bindingController.BindingChanged -= OnGraphChanged;
            sequenceController.SequenceChanged -= OnGraphChanged;

            isEnabled = false;
        }

        #endregion

        #region Screen Management

        /// <summary>
        /// Display the graph launcher
        /// </summary>
        public void ShowLauncher()
        {
            graphLauncherView.Show();
            graphBrowserView.Hide();
            graphCreationView.Hide();

            editorHost.style.display = DisplayStyle.None;

            graphStatusBarView.DisplayNoGraph();
        }

        /// <summary>
        /// Display the existing graph browser
        /// </summary>
        public void ShowBrowser()
        {
            graphLauncherView.Hide();
            graphCreationView.Hide();

            editorHost.style.display = DisplayStyle.None;

            RefreshBrowser();

            graphBrowserView.Show();
            graphStatusBarView.SetStatus("Select a tutorial graph", ETutorialGraphStatus.Normal);
        }

        /// <summary>
        /// Display the new graph creation screen
        /// </summary>
        public void ShowCreation()
        {
            graphLauncherView.Hide();
            graphBrowserView.Hide();

            editorHost.style.display = DisplayStyle.None;

            graphCreationView.Show();
            graphStatusBarView.SetStatus("Enter a graph name", ETutorialGraphStatus.Normal);
        }

        /// <summary>
        /// Display the graph canvas and inspector
        /// </summary>
        public void ShowEditor()
        {
            graphLauncherView.Hide();
            graphBrowserView.Hide();
            graphCreationView.Hide();

            editorHost.style.display = DisplayStyle.Flex;
        }

        #endregion

        #region Graph Browser

        /// <summary>
        /// Reload every TutorialGraphAsset inside the graph browser
        /// </summary>
        private void RefreshBrowser()
        {
            graphBrowserView.SetGraphs(graphRepository.FindAllGraphs());
        }

        #endregion

        #region Graph Opening

        /// <summary>
        /// Open and reconstruct the supplied tutorial graph
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public bool TryOpenGraph(TutorialGraphAsset graph)
        {
            if (graph == null)
            {
                graphStatusBarView.SetStatus("No valid graph selected", ETutorialGraphStatus.Warning);
                return false;
            }

            if (activeGraph == graph)
            {
                ShowEditor();

                graphToolbarView.SetSelectedGraph(graph);
                graphToolbarView.SetCommandAvailability(true, true, true);

                graphStatusBarView.DisplayGraph(graph, runtimeRegistry.Count);
                graphStatusBarView.SetStatus("Graph already opened", ETutorialGraphStatus.Normal);

                return true;
            }

            if (!TryFlushActiveGraph())
            {
                return false;
            }

            graphStatusBarView.SetStatus("Preparing graph...", ETutorialGraphStatus.Normal);

            if (!graphPersistenceService.TryCreateLoadPlan(graph, out TutorialGraphLoadPlan loadPlan, out string failureReason))
            {
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);
                return false;
            }

            autosaveService.CancelPendingSave();
            canvasController.ClearVisualGraph();

            if (!canvasController.TryRestoreNodes(loadPlan, out failureReason))
            {
                canvasController.ClearVisualGraph();
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

                return false;
            }

            if (!canvasController.TryRestoreConnections(loadPlan, out failureReason))
            {
                canvasController.ClearVisualGraph();
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

                return false;
            }

            graphSession.SetActiveGraph(graph);
            graphSession.MarkSaved();

            activeGraph = graph;

            ShowEditor();

            graphToolbarView.SetSelectedGraph(graph);
            graphToolbarView.SetCommandAvailability(true, true, true);

            graphStatusBarView.DisplayGraph(graph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Graph loaded", ETutorialGraphStatus.Success);

            Selection.activeObject = graph;

            return true;
        }

        /// <summary>
        /// Save pending changes before switching graph
        /// </summary>
        /// <returns></returns>
        private bool TryFlushActiveGraph()
        {
            if (!graphSession.IsDirty)
            {
                return true;
            }

            if (autosaveService.TryFlush(out string failureReason))
            {
                return true;
            }

            graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);

            return false;
        }

        #endregion

        #region Graph Creation

        /// <summary>
        /// Create and open a new TutorialGraphAsset
        /// </summary>
        /// <param name="graphName"></param>
        private void CreateGraph(string graphName)
        {
            if (string.IsNullOrWhiteSpace(graphName))
            {
                graphStatusBarView.SetStatus("The graph name is empty", ETutorialGraphStatus.Warning);
                return;
            }

            string sanitizedGraphName = SanitizeGraphName(graphName);

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Create Tutorial Graph",
                sanitizedGraphName,
                "asset",
                "Choose where the TutorialGraphAsset should be created."
            );

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                graphStatusBarView.SetStatus("Graph creation cancelled", ETutorialGraphStatus.Normal);
                return;
            }

            TutorialGraphAsset graph = null;
            string failureReason = string.Empty;
            bool graphCreated = false;

            graphCreationView.SetCommandsEnabled(false);

            try
            {
                graphCreated = graphRepository.TryCreateGraph(assetPath, out graph, out failureReason);
            }
            finally
            {
                graphCreationView.SetCommandsEnabled(true);
            }

            if (!graphCreated || graph == null)
            {
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);
                return;
            }

            graphToolbarView.SetSelectedGraph(graph);
            TryOpenGraph(graph);
        }

        /// <summary>
        /// Remove unsupported file name characters from a graph name
        /// </summary>
        /// <param name="graphName"></param>
        /// <returns></returns>
        private static string SanitizeGraphName(string graphName)
        {
            string sanitizedName = graphName.Trim();

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                sanitizedName = sanitizedName.Replace(invalidCharacter, '_');
            }

            sanitizedName = sanitizedName.Replace('/', '_');
            sanitizedName = sanitizedName.Replace('\\', '_');

            return string.IsNullOrWhiteSpace(sanitizedName) ? "TutorialGraph" : sanitizedName;
        }

        #endregion

        #region Graph Saving

        /// <summary>
        /// Immediately save the active graph
        /// </summary>
        /// <returns></returns>
        public bool TrySaveActiveGraph()
        {
            if (activeGraph == null)
            {
                graphStatusBarView.SetStatus("No active graph to save", ETutorialGraphStatus.Warning);
                return false;
            }

            if (!autosaveService.TrySaveNow(out string failureReason))
            {
                graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);
                return false;
            }

            graphStatusBarView.DisplayGraph(activeGraph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Graph saved", ETutorialGraphStatus.Success);

            return true;
        }

        #endregion

        #region Graph Location

        /// <summary>
        /// Locate a tutorial graph inside the Project window
        /// </summary>
        /// <param name="graph"></param>
        private void LocateGraph(TutorialGraphAsset graph)
        {
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

        #region Toolbar Callbacks

        /// <summary>
        /// Update graph commands when the selected graph changes
        /// </summary>
        /// <param name="selectedObject"></param>
        private void OnGraphSelectionChanged(UnityObject selectedObject)
        {
            bool hasSelection = selectedObject is TutorialGraphAsset;

            graphToolbarView.SetCommandAvailability(hasSelection, activeGraph != null, hasSelection || activeGraph != null);

            if (!hasSelection)
            {
                graphStatusBarView.SetStatus("Select a TutorialGraphAsset", ETutorialGraphStatus.Normal);
                return;
            }

            if (selectedObject == activeGraph)
            {
                graphStatusBarView.SetStatus("Active graph selected", ETutorialGraphStatus.Normal);
                return;
            }

            graphStatusBarView.SetStatus("Graph selected - press Open", ETutorialGraphStatus.Normal);
        }

        /// <summary>
        /// Open the graph selected inside the toolbar
        /// </summary>
        private void OnToolbarOpenRequested()
        {
            TryOpenGraph(graphToolbarView.SelectedGraph as TutorialGraphAsset);
        }

        /// <summary>
        /// Save the active graph
        /// </summary>
        private void OnToolbarSaveRequested()
        {
            TrySaveActiveGraph();
        }

        /// <summary>
        /// Locate the selected or active graph
        /// </summary>
        private void OnToolbarLocateRequested()
        {
            TutorialGraphAsset graph = graphToolbarView.SelectedGraph as TutorialGraphAsset;

            if (graph == null)
            {
                graph = activeGraph;
            }

            LocateGraph(graph);
        }

        #endregion

        #region Launcher Callbacks

        /// <summary>
        /// Display graph creation
        /// </summary>
        private void OnLauncherCreateRequested()
        {
            ShowCreation();
        }

        /// <summary>
        /// Display existing tutorial graphs
        /// </summary>
        private void OnLauncherBrowseRequested()
        {
            ShowBrowser();
        }

        #endregion

        #region Browser Callbacks

        /// <summary>
        /// Open a graph selected inside the browser
        /// </summary>
        /// <param name="graph"></param>
        private void OnBrowserOpenRequested(TutorialGraphAsset graph)
        {
            if (graph == null)
            {
                graphStatusBarView.SetStatus("The selected graph is invalid", ETutorialGraphStatus.Error);
                return;
            }

            graphToolbarView.SetSelectedGraph(graph);
            TryOpenGraph(graph);
        }

        /// <summary>
        /// Locate a graph selected inside the browser
        /// </summary>
        /// <param name="graph"></param>
        private void OnBrowserLocateRequested(TutorialGraphAsset graph)
        {
            LocateGraph(graph);
        }

        /// <summary>
        /// Refresh the graph browser
        /// </summary>
        private void OnBrowserRefreshRequested()
        {
            RefreshBrowser();
            graphStatusBarView.SetStatus("Graph browser refreshed", ETutorialGraphStatus.Success);
        }

        /// <summary>
        /// Return to the graph launcher
        /// </summary>
        private void OnBrowserBackRequested()
        {
            ShowLauncher();
        }

        #endregion

        #region Creation Callbacks

        /// <summary>
        /// Create a new graph using the supplied graph name
        /// </summary>
        /// <param name="graphName"></param>
        private void OnCreationCreateRequested(string graphName)
        {
            CreateGraph(graphName);
        }

        /// <summary>
        /// Return to the graph launcher
        /// </summary>
        private void OnCreationBackRequested()
        {
            ShowLauncher();
        }

        #endregion

        #region Graph Change Callbacks

        /// <summary>
        /// Mark the active graph as changed and request an automatic save
        /// </summary>
        private void OnGraphChanged()
        {
            if (activeGraph == null)
            {
                return;
            }

            autosaveService.RequestSave();

            graphStatusBarView.DisplayGraph(activeGraph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Unsaved changes", ETutorialGraphStatus.Warning);
        }

        /// <summary>
        /// Display a successful automatic save
        /// </summary>
        private void OnGraphSaved()
        {
            if (activeGraph == null)
            {
                return;
            }

            graphStatusBarView.DisplayGraph(activeGraph, runtimeRegistry.Count);
            graphStatusBarView.SetStatus("Saved", ETutorialGraphStatus.Success);
        }

        /// <summary>
        /// Display an automatic save failure
        /// </summary>
        /// <param name="failureReason"></param>
        private void OnGraphSaveFailed(string failureReason)
        {
            graphStatusBarView.SetStatus(failureReason, ETutorialGraphStatus.Error);
        }

        #endregion
    }
}