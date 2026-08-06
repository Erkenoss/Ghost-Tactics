using System;
using UnityEditor;
using UnityEngine;

using Tutorial.Editor.Core;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Schedule and execute delayed tutorial graph saves
    /// </summary>
    internal sealed class TutorialGraphAutosaveService : IDisposable
    {
        #region Constants

        /// <summary>
        /// Default delay applied before saving a modified graph
        /// </summary>
        private const double DefaultSaveDelay = 1.25d;

        #endregion

        #region Events

        /// <summary>
        /// Raised after a successful automatic save
        /// </summary>
        public event Action Saved = null;

        /// <summary>
        /// Raised when an automatic save fails
        /// </summary>
        public event Action<string> SaveFailed = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether an automatic save is currently scheduled
        /// </summary>
        public bool HasPendingSave
        {
            get { return hasPendingSave; }
        }

        /// <summary>
        /// Whether the service is currently enabled
        /// </summary>
        public bool IsEnabled
        {
            get { return isEnabled; }
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Current tutorial graph editing session
        /// </summary>
        private readonly TutorialGraphSession graphSession = null;

        /// <summary>
        /// Service responsible for serializing the active graph
        /// </summary>
        private readonly TutorialGraphPersistenceService persistenceService = null;

        /// <summary>
        /// Delay applied before an automatic save
        /// </summary>
        private readonly double saveDelay = DefaultSaveDelay;

        /// <summary>
        /// Editor time at which the pending save can be executed
        /// </summary>
        private double scheduledSaveTime = -1d;

        /// <summary>
        /// Whether the service is currently listening to Editor updates
        /// </summary>
        private bool isEnabled = false;

        /// <summary>
        /// Whether a graph save is scheduled
        /// </summary>
        private bool hasPendingSave = false;

        /// <summary>
        /// Whether a graph save is currently being executed
        /// </summary>
        private bool isSaving = false;

        #endregion

        #region Constructor

        public TutorialGraphAutosaveService(TutorialGraphSession graphSession, TutorialGraphPersistenceService persistenceService, double saveDelay = DefaultSaveDelay)
        {
            this.graphSession = graphSession ?? throw new ArgumentNullException(nameof(graphSession));
            this.persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            this.saveDelay = Math.Max(0.1d, saveDelay);
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Start listening to Editor updates
        /// </summary>
        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            EditorApplication.update += OnEditorUpdate;

            isEnabled = true;
        }

        /// <summary>
        /// Stop listening to Editor updates
        /// </summary>
        public void Dispose()
        {
            if (isEnabled)
            {
                EditorApplication.update -= OnEditorUpdate;
            }

            isEnabled = false;
            isSaving = false;

            CancelPendingSave();
        }

        #endregion

        #region Save Scheduling

        /// <summary>
        /// Mark the active graph as modified and schedule an automatic save
        /// </summary>
        public void RequestSave()
        {
            if (graphSession.IsLoading || !graphSession.HasActiveGraph)
            {
                return;
            }

            graphSession.MarkDirty();

            if (!isEnabled)
            {
                return;
            }

            hasPendingSave = true;
            scheduledSaveTime = EditorApplication.timeSinceStartup + saveDelay;
        }

        /// <summary>
        /// Cancel the currently scheduled automatic save
        /// </summary>
        public void CancelPendingSave()
        {
            hasPendingSave = false;
            scheduledSaveTime = -1d;
        }

        #endregion

        #region Immediate Save

        /// <summary>
        /// Immediately save the active graph when it contains unsaved changes
        /// </summary>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TryFlush(out string failureReason)
        {
            failureReason = string.Empty;

            if (!hasPendingSave && !graphSession.IsDirty)
            {
                return true;
            }

            return TrySaveNow(out failureReason);
        }

        /// <summary>
        /// Immediately execute the pending graph save
        /// </summary>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public bool TrySaveNow(out string failureReason)
        {
            failureReason = string.Empty;

            if (isSaving)
            {
                failureReason = "A tutorial graph save is already in progress.";

                return false;
            }

            if (!graphSession.HasActiveGraph)
            {
                failureReason = "No active tutorial graph can be saved.";

                return false;
            }

            if (graphSession.IsLoading)
            {
                failureReason = "The tutorial graph cannot be saved while it is loading.";

                return false;
            }

            if (graphSession.IsAutosaveSuspended)
            {
                failureReason = "Tutorial graph autosave is currently suspended.";

                return false;
            }

            if (!graphSession.IsDirty)
            {
                CancelPendingSave();

                return true;
            }

            isSaving = true;

            try
            {
                if (!persistenceService.TrySaveActiveGraph(out failureReason))
                {
                    CancelPendingSave();
                    SaveFailed?.Invoke(failureReason);

                    return false;
                }

                CancelPendingSave();
                Saved?.Invoke();

                return true;
            }
            finally
            {
                isSaving = false;
            }
        }

        #endregion

        #region Editor Update

        /// <summary>
        /// Execute the pending save after the configured delay
        /// </summary>
        private void OnEditorUpdate()
        {
            if (!isEnabled || !hasPendingSave || isSaving)
            {
                return;
            }

            if (!graphSession.HasActiveGraph)
            {
                CancelPendingSave();

                return;
            }

            if (!graphSession.IsDirty)
            {
                CancelPendingSave();

                return;
            }

            if (graphSession.IsLoading || graphSession.IsAutosaveSuspended)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < scheduledSaveTime)
            {
                return;
            }

            if (!TrySaveNow(out string failureReason) && !string.IsNullOrWhiteSpace(failureReason))
            {
                Debug.LogError($"Tutorial graph autosave failed: {failureReason}");
            }
        }

        #endregion
    }
}