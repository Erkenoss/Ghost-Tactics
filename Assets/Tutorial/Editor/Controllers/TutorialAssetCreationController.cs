using System;
using UnityEngine;

using Tutorial.Runtime.Data;
using Tutorial.Editor.Services;
using Tutorial.Editor.Views;

namespace Tutorial.Editor.Controllers
{
    /// <summary>
    /// Orchestrate StepSO creation from the tutorial editor
    /// </summary>
    internal sealed class TutorialAssetCreationController : IDisposable
    {
        #region Private Fields

        private readonly TutorialAssetCreationView creationView = null;
        private readonly TutorialStepAssetService stepAssetService = null;
        private readonly TutorialCanvasController canvasController = null;
        private readonly TutorialSessionController sessionController = null;
        private readonly TutorialSequenceAssetService sequenceAssetService = null;

        private bool isEnabled = false;

        #endregion

        #region Constructor

        public TutorialAssetCreationController(TutorialAssetCreationView creationView, TutorialStepAssetService stepAssetService, TutorialSequenceAssetService sequenceAssetService, TutorialCanvasController canvasController, TutorialSessionController sessionController)
        {
            this.creationView = creationView ?? throw new ArgumentNullException(nameof(creationView));
            this.stepAssetService = stepAssetService ?? throw new ArgumentNullException(nameof(stepAssetService));
            this.sequenceAssetService = sequenceAssetService ?? throw new ArgumentNullException(nameof(sequenceAssetService));
            this.canvasController = canvasController ?? throw new ArgumentNullException(nameof(canvasController));
            this.sessionController = sessionController ?? throw new ArgumentNullException(nameof(sessionController));
        }

        #endregion

        #region Lifecycle

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            creationView.CreateStepRequested += OnCreateStepRequested;
            creationView.CreateSequenceRequested += OnCreateSequenceRequested;

            isEnabled = true;
        }

        public void Dispose()
        {
            if (!isEnabled)
            {
                return;
            }

            creationView.CreateStepRequested -= OnCreateStepRequested;
            creationView.CreateSequenceRequested -= OnCreateSequenceRequested;

            isEnabled = false;
        }

        #endregion

        #region Creation

        private void OnCreateStepRequested(string stepName)
        {
            if (!sessionController.HasActiveGraph)
            {
                Debug.LogWarning("A TutorialGraphAsset must be opened before creating a Tutorial Step.");

                return;
            }

            if (!stepAssetService.TryCreateStepAsset(stepName, out StepSO step, out string failureReason))
            {
                Debug.LogError(failureReason);

                return;
            }

            if (!canvasController.TryAddStep(step, out failureReason))
            {
                Debug.LogError(failureReason, step);

                return;
            }

            creationView.ClearStepName();
        }

        private void OnCreateSequenceRequested(string sequenceName)
        {
            if (!sessionController.HasActiveGraph)
            {
                Debug.LogWarning("A TutorialGraphAsset must be opened before creating a Tutorial Sequence.");

                return;
            }

            if (!sequenceAssetService.TryCreateSequenceAsset(sequenceName, out StepSequenceSO sequence, out string failureReason))
            {
                Debug.LogError(failureReason);

                return;
            }

            if (!canvasController.TryAddStep(sequence, out failureReason))
            {
                Debug.LogError(failureReason, sequence);

                return;
            }

            creationView.ClearSequenceName();
        }

        #endregion
    }
}