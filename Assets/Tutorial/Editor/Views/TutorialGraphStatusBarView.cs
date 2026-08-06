using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Importance level of a tutorial graph status message
    /// </summary>
    public enum ETutorialGraphStatus
    {
        Normal,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Display information about the active tutorial graph
    /// </summary>
    public sealed class TutorialGraphStatusBarView
    {
        #region Colors

        private static readonly Color NormalColor = new Color(0.72f, 0.72f, 0.72f);
        private static readonly Color SuccessColor = new Color(0.35f, 0.8f, 0.45f);
        private static readonly Color WarningColor = new Color(0.95f, 0.7f, 0.25f);
        private static readonly Color ErrorColor = new Color(0.95f, 0.35f, 0.35f);

        #endregion

        #region Public Properties

        /// <summary>
        /// Root visual element of the status bar
        /// </summary>
        public VisualElement Root { get; } = null;

        #endregion

        #region Private Fields

        /// <summary>
        /// Label displaying the active graph
        /// </summary>
        private readonly Label graphLabel = null;

        /// <summary>
        /// Label displaying the current graph state
        /// </summary>
        private readonly Label statusLabel = null;

        /// <summary>
        /// Label displaying the graph node count
        /// </summary>
        private readonly Label nodeCountLabel = null;

        /// <summary>
        /// Label displaying autosave state
        /// </summary>
        private readonly Label autosaveLabel = null;

        #endregion

        #region Constructor

        public TutorialGraphStatusBarView()
        {
            Root = new VisualElement
            {
                name = "tutorial-graph-status-bar"
            };

            Root.style.height = 22f;
            Root.style.minHeight = 22f;
            Root.style.flexDirection = FlexDirection.Row;
            Root.style.alignItems = Align.Center;
            Root.style.paddingLeft = 8f;
            Root.style.paddingRight = 8f;
            Root.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f);
            Root.style.borderTopWidth = 1f;
            Root.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f);

            graphLabel = CreateLabel("No graph");
            statusLabel = CreateLabel("Idle");
            nodeCountLabel = CreateLabel("0 nodes");
            autosaveLabel = CreateLabel("Autosave: Off");

            graphLabel.style.minWidth = 220f;
            statusLabel.style.flexGrow = 1f;

            Root.Add(graphLabel);
            Root.Add(CreateSeparator());
            Root.Add(statusLabel);
            Root.Add(CreateSeparator());
            Root.Add(nodeCountLabel);
            Root.Add(CreateSeparator());
            Root.Add(autosaveLabel);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Display the absence of an active graph
        /// </summary>
        public void DisplayNoGraph()
        {
            graphLabel.text = "No graph";
            nodeCountLabel.text = "0 nodes";

            SetStatus("Select a TutorialGraphAsset", ETutorialGraphStatus.Normal);
        }

        /// <summary>
        /// Display the active graph and its node count
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="nodeCount"></param>
        public void DisplayGraph(UnityObject graph, int nodeCount)
        {
            graphLabel.text = graph != null ? $"Graph: {graph.name}" : "No graph";

            SetNodeCount(nodeCount);
        }

        /// <summary>
        /// Update the displayed node count
        /// </summary>
        /// <param name="nodeCount"></param>
        public void SetNodeCount(int nodeCount)
        {
            int validatedCount = Mathf.Max(0, nodeCount);
            nodeCountLabel.text = validatedCount == 1 ? "1 node" : $"{validatedCount} nodes";
        }

        /// <summary>
        /// Update autosave display
        /// </summary>
        /// <param name="isEnabled"></param>
        public void SetAutosaveEnabled(bool isEnabled)
        {
            autosaveLabel.text = isEnabled ? "Autosave: On" : "Autosave: Off";
            autosaveLabel.style.color = isEnabled ? SuccessColor : NormalColor;
        }

        /// <summary>
        /// Display a status message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="status"></param>
        public void SetStatus(string message, ETutorialGraphStatus status)
        {
            statusLabel.text = string.IsNullOrWhiteSpace(message) ? "Idle" : message;

            switch (status)
            {
                case ETutorialGraphStatus.Success:
                    statusLabel.style.color = SuccessColor;
                    break;

                case ETutorialGraphStatus.Warning:
                    statusLabel.style.color = WarningColor;
                    break;

                case ETutorialGraphStatus.Error:
                    statusLabel.style.color = ErrorColor;
                    break;

                default:
                    statusLabel.style.color = NormalColor;
                    break;
            }
        }

        #endregion

        #region Creation

        /// <summary>
        /// Create a status bar label
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Label CreateLabel(string text)
        {
            Label label = new Label(text);
            label.style.marginLeft = 4f;
            label.style.marginRight = 4f;
            label.style.color = NormalColor;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;

            return label;
        }

        /// <summary>
        /// Create a visual separator
        /// </summary>
        /// <returns></returns>
        private static VisualElement CreateSeparator()
        {
            VisualElement separator = new VisualElement();
            separator.style.width = 1f;
            separator.style.height = 14f;
            separator.style.marginLeft = 5f;
            separator.style.marginRight = 5f;
            separator.style.backgroundColor = new Color(0.32f, 0.32f, 0.32f);

            return separator;
        }

        #endregion
    }
}