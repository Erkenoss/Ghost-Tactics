using System;
using System.Collections.Generic;
using Tutorial.Runtime.Components;
using UnityEngine;

namespace Tutorial.Runtime.Resolution
{
    /// <summary>
    /// Store and resolve every TutoIdentifier currently available at runtime
    /// </summary>
    public sealed class TutorialIdentifierRegistry
    {
        #region Private Fields

        /// <summary>
        /// Shared runtime registry used by every tutorial system
        /// </summary>
        private static readonly TutorialIdentifierRegistry instance = new TutorialIdentifierRegistry();

        /// <summary>
        /// Available tutorial identifiers indexed by their persistent object GUID
        /// </summary>
        private readonly Dictionary<string, TutoIdentifier> identifiers = new Dictionary<string, TutoIdentifier>(StringComparer.Ordinal);

        #endregion

        #region Properties

        public static TutorialIdentifierRegistry Instance => instance;
        public int Count => identifiers.Count;

        #endregion

        #region Constructor

        /// <summary>
        /// Create the shared tutorial identifier registry
        /// </summary>
        public TutorialIdentifierRegistry()
        {
        }

        #endregion

        #region Runtime Initialization

        /// <summary>
        /// Reset the shared registry before runtime scene objects become available
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Register one TutoIdentifier currently available at runtime
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool TryRegister(TutoIdentifier identifier, out string error)
        {
            error = string.Empty;

            if (identifier == null)
            {
                error = "The TutoIdentifier to register is null.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(identifier.ObjectGUID))
            {
                error = $"TutoIdentifier on GameObject '{identifier.gameObject.name}' contains no object GUID.";

                return false;
            }

            if (!identifier.isActiveAndEnabled)
            {
                error = $"TutoIdentifier on GameObject '{identifier.gameObject.name}' is not currently available.";

                return false;
            }

            string objectGuid = identifier.ObjectGUID;

            if (identifiers.TryGetValue(objectGuid, out TutoIdentifier registeredIdentifier))
            {
                if (registeredIdentifier == null)
                {
                    identifiers.Remove(objectGuid);
                }
                else if (ReferenceEquals(registeredIdentifier, identifier))
                {
                    return true;
                }
                else
                {
                    error = $"Tutorial object GUID '{objectGuid}' is already registered by GameObject '{registeredIdentifier.gameObject.name}' and cannot also be registered by '{identifier.gameObject.name}'.";

                    return false;
                }
            }

            identifiers.Add(objectGuid, identifier);

            return true;
        }

        /// <summary>
        /// Remove one TutoIdentifier from the runtime registry
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool TryUnregister(TutoIdentifier identifier)
        {
            if (identifier == null)
            {
                return false;
            }

            string objectGuid = identifier.ObjectGUID;

            if (!string.IsNullOrWhiteSpace(objectGuid) && identifiers.TryGetValue(objectGuid, out TutoIdentifier registeredIdentifier))
            {
                if (ReferenceEquals(registeredIdentifier, identifier))
                {
                    identifiers.Remove(objectGuid);

                    return true;
                }

                return false;
            }

            return TryRemoveIdentifierReference(identifier);
        }

        /// <summary>
        /// Retrieve one currently available TutoIdentifier from its persistent object GUID
        /// </summary>
        /// <param name="objectGuid"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool TryGet(string objectGuid, out TutoIdentifier identifier)
        {
            identifier = null;

            if (string.IsNullOrWhiteSpace(objectGuid))
            {
                return false;
            }

            if (!identifiers.TryGetValue(objectGuid, out TutoIdentifier registeredIdentifier))
            {
                return false;
            }

            if (registeredIdentifier == null || !registeredIdentifier.isActiveAndEnabled)
            {
                identifiers.Remove(objectGuid);

                return false;
            }

            identifier = registeredIdentifier;

            return true;
        }

        /// <summary>
        /// Check whether one currently available TutoIdentifier is registered
        /// </summary>
        /// <param name="objectGuid"></param>
        /// <returns></returns>
        public bool Contains(string objectGuid)
        {
            return TryGet(objectGuid, out _);
        }

        /// <summary>
        /// Remove every registered TutoIdentifier
        /// </summary>
        public void Clear()
        {
            identifiers.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Remove one identifier by instance when its current GUID no longer matches its registered key
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private bool TryRemoveIdentifierReference(TutoIdentifier identifier)
        {
            string registeredGuid = string.Empty;

            foreach (KeyValuePair<string, TutoIdentifier> pair in identifiers)
            {
                if (!ReferenceEquals(pair.Value, identifier))
                {
                    continue;
                }

                registeredGuid = pair.Key;

                break;
            }

            if (string.IsNullOrWhiteSpace(registeredGuid))
            {
                return false;
            }

            return identifiers.Remove(registeredGuid);
        }

        #endregion
    }
}