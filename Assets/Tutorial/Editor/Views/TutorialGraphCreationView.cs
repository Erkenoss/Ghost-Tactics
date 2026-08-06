using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display the tutorial graph creation screen
    /// </summary>
    internal sealed class TutorialGraphCreationView
    {
        #region Constants

        private const string DefaultGraphName = "TutorialGraph";

        #endregion

        #region Events

        /// <summary>
        /// Raised when graph creation is confirmed
        /// </summary>
        public event Action<string> CreateRequested = null;

        /// <summary>
        /// Raised when returning to the launcher is requested
        /// </summary>
        public event Action BackRequested = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Root visual element of the creation screen
        /// </summary>
        public VisualElement Root { get; } = null;

        #endregion

        #region Private Fields

        private readonly TextField graphNameField = null;
        private readonly Button createButton = null;

        #endregion

        #region Constructor

        public TutorialGraphCreationView()
        {
            Root = new VisualElement
            {
                name = "tutorial-graph-creation"
            };

            Root.style.flexGrow = 1f;
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;
            Root.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            VisualElement card = CreateCard();

            Label title = new Label("Create Tutorial Graph");
            title.style.fontSize = 24f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 20f;

            graphNameField = new TextField("Graph Name")
            {
                name = "tutorial-graph-name-field",
                value = DefaultGraphName
            };

            graphNameField.style.marginBottom = 16f;
            graphNameField.RegisterValueChangedCallback(OnGraphNameChanged);

            createButton = new Button(OnCreateClicked)
            {
                name = "tutorial-confirm-create-button",
                text = "Choose Location and Create"
            };

            Button backButton = new Button(OnBackClicked)
            {
                name = "tutorial-creation-back-button",
                text = "Back"
            };

            ConfigureButton(createButton);
            ConfigureButton(backButton);

            card.Add(title);
            card.Add(graphNameField);
            card.Add(createButton);
            card.Add(backButton);

            Root.Add(card);

            Hide();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the creation screen
        /// </summary>
        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            graphNameField.value = DefaultGraphName;
            createButton.SetEnabled(true);

            graphNameField.schedule.Execute(() =>
            {
                graphNameField.Focus();
                graphNameField.SelectAll();
            });
        }

        /// <summary>
        /// Hide the creation screen
        /// </summary>
        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Enable or disable creation commands
        /// </summary>
        /// <param name="isEnabled"></param>
        public void SetCommandsEnabled(bool isEnabled)
        {
            graphNameField.SetEnabled(isEnabled);
            createButton.SetEnabled(isEnabled && !string.IsNullOrWhiteSpace(graphNameField.value));
        }

        #endregion

        #region Creation

        private static VisualElement CreateCard()
        {
            VisualElement card = new VisualElement
            {
                name = "tutorial-graph-creation-card"
            };

            card.style.width = 460f;
            card.style.paddingLeft = 32f;
            card.style.paddingRight = 32f;
            card.style.paddingTop = 30f;
            card.style.paddingBottom = 30f;
            card.style.backgroundColor = new Color(0.19f, 0.19f, 0.19f);

            Color borderColor = new Color(0.32f, 0.32f, 0.32f);

            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;

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

        private static void ConfigureButton(Button button)
        {
            button.style.height = 34f;
            button.style.marginTop = 4f;
            button.style.marginBottom = 4f;
        }

        #endregion

        #region Callbacks

        private void OnGraphNameChanged(ChangeEvent<string> changeEvent)
        {
            createButton.SetEnabled(!string.IsNullOrWhiteSpace(changeEvent.newValue));
        }

        private void OnCreateClicked()
        {
            string graphName = graphNameField.value?.Trim();

            if (string.IsNullOrWhiteSpace(graphName))
            {
                return;
            }

            CreateRequested?.Invoke(graphName);
        }

        private void OnBackClicked()
        {
            BackRequested?.Invoke();
        }

        #endregion
    }
}