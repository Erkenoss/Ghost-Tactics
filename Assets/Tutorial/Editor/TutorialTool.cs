using System;
using System.Collections.Generic;
using Tutorial.Editor.Controllers;
using Tutorial.Editor.Core;
using Tutorial.Editor.Services;
using Tutorial.Editor.Settings;
using Tutorial.Editor.Views;
using Tutorial.Runtime.Persistence;
using UnityEditor;
using UnityEditor.UIElements;
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
        /// Root containing project-wide tutorial controls
        /// </summary>
        private VisualElement globalControlsRoot = null;

        /// <summary>
        /// Object field used to select the GameObject containing the Skip method
        /// </summary>
        private ObjectField skipTargetField = null;

        /// <summary>
        /// Dropdown used to select one MonoBehaviour from the Skip target
        /// </summary>
        private DropdownField skipScriptDropdown = null;

        /// <summary>
        /// Dropdown used to select one compatible public method
        /// </summary>
        private DropdownField skipMethodDropdown = null;

        /// <summary>
        /// Display the currently persisted Skip binding
        /// </summary>
        private Label skipBindingStatus = null;

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

        /// <summary>
        /// Temporary GameObject used to configure the global Skip Current Step binding
        /// </summary>
        [SerializeField]
        private GameObject skipBindingTarget = null;

        /// <summary>
        /// MonoBehaviours currently available on the Skip target
        /// </summary>
        private readonly List<MonoBehaviour> skipBindingScripts = new List<MonoBehaviour>();

        /// <summary>
        /// Compatible methods currently available on the selected Skip script
        /// </summary>
        private readonly List<MethodBindingOption> skipBindingMethods = new List<MethodBindingOption>();

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
        private TutorialAssetCreationView assetCreationView = null;

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


        private TutorialAssetPathService assetPathService = null;


        private TutorialStepAssetService stepAssetService = null;

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

        private TutorialAssetCreationController assetCreationController = null;

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
            assetPathService = new TutorialAssetPathService();
            stepAssetService = new TutorialStepAssetService(projectSettings, assetPathService);
            sequenceAssetService = new TutorialSequenceAssetService(projectSettings, assetPathService);

            graphRepository = new TutorialGraphRepository();
            graphReferenceResolver = new TutorialGraphReferenceResolver();
            graphPersistenceService = new TutorialGraphPersistenceService(graphRepository, graphReferenceResolver, runtimeRegistry, graphState, graphSession, injectionManifestService);

            connectionRenderer = new TutorialConnectionRenderer(canvas, connectionLayer, graphState);
            inspectorView = new TutorialInspectorView(inspectorPanel, guidService, methodBindingService);

            bindingController = new TutorialBindingController(graphState, canvas, guidService, inspectorView, connectionRenderer);
            sequenceController = new TutorialSequenceController(graphState, canvas, sequenceAssetService, connectionRenderer);
            nodeFactory = new TutorialNodeFactory(canvas, bindingController, sequenceController, connectionRenderer);

            assetCreationView = new TutorialAssetCreationView(projectSettings);
            toolbarHost.Insert(0, assetCreationView.Root);

            CreateGlobalControls(projectSettings);
            toolbarHost.Insert(1, globalControlsRoot);

            canvasController = new TutorialCanvasController(editorHost, canvas, connectionLayer, dropHint, graphState, runtimeRegistry, nodeFactory, inspectorView, bindingController, sequenceController, connectionRenderer);

            sessionController = new TutorialSessionController(graphSession, runtimeRegistry, graphRepository, graphPersistenceService, editorHost, graphLauncherView,
                                                              graphBrowserView, graphCreationView, graphToolbarView, graphStatusBarView, canvasController, bindingController, sequenceController);

            assetCreationController = new TutorialAssetCreationController(assetCreationView, stepAssetService, sequenceAssetService, canvasController, sessionController);

            connectionRenderer.Enable();
            canvasController.Enable();
            assetCreationController.Enable();
            inspectorView.DisplayPlaceholder();
            sessionController.Enable();

            if (graphToRestore != null)
            {
                sessionController.TryOpenGraph(graphToRestore);
            }
        }

        #endregion

        #region Global Controls

        /// <summary>
        /// Create project-wide tutorial control configuration
        /// </summary>
        /// <param name="projectSettings"></param>
        private void CreateGlobalControls(TutorialToolProjectSettings projectSettings)
        {
            globalControlsRoot = new VisualElement
            {
                name = "tutorial-global-controls"
            };

            globalControlsRoot.style.flexDirection = FlexDirection.Column;
            globalControlsRoot.style.paddingLeft = 6f;
            globalControlsRoot.style.paddingRight = 6f;
            globalControlsRoot.style.paddingTop = 4f;
            globalControlsRoot.style.paddingBottom = 4f;

            Foldout controlsFoldout = new Foldout
            {
                text = "Global Controls",
                value = true
            };

            VisualElement skipContainer = new VisualElement();
            skipContainer.style.flexDirection = FlexDirection.Row;
            skipContainer.style.alignItems = Align.Center;

            skipTargetField = new ObjectField("Skip Target")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                value = skipBindingTarget
            };

            skipTargetField.style.flexGrow = 1f;
            skipTargetField.style.marginRight = 6f;

            skipScriptDropdown = new DropdownField("Script", new List<string> { "Select a script..." }, 0);
            skipScriptDropdown.style.width = 260f;
            skipScriptDropdown.style.marginRight = 6f;

            skipMethodDropdown = new DropdownField("Method", new List<string> { "Select a method..." }, 0);
            skipMethodDropdown.style.width = 260f;

            skipBindingStatus = new Label();
            skipBindingStatus.style.marginTop = 3f;
            skipBindingStatus.style.marginLeft = 3f;

            skipTargetField.RegisterValueChangedCallback(changeEvent => OnSkipTargetChanged(changeEvent.newValue as GameObject, projectSettings));
            skipScriptDropdown.RegisterValueChangedCallback(changeEvent => OnSkipScriptSelected(projectSettings));
            skipMethodDropdown.RegisterValueChangedCallback(changeEvent => OnSkipMethodSelected(projectSettings));

            skipContainer.Add(skipTargetField);
            skipContainer.Add(skipScriptDropdown);
            skipContainer.Add(skipMethodDropdown);

            controlsFoldout.Add(skipContainer);
            controlsFoldout.Add(skipBindingStatus);

            globalControlsRoot.Add(controlsFoldout);

            RefreshSkipBindingUI(projectSettings);
        }

        /// <summary>
        /// Refresh Skip configuration after selecting another GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="projectSettings"></param>
        private void OnSkipTargetChanged(GameObject gameObject, TutorialToolProjectSettings projectSettings)
        {
            skipBindingTarget = gameObject;

            RefreshSkipBindingUI(projectSettings);
        }

        /// <summary>
        /// Rebuild every Skip configuration dropdown
        /// </summary>
        /// <param name="projectSettings"></param>
        private void RefreshSkipBindingUI(TutorialToolProjectSettings projectSettings)
        {
            skipBindingScripts.Clear();
            skipBindingMethods.Clear();

            List<string> scriptChoices = new List<string>
            {
                "Select a script..."
            };

            if (skipBindingTarget != null)
            {
                IReadOnlyList<MonoBehaviour> scripts = methodBindingService.GetScripts(skipBindingTarget);

                foreach (MonoBehaviour script in scripts)
                {
                    if (script == null)
                    {
                        continue;
                    }

                    skipBindingScripts.Add(script);
                    scriptChoices.Add(script.GetType().Name);
                }
            }

            skipScriptDropdown.choices = scriptChoices;

            int selectedScriptIndex = GetStoredSkipScriptIndex(projectSettings);

            skipScriptDropdown.SetValueWithoutNotify(scriptChoices[selectedScriptIndex]);
            skipScriptDropdown.SetEnabled(skipBindingScripts.Count > 0);

            if (selectedScriptIndex > 0)
            {
                RefreshSkipMethodDropdown(skipBindingScripts[selectedScriptIndex - 1], projectSettings);
            }
            else
            {
                ClearSkipMethodDropdown();
            }

            RefreshSkipBindingStatus(projectSettings);
        }

        /// <summary>
        /// Find the script dropdown index corresponding to the persisted Skip binding
        /// </summary>
        /// <param name="projectSettings"></param>
        /// <returns></returns>
        private int GetStoredSkipScriptIndex(TutorialToolProjectSettings projectSettings)
        {
            if (projectSettings == null || !projectSettings.HasSkipBinding)
            {
                return 0;
            }

            for (int i = 0; i < skipBindingScripts.Count; i++)
            {
                MonoBehaviour script = skipBindingScripts[i];

                if (script == null)
                {
                    continue;
                }

                if (string.Equals(script.GetType().FullName, projectSettings.SkipScriptName, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Refresh the method dropdown after selecting another MonoBehaviour
        /// </summary>
        /// <param name="projectSettings"></param>
        private void OnSkipScriptSelected(TutorialToolProjectSettings projectSettings)
        {
            int scriptIndex = skipScriptDropdown.index - 1;

            if (scriptIndex < 0 || scriptIndex >= skipBindingScripts.Count)
            {
                ClearSkipMethodDropdown();
                return;
            }

            RefreshSkipMethodDropdown(skipBindingScripts[scriptIndex], projectSettings);
        }

        /// <summary>
        /// Rebuild the compatible public method dropdown for one MonoBehaviour
        /// </summary>
        /// <param name="script"></param>
        /// <param name="projectSettings"></param>
        private void RefreshSkipMethodDropdown(MonoBehaviour script, TutorialToolProjectSettings projectSettings)
        {
            skipBindingMethods.Clear();

            IReadOnlyList<MethodBindingOption> options = methodBindingService.GetScriptBindingOptions(script);

            List<string> methodChoices = new List<string>
            {
                "Select a method..."
            };

            foreach (MethodBindingOption option in options)
            {
                if (option == null || !option.IsValid)
                {
                    continue;
                }

                skipBindingMethods.Add(option);
                methodChoices.Add($"{option.StoredMethodName}()");
            }

            if (skipBindingMethods.Count == 0)
            {
                skipMethodDropdown.choices = new List<string>
                {
                    "No compatible public method"
                };

                skipMethodDropdown.SetValueWithoutNotify(skipMethodDropdown.choices[0]);
                skipMethodDropdown.SetEnabled(false);

                return;
            }

            skipMethodDropdown.choices = methodChoices;

            int selectedMethodIndex = GetStoredSkipMethodIndex(script, projectSettings);

            skipMethodDropdown.SetValueWithoutNotify(methodChoices[selectedMethodIndex]);
            skipMethodDropdown.SetEnabled(true);
        }

        /// <summary>
        /// Find the method dropdown index corresponding to the persisted Skip binding
        /// </summary>
        /// <param name="script"></param>
        /// <param name="projectSettings"></param>
        /// <returns></returns>
        private int GetStoredSkipMethodIndex(MonoBehaviour script, TutorialToolProjectSettings projectSettings)
        {
            if (script == null || projectSettings == null || !projectSettings.HasSkipBinding)
            {
                return 0;
            }

            if (!string.Equals(script.GetType().FullName, projectSettings.SkipScriptName, StringComparison.Ordinal))
            {
                return 0;
            }

            for (int i = 0; i < skipBindingMethods.Count; i++)
            {
                MethodBindingOption option = skipBindingMethods[i];

                if (option == null || !option.IsValid)
                {
                    continue;
                }

                if (string.Equals(option.StoredMethodName, projectSettings.SkipMethodName, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Clear and disable the Skip method dropdown
        /// </summary>
        private void ClearSkipMethodDropdown()
        {
            skipBindingMethods.Clear();

            skipMethodDropdown.choices = new List<string>
            {
                "Select a method..."
            };

            skipMethodDropdown.SetValueWithoutNotify(skipMethodDropdown.choices[0]);
            skipMethodDropdown.SetEnabled(false);
        }

        /// <summary>
        /// Persist the selected global Skip Current Step binding
        /// </summary>
        /// <param name="projectSettings"></param>
        private void OnSkipMethodSelected(TutorialToolProjectSettings projectSettings)
        {
            int methodIndex = skipMethodDropdown.index - 1;

            if (methodIndex < 0 || methodIndex >= skipBindingMethods.Count)
            {
                return;
            }

            MethodBindingOption option = skipBindingMethods[methodIndex];

            if (option == null || !option.IsValid)
            {
                return;
            }

            if (!projectSettings.TrySetSkipBinding(option.StoredScriptName, option.StoredMethodName))
            {
                return;
            }

            RefreshSkipBindingStatus(projectSettings);
        }

        /// <summary>
        /// Refresh the displayed persistent Skip binding
        /// </summary>
        /// <param name="projectSettings"></param>
        private void RefreshSkipBindingStatus(TutorialToolProjectSettings projectSettings)
        {
            if (projectSettings == null || !projectSettings.HasSkipBinding)
            {
                skipBindingStatus.text = "Skip Current Step: Not configured";
                return;
            }

            skipBindingStatus.text = $"Skip Current Step: {projectSettings.SkipScriptName}.{projectSettings.SkipMethodName}()";
        }

        #endregion

        #region Tool Disposal

        /// <summary>
        /// Release every component created by the window
        /// </summary>
        private void DisposeTool()
        {
            assetCreationController?.Dispose();
            assetCreationController = null;

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
            assetCreationView = null;

            graphPersistenceService = null;
            injectionManifestService = null;
            graphReferenceResolver = null;
            graphRepository = null;

            stepAssetService = null;
            assetPathService = null;

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

            skipBindingScripts.Clear();
            skipBindingMethods.Clear();

            globalControlsRoot = null;
            skipTargetField = null;
            skipScriptDropdown = null;
            skipMethodDropdown = null;
            skipBindingStatus = null;

            contentHost = null;
            editorHost = null;
            toolbarHost = null;
            statusBarHost = null;
        }

        #endregion
    }
}