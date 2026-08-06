using System;
using UnityEngine;
using UnityEngine.UIElements;

using Tutorial.Editor.Services;

namespace Tutorial.Editor.Views
{
    /// <summary>
    /// Display and edit the folder used to create StepSequenceSO assets
    /// </summary>
    public sealed class TutorialSequenceFolderView
    {
        #region Constants

        private const string EmptyFolderLabel = "Not configured";

        #endregion

        #region Private Fields

        /// <summary>
        /// Service responsible for sequence asset creation
        /// </summary>
        private readonly TutorialSequenceAssetService sequenceAssetService = null;

        /// <summary>
        /// Root element of the view
        /// </summary>
        private readonly VisualElement root = null;

        /// <summary>
        /// Label displaying the configured folder path
        /// </summary>
        private readonly Label folderPathLabel = null;

        /// <summary>
        /// Button used to locate the configured folder
        /// </summary>
        private readonly Button locateButton = null;

        #endregion

        #region Properties

        public VisualElement Root => root;

        #endregion

        #region Constructor

        public TutorialSequenceFolderView(TutorialSequenceAssetService sequenceAssetService)
        {
            this.sequenceAssetService = sequenceAssetService ?? throw new ArgumentNullException(nameof(sequenceAssetService));

            root = new VisualElement();
            folderPathLabel = new Label();
            locateButton = new Button(LocateFolder)
            {
                text = "Locate"
            };

            BuildView();
            Refresh();
        }

        #endregion

        #region View

        /// <summary>
        /// Build the sequence folder toolbar
        /// </summary>
        private void BuildView()
        {
            root.name = "tutorial-sequence-folder-view";
            root.style.height = 28f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.paddingLeft = 6f;
            root.style.paddingRight = 6f;
            root.style.borderBottomWidth = 1f;
            root.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);

            Label title = new Label("Sequence folder:");

            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginRight = 6f;

            folderPathLabel.style.flexGrow = 1f;
            folderPathLabel.style.overflow = Overflow.Hidden;
            folderPathLabel.style.textOverflow = TextOverflow.Ellipsis;
            folderPathLabel.style.whiteSpace = WhiteSpace.NoWrap;

            Button changeButton = new Button(ChangeFolder)
            {
                text = "Change"
            };

            locateButton.style.marginLeft = 4f;
            changeButton.style.marginLeft = 4f;

            root.Add(title);
            root.Add(folderPathLabel);
            root.Add(locateButton);
            root.Add(changeButton);
        }

        /// <summary>
        /// Refresh the displayed folder information
        /// </summary>
        public void Refresh()
        {
            string folderPath = sequenceAssetService.GetSequenceFolderPath();
            bool hasValidFolder = !string.IsNullOrWhiteSpace(folderPath);

            folderPathLabel.text = hasValidFolder ? folderPath : EmptyFolderLabel;
            folderPathLabel.tooltip = hasValidFolder ? folderPath : "A folder will be requested when the first StepSequenceSO is created.";

            locateButton.SetEnabled(hasValidFolder);
        }

        #endregion

        #region Buttons

        /// <summary>
        /// Open the folder selector
        /// </summary>
        private void ChangeFolder()
        {
            if (!sequenceAssetService.TrySelectSequenceFolder())
            {
                return;
            }

            Refresh();
        }

        /// <summary>
        /// Locate the current folder inside the Project window
        /// </summary>
        private void LocateFolder()
        {
            sequenceAssetService.LocateSequenceFolder();
        }

        #endregion
    }
}