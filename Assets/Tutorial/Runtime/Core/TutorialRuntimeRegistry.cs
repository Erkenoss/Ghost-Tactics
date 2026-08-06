using System;
using System.Collections.Generic;

namespace Tutorial.Runtime.Core
{
    /// <summary>
    /// Store and manage the active runtime tutorial instances
    /// </summary>
    public sealed class TutorialRuntimeRegistry : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Active runtime tutorial instances indexed by their tutorial GUID
        /// </summary>
        private readonly Dictionary<string, TutorialRuntimeInstance> runtimeInstances = new Dictionary<string, TutorialRuntimeInstance>(StringComparer.Ordinal);

        /// <summary>
        /// Whether this registry has already released its instances
        /// </summary>
        private bool isDisposed = false;

        #endregion

        #region Properties

        public int Count => runtimeInstances.Count;
        public bool IsDisposed => isDisposed;

        #endregion

        #region Public Methods

        /// <summary>
        /// Register one runtime tutorial instance
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <returns></returns>
        public bool TryRegister(TutorialRuntimeInstance runtimeInstance)
        {
            if (isDisposed || !IsRegistrable(runtimeInstance))
            {
                return false;
            }

            string tutorialGuid = runtimeInstance.TutorialGuid;

            if (runtimeInstances.TryGetValue(tutorialGuid, out TutorialRuntimeInstance registeredInstance))
            {
                if (registeredInstance != null && !registeredInstance.IsDisposed)
                {
                    return false;
                }

                runtimeInstances.Remove(tutorialGuid);
            }

            runtimeInstances.Add(tutorialGuid, runtimeInstance);

            return true;
        }

        /// <summary>
        /// Retrieve one active runtime tutorial instance
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <param name="runtimeInstance"></param>
        /// <returns></returns>
        public bool TryGet(string tutorialGuid, out TutorialRuntimeInstance runtimeInstance)
        {
            runtimeInstance = null;

            if (isDisposed || string.IsNullOrWhiteSpace(tutorialGuid))
            {
                return false;
            }

            if (!runtimeInstances.TryGetValue(tutorialGuid, out TutorialRuntimeInstance registeredInstance))
            {
                return false;
            }

            if (registeredInstance == null || registeredInstance.IsDisposed)
            {
                runtimeInstances.Remove(tutorialGuid);

                return false;
            }

            runtimeInstance = registeredInstance;

            return true;
        }

        /// <summary>
        /// Check whether one active runtime tutorial instance is registered
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <returns></returns>
        public bool Contains(string tutorialGuid)
        {
            return TryGet(tutorialGuid, out _);
        }

        /// <summary>
        /// Remove and dispose one runtime tutorial instance
        /// </summary>
        /// <param name="tutorialGuid"></param>
        /// <returns></returns>
        public bool TryRemove(string tutorialGuid)
        {
            if (isDisposed || string.IsNullOrWhiteSpace(tutorialGuid))
            {
                return false;
            }

            if (!runtimeInstances.Remove(tutorialGuid, out TutorialRuntimeInstance runtimeInstance))
            {
                return false;
            }

            runtimeInstance?.Dispose();

            return true;
        }

        /// <summary>
        /// Remove and dispose every registered runtime tutorial instance
        /// </summary>
        public void Clear()
        {
            if (isDisposed)
            {
                return;
            }

            foreach (TutorialRuntimeInstance runtimeInstance in runtimeInstances.Values)
            {
                runtimeInstance?.Dispose();
            }

            runtimeInstances.Clear();
        }

        /// <summary>
        /// Release this registry and every runtime instance it owns
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            Clear();
            isDisposed = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check whether a runtime instance can be owned by this registry
        /// </summary>
        /// <param name="runtimeInstance"></param>
        /// <returns></returns>
        private static bool IsRegistrable(TutorialRuntimeInstance runtimeInstance)
        {
            if (runtimeInstance == null || runtimeInstance.IsDisposed || string.IsNullOrWhiteSpace(runtimeInstance.TutorialGuid))
            {
                return false;
            }

            return runtimeInstance.Status == ETutorialRuntimeInstanceStatus.Ready;
        }

        #endregion
    }
}
