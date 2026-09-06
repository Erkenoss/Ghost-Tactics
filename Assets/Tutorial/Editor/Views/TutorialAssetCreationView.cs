using System;
using UnityEngine;
using UnityEngine.UIElements;

using Tutorial.Editor.Settings;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display tutorial asset folders and StepSO creation commands
    /// </summary>
    public sealed class TutorialAssetCreationView
    {
        #region Events

        public event Action<string> CreateStepRequested = null;
        public event Action<string> CreateSequenceRequested = null;

        #endregion

        #region Private Fields

        private readonly TutorialToolProjectSettings settings = null;

        private readonly VisualElement root = null;
        private readonly TextField stepFolderField = null;
        private readonly TextField sequenceFolderField = null;
        private readonly TextField stepNameField = null;
        private readonly TextField sequenceNameField = null;

        #endregion

        #region Properties

        public VisualElement Root => root;

        #endregion

        #region Constructor

        public TutorialAssetCreationView(TutorialToolProjectSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            root = new VisualElement();

            stepFolderField = new TextField
            {
                value = settings.StepFolderPath,
                isDelayed = true
            };

            sequenceFolderField = new TextField
            {
                value = settings.SequenceFolderPath,
                isDelayed = true
            };

            stepNameField = new TextField();
            sequenceNameField = new TextField();

            BuildView();
            RegisterCallbacks();
        }

        #endregion

        #region View

        private void BuildView()
        {
            root.name = "tutorial-asset-creation-view";
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 6f;
            root.style.paddingRight = 6f;
            root.style.paddingTop = 4f;
            root.style.paddingBottom = 4f;
            root.style.borderBottomWidth = 1f;
            root.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);

            root.Add(CreatePathRow("Step folder:", stepFolderField));
            root.Add(CreatePathRow("Sequence folder:", sequenceFolderField));
            root.Add(CreateStepCreationRow());
            root.Add(CreateSequenceCreationRow());
        }

        private static VisualElement CreatePathRow(string labelText, TextField field)
        {
            VisualElement row = new VisualElement();

            row.style.height = 24f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            Label label = new Label(labelText);

            label.style.width = 110f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            field.style.flexGrow = 1f;

            row.Add(label);
            row.Add(field);

            return row;
        }

        private VisualElement CreateSequenceCreationRow()
        {
            VisualElement row = new VisualElement();

            row.style.height = 26f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            Label label = new Label("Sequence name:");

            label.style.width = 110f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            sequenceNameField.style.flexGrow = 1f;

            Button createButton = new Button(RequestSequenceCreation)
            {
                text = "Create Tutorial Sequence"
            };

            createButton.style.marginLeft = 6f;

            row.Add(label);
            row.Add(sequenceNameField);
            row.Add(createButton);

            return row;
        }

        private VisualElement CreateStepCreationRow()
        {
            VisualElement row = new VisualElement();

            row.style.height = 26f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            Label label = new Label("Step name:");

            label.style.width = 110f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            stepNameField.style.flexGrow = 1f;

            Button createButton = new Button(RequestStepCreation)
            {
                text = "Create Tutorial Step"
            };

            createButton.style.marginLeft = 6f;

            row.Add(label);
            row.Add(stepNameField);
            row.Add(createButton);

            return row;
        }

        #endregion

        #region Callbacks

        private void RegisterCallbacks()
        {
            stepFolderField.RegisterValueChangedCallback(OnStepFolderChanged);
            sequenceFolderField.RegisterValueChangedCallback(OnSequenceFolderChanged);
        }

        private void OnStepFolderChanged(ChangeEvent<string> changeEvent)
        {
            if (settings.TrySetStepFolder(changeEvent.newValue))
            {
                stepFolderField.SetValueWithoutNotify(settings.StepFolderPath);

                return;
            }

            Debug.LogWarning($"Invalid tutorial Step folder path: '{changeEvent.newValue}'.");
            stepFolderField.SetValueWithoutNotify(settings.StepFolderPath);
        }

        private void OnSequenceFolderChanged(ChangeEvent<string> changeEvent)
        {
            if (settings.TrySetSequenceFolder(changeEvent.newValue))
            {
                sequenceFolderField.SetValueWithoutNotify(settings.SequenceFolderPath);

                return;
            }

            Debug.LogWarning($"Invalid tutorial Sequence folder path: '{changeEvent.newValue}'.");
            sequenceFolderField.SetValueWithoutNotify(settings.SequenceFolderPath);
        }

        private void RequestStepCreation()
        {
            CreateStepRequested?.Invoke(stepNameField.value);
        }

        private void RequestSequenceCreation()
        {
            CreateSequenceRequested?.Invoke(sequenceNameField.value);
        }

        #endregion

        #region Public Methods

        public void ClearStepName()
        {
            stepNameField.SetValueWithoutNotify(string.Empty);
            stepNameField.Focus();
        }

        public void ClearSequenceName()
        {
            sequenceNameField.SetValueWithoutNotify(string.Empty);
            sequenceNameField.Focus();
        }

        #endregion
    }
}