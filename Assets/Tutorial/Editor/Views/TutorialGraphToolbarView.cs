using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display the commands used to select, open, save and locate a tutorial graph
    /// </summary>
    public sealed class TutorialGraphToolbarView
    {
        #region Events

        /// <summary>
        /// Raised when the selected graph changes
        /// </summary>
        public event Action<UnityObject> GraphSelectionChanged = null;

        /// <summary>
        /// Raised when graph opening is requested
        /// </summary>
        public event Action OpenRequested = null;

        /// <summary>
        /// Raised when graph saving is requested
        /// </summary>
        public event Action SaveRequested = null;

        /// <summary>
        /// Raised when graph location is requested
        /// </summary>
        public event Action LocateRequested = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Root visual element of the toolbar
        /// </summary>
        public VisualElement Root { get; } = null;

        /// <summary>
        /// Currently selected graph asset
        /// </summary>
        public UnityObject SelectedGraph
        {
            get { return graphField.value; }
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Field containing the graph selected by the user
        /// </summary>
        private readonly ObjectField graphField = null;

        /// <summary>
        /// Button used to open the selected graph
        /// </summary>
        private readonly ToolbarButton openButton = null;

        /// <summary>
        /// Button used to save the active graph
        /// </summary>
        private readonly ToolbarButton saveButton = null;

        /// <summary>
        /// Button used to locate the selected or active graph
        /// </summary>
        private readonly ToolbarButton locateButton = null;

        #endregion

        #region Constructor

        public TutorialGraphToolbarView(Type graphAssetType)
        {
            if (graphAssetType == null)
            {
                throw new ArgumentNullException(nameof(graphAssetType));
            }

            Root = new Toolbar
            {
                name = "tutorial-graph-toolbar"
            };

            Label graphLabel = new Label("Graph");
            graphLabel.style.marginLeft = 6f;
            graphLabel.style.marginRight = 4f;
            graphLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleLeft;

            graphField = new ObjectField
            {
                name = "tutorial-graph-field",
                objectType = graphAssetType,
                allowSceneObjects = false
            };

            graphField.style.width = 280f;
            graphField.RegisterValueChangedCallback(OnGraphFieldChanged);

            openButton = new ToolbarButton(OnOpenClicked)
            {
                text = "Open",
                tooltip = "Open and reconstruct the selected tutorial graph"
            };

            saveButton = new ToolbarButton(OnSaveClicked)
            {
                text = "Save",
                tooltip = "Immediately save the active tutorial graph"
            };

            locateButton = new ToolbarButton(OnLocateClicked)
            {
                text = "Locate",
                tooltip = "Locate the selected tutorial graph in the Project window"
            };

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;

            Root.Add(graphLabel);
            Root.Add(graphField);
            Root.Add(openButton);
            Root.Add(saveButton);
            Root.Add(locateButton);
            Root.Add(spacer);

            SetCommandAvailability(false, false, false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the graph displayed inside the object field
        /// </summary>
        /// <param name="graph"></param>
        public void SetSelectedGraph(UnityObject graph)
        {
            graphField.SetValueWithoutNotify(graph);
        }

        /// <summary>
        /// Update graph command availability
        /// </summary>
        /// <param name="canOpen"></param>
        /// <param name="canSave"></param>
        /// <param name="canLocate"></param>
        public void SetCommandAvailability(bool canOpen, bool canSave, bool canLocate)
        {
            openButton.SetEnabled(canOpen);
            saveButton.SetEnabled(canSave);
            locateButton.SetEnabled(canLocate);
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Notify that the selected graph has changed
        /// </summary>
        /// <param name="changeEvent"></param>
        private void OnGraphFieldChanged(ChangeEvent<UnityObject> changeEvent)
        {
            GraphSelectionChanged?.Invoke(changeEvent.newValue);
        }

        /// <summary>
        /// Notify that graph opening was requested
        /// </summary>
        private void OnOpenClicked()
        {
            OpenRequested?.Invoke();
        }

        /// <summary>
        /// Notify that graph saving was requested
        /// </summary>
        private void OnSaveClicked()
        {
            SaveRequested?.Invoke();
        }

        /// <summary>
        /// Notify that graph location was requested
        /// </summary>
        private void OnLocateClicked()
        {
            LocateRequested?.Invoke();
        }

        #endregion
    }
}