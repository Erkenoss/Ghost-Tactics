using System;
using Tutorial.Editor.Persistence;

namespace Tutorial.Editor.Core
{
    /// <summary>
    /// Store the temporary working state of one tutorial graph editor window
    /// </summary>
    public sealed class TutorialGraphSession
    {
        #region Private Fields

        /// <summary>
        /// Tutorial graph currently edited by the window
        /// </summary>
        private TutorialGraphAsset activeGraph = null;

        /// <summary>
        /// Whether the active graph contains unsaved modifications
        /// </summary>
        private bool isDirty = false;

        /// <summary>
        /// Number of currently active loading operations
        /// </summary>
        private int loadingDepth = 0;

        /// <summary>
        /// Number of explicit autosave suspensions
        /// </summary>
        private int autosaveSuspensionDepth = 0;

        /// <summary>
        /// Date of the last successful save operation
        /// </summary>
        private DateTime? lastSavedAt = null;

        /// <summary>
        /// Current information displayed by the tool
        /// </summary>
        private string statusMessage = string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Raised whenever the session state changes
        /// </summary>
        public event Action StateChanged = null;

        #endregion

        #region Properties

        /// <summary>
        /// Tutorial graph currently edited by the window
        /// </summary>
        public TutorialGraphAsset ActiveGraph => activeGraph;

        /// <summary>
        /// Whether the session currently contains an active graph
        /// </summary>
        public bool HasActiveGraph => activeGraph != null;

        /// <summary>
        /// Whether the active graph contains unsaved modifications
        /// </summary>
        public bool IsDirty => isDirty;

        /// <summary>
        /// Whether a graph loading operation is currently running
        /// </summary>
        public bool IsLoading => loadingDepth > 0;

        /// <summary>
        /// Whether automatic saves are currently suspended
        /// </summary>
        public bool IsAutosaveSuspended => IsLoading || autosaveSuspensionDepth > 0;

        /// <summary>
        /// Whether the active graph can currently be saved
        /// </summary>
        public bool CanSave => HasActiveGraph && !IsLoading;

        /// <summary>
        /// Whether an automatic save can currently be executed
        /// </summary>
        public bool CanAutosave => HasActiveGraph && IsDirty && !IsAutosaveSuspended;

        /// <summary>
        /// Date of the last successful save operation
        /// </summary>
        public DateTime? LastSavedAt => lastSavedAt;

        /// <summary>
        /// Current information displayed by the tool
        /// </summary>
        public string StatusMessage => statusMessage;

        #endregion

        #region Active Graph

        /// <summary>
        /// Assign a tutorial graph to this session
        /// </summary>
        /// <param name="graph"></param>
        public void SetActiveGraph(TutorialGraphAsset graph)
        {
            if (activeGraph == graph)
            {
                return;
            }

            activeGraph = graph;
            isDirty = false;
            loadingDepth = 0;
            autosaveSuspensionDepth = 0;
            lastSavedAt = null;
            statusMessage = string.Empty;

            NotifyStateChanged();
        }

        /// <summary>
        /// Remove the active tutorial graph from this session
        /// </summary>
        public void ClearActiveGraph()
        {
            if (activeGraph == null && !isDirty && loadingDepth == 0 && autosaveSuspensionDepth == 0 && lastSavedAt == null && string.IsNullOrWhiteSpace(statusMessage))
            {
                return;
            }

            activeGraph = null;
            isDirty = false;
            loadingDepth = 0;
            autosaveSuspensionDepth = 0;
            lastSavedAt = null;
            statusMessage = string.Empty;

            NotifyStateChanged();
        }

        #endregion

        #region Modification State

        /// <summary>
        /// Mark the active graph as modified
        /// </summary>
        public void MarkDirty()
        {
            if (!HasActiveGraph || IsLoading || isDirty)
            {
                return;
            }

            isDirty = true;

            NotifyStateChanged();
        }

        /// <summary>
        /// Mark the active graph as successfully saved
        /// </summary>
        public void MarkSaved()
        {
            if (!HasActiveGraph)
            {
                return;
            }

            isDirty = false;
            lastSavedAt = DateTime.UtcNow;

            NotifyStateChanged();
        }

        #endregion

        #region Loading State

        /// <summary>
        /// Begin a graph loading operation
        /// </summary>
        public void BeginLoading()
        {
            loadingDepth++;

            NotifyStateChanged();
        }

        /// <summary>
        /// Complete a graph loading operation
        /// </summary>
        public void EndLoading()
        {
            if (loadingDepth <= 0)
            {
                return;
            }

            loadingDepth--;

            NotifyStateChanged();
        }

        #endregion

        #region Autosave State

        /// <summary>
        /// Suspend automatic graph saves
        /// </summary>
        public void SuspendAutosave()
        {
            autosaveSuspensionDepth++;

            NotifyStateChanged();
        }

        /// <summary>
        /// Resume automatic graph saves
        /// </summary>
        public void ResumeAutosave()
        {
            if (autosaveSuspensionDepth <= 0)
            {
                return;
            }

            autosaveSuspensionDepth--;

            NotifyStateChanged();
        }

        #endregion

        #region Status

        /// <summary>
        /// Set the current session status message
        /// </summary>
        /// <param name="message"></param>
        public void SetStatusMessage(string message)
        {
            string normalizedMessage = message ?? string.Empty;

            if (string.Equals(statusMessage, normalizedMessage, StringComparison.Ordinal))
            {
                return;
            }

            statusMessage = normalizedMessage;

            NotifyStateChanged();
        }

        /// <summary>
        /// Clear the current session status message
        /// </summary>
        public void ClearStatusMessage()
        {
            SetStatusMessage(string.Empty);
        }

        #endregion

        #region Reset

        /// <summary>
        /// Reset the complete session state
        /// </summary>
        public void Reset()
        {
            ClearActiveGraph();
        }

        #endregion

        #region Notification

        /// <summary>
        /// Notify listeners that the session state has changed
        /// </summary>
        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        #endregion
    }
}