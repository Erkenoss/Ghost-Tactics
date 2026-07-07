using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

namespace Crimson.Core.Addressable
{
    public static class AddressableManager
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Contains every object load currently
        /// </summary>
        private static Dictionary<AssetReference, AsyncOperationHandle> loadedAssets = new Dictionary<AssetReference, AsyncOperationHandle>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return the handle and the asset load
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static async Task<(T asset, AsyncOperationHandle handle)> LoadAssetWithHandle<T>(AssetReferenceT<T> reference) where T : Object
        {
            if (reference == null)
            {
                return (null, default);
            }

            if (loadedAssets.TryGetValue(reference, out AsyncOperationHandle existingHandle))
            {
                await existingHandle.Task;
                return (existingHandle.Result as T, existingHandle);
            }

            AsyncOperationHandle<T> handle = reference.LoadAssetAsync();
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssets[reference] = handle;
                return (handle.Result, handle);
            }

            return (null, handle);
        }

        /// <summary>
        /// UnLoad an Asset by AssetReferenceT
        /// </summary>
        public static void UnloadAsset(AssetReference reference)
        {
            if (reference == null || !loadedAssets.TryGetValue(reference, out var handle))
            {
                return;
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            loadedAssets.Remove(reference);
        }

        /// <summary>
        /// Unload everythink
        /// </summary>
        public static void UnloadAll()
        {
            foreach (KeyValuePair<AssetReference, AsyncOperationHandle> handle in loadedAssets)
            {
                if (handle.Value.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            loadedAssets.Clear();
        }

        #endregion

        #region Private Methods
        #endregion
    }
}