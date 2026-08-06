using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display the tutorial graph welcome screen when no graph is currently opened
    /// </summary>
    internal sealed class TutorialGraphLauncherView
    {
        #region Events

        /// <summary>
        /// Raised when graph creation is requested
        /// </summary>
        public event Action CreateRequested = null;

        /// <summary>
        /// Raised when graph browsing is requested
        /// </summary>
        public event Action BrowseRequested = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Root visual element of the launcher
        /// </summary>
        public VisualElement Root { get; } = null;

        #endregion

        #region Private Fields

        /// <summary>
        /// Button used to request graph creation
        /// </summary>
        private readonly Button createButton = null;

        /// <summary>
        /// Button used to request graph browsing
        /// </summary>
        private readonly Button browseButton = null;

        #endregion

        #region Constructor

        public TutorialGraphLauncherView()
        {
            Root = new VisualElement
            {
                name = "tutorial-graph-launcher"
            };

            Root.style.flexGrow = 1f;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            VisualElement card = CreateCard();

            Label title = new Label("Tutorial Tool");
            title.style.fontSize = 28f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 8f;

            Label description = new Label("Create a new tutorial graph or open an existing one.");
            description.style.fontSize = 13f;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            description.style.color = new Color(0.72f, 0.72f, 0.72f);
            description.style.marginBottom = 24f;

            createButton = new Button(OnCreateClicked)
            {
                name = "tutorial-create-graph-button",
                text = "Create New Graph",
                tooltip = "Create a new TutorialGraphAsset"
            };

            browseButton = new Button(OnBrowseClicked)
            {
                name = "tutorial-browse-graphs-button",
                text = "Browse Graphs",
                tooltip = "Browse existing TutorialGraphAsset assets"
            };

            ConfigureButton(createButton);
            ConfigureButton(browseButton);

            card.Add(title);
            card.Add(description);
            card.Add(createButton);
            card.Add(browseButton);

            Root.Add(card);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the launcher
        /// </summary>
        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hide the launcher
        /// </summary>
        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Enable or disable launcher commands
        /// </summary>
        /// <param name="isEnabled"></param>
        public void SetCommandsEnabled(bool isEnabled)
        {
            createButton.SetEnabled(isEnabled);
            browseButton.SetEnabled(isEnabled);
        }

        #endregion

        #region Creation

        /// <summary>
        /// Create the launcher central card
        /// </summary>
        /// <returns></returns>
        private static VisualElement CreateCard()
        {
            VisualElement card = new VisualElement
            {
                name = "tutorial-graph-launcher-card"
            };

            card.style.width = 440f;
            card.style.maxWidth = 520f;
            card.style.paddingLeft = 32f;
            card.style.paddingRight = 32f;
            card.style.paddingTop = 30f;
            card.style.paddingBottom = 30f;
            card.style.backgroundColor = new Color(0.19f, 0.19f, 0.19f);

            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;

            Color borderColor = new Color(0.32f, 0.32f, 0.32f);

            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;

            card.style.borderTopLeftRadius = 6f;
            card.style.borderTopRightRadius = 6f;
            card.style.borderBottomLeftRadius = 6f;
            card.style.borderBottomRightRadius = 6f;

            return card;
        }

        /// <summary>
        /// Apply the common launcher button style
        /// </summary>
        /// <param name="button"></param>
        private static void ConfigureButton(Button button)
        {
            button.style.height = 36f;
            button.style.marginTop = 4f;
            button.style.marginBottom = 4f;
            button.style.fontSize = 13f;
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Notify that graph creation was requested
        /// </summary>
        private void OnCreateClicked()
        {
            CreateRequested?.Invoke();
        }

        /// <summary>
        /// Notify that graph browsing was requested
        /// </summary>
        private void OnBrowseClicked()
        {
            BrowseRequested?.Invoke();
        }

        #endregion
    }
}