using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Crimson.Core.Scenes
{
    /// <summary>
    /// Class usee to set the differents scene in the SceneContainer SO
    /// </summary>
    [Serializable]
    public class SceneReference
    {
        public string Name { get { return name; } }
        public AssetReference Asset { get { return asset; } }

        [Tooltip("Name of the scene. Use only for reference in inspector. Never use in runtime")]
        [SerializeField]
        private string name = string.Empty;

        [Tooltip("Reference of the AssetReference addressable scene")]
        [SerializeField]
        private AssetReference asset = null;
    }

    public class CrimsonSceneManager : Singleton<CrimsonSceneManager>
    {
        #region Public Fields

        public SceneGroupSO Current { get { return currentGroup; } }

        #endregion

        #region Private Fields

        [Tooltip("Database Scene in the game")]
        [SerializeField]
        protected SceneDatabase allScene = null;

        [Tooltip("First group to run")]
        [SerializeField]
        protected SceneGroupSO firstGroup = null;

        [Tooltip("GameCore group scene. NEVER UNLOAD")]
        [SerializeField]
        protected SceneGroupSO coreGroup = null;

        [Tooltip("Player group scene. Sometime unloaded")]
        [SerializeField]
        protected SceneGroupSO playerGroup = null;

        /// <summary>
        /// Current group actually running
        /// </summary>
        protected SceneGroupSO currentGroup = null;

        /// <summary>
        /// All scene container
        /// </summary>
        private Dictionary<string, SceneGroupSO> sceneGroupContainer = new Dictionary<string, SceneGroupSO>();

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();
            BuildDictionary();
        }

        protected async virtual void Start()
        {
            await LoadGroupAsync(firstGroup.name);
        }

        protected async virtual void OnDestroy()
        {
            await UnloadAllScene();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load playerGroup
        /// </summary>
        /// <returns></returns>
        public async Task LoadPlayerGroup()
        {
            await LoadGroupAsync(playerGroup);
        }

        /// <summary>
        /// Unload playerGroup
        /// </summary>
        /// <returns></returns>
        public async Task UnloadPlayerGroup()
        {
            await UnLoadGroupScene(playerGroup);
        }

        /// <summary>
        /// unload the currentGroup currently running
        /// </summary>
        /// <returns></returns>
        public async Task UnloadCurrentGroup()
        {
            if (currentGroup == null || currentGroup.SceneToLoad == null || currentGroup.SceneToLoad.Count == 0)
            {
                return;
            }

            await UnLoadGroupScene(currentGroup);
            currentGroup = null;
        }

        /// <summary>
        /// Load multiple group in async
        /// </summary>
        /// <param name="groups"></param>
        /// <returns></returns>
        public async Task LoadMultipleGroup(List<SceneGroupSO> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                return;
            }

            foreach(SceneGroupSO group in groups)
            {
                if (group == playerGroup)
                {
                    await LoadGroupAsync(group);
                }
            }

            List<Task> tasks = new List<Task>();
            foreach (SceneGroupSO group in groups)
            {
                if (group == playerGroup)
                {
                    continue;
                }

                tasks.Add(LoadGroupAsync(group));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Load a group in async task by SceneGroupeSO
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public async Task LoadGroupAsync(SceneGroupSO group)
        {
            if (group == null || group.SceneToLoad == null || group.SceneToLoad.Count == 0)
            {
                return;
            }
        
            List<Task> tasks = new List<Task>();

            if (group != coreGroup && group != playerGroup)
            {
                currentGroup = group;
            }

            foreach (SceneReference scene in group.SceneToLoad)
            {
                var task = scene.Asset.LoadSceneAsync(LoadSceneMode.Additive);
                tasks.Add(task.Task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Load a group in async task by name
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public async Task LoadGroupAsync(string name)
        {
            if (sceneGroupContainer.TryGetValue(name, out SceneGroupSO group))
            {
                if (group == null || group.SceneToLoad == null || group.SceneToLoad.Count == 0)
                {
                    return;
                }

                List<Task> tasks = new List<Task>();

                if (group != coreGroup && group != playerGroup)
                {
                    currentGroup = group;
                }

                foreach (SceneReference scene in group.SceneToLoad)
                {
                    var task = scene.Asset.LoadSceneAsync(LoadSceneMode.Additive);
                    tasks.Add(task.Task);
                }

                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Unload a group scene base on name
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        public async Task UnLoadGroupScene(string name)
        {
            if (sceneGroupContainer.TryGetValue(name, out SceneGroupSO group))
            {
                if (group.SceneToLoad == null || group.SceneToLoad.Count == 0)
                {
                    return ;
                }

                List<Task> tasks = new List<Task>();

                foreach (SceneReference scene in group.SceneToLoad)
                {
                    var task = scene.Asset.UnLoadScene();
                    tasks.Add(task.Task);
                }

                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Unload a group base on SceneGroupSO
        /// </summary>
        /// <param name="group"></param>
        public async Task UnLoadGroupScene(SceneGroupSO group)
        {
            if (group.SceneToLoad == null || group.SceneToLoad.Count == 0)
            {
                return;
            }


            List<Task> tasks = new List<Task>();

            foreach (SceneReference scene in group.SceneToLoad)
            {
                var task = scene.Asset.UnLoadScene();
                tasks.Add(task.Task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// UnLoad every active scene
        /// </summary>
        public async Task UnloadAllScene()
        {
            List<Task> tasks = new List<Task>();

            if (currentGroup != null && currentGroup.SceneToLoad != null && currentGroup.SceneToLoad.Count > 0)
            {
                foreach (SceneReference scene in currentGroup.SceneToLoad)
                {
                    var task = scene.Asset.UnLoadScene();
                    tasks.Add(task.Task);
                }

                await Task.WhenAll(tasks);
            }

            tasks.Clear();

            if (playerGroup != null && playerGroup.SceneToLoad != null && playerGroup.SceneToLoad.Count > 0)
            {
                foreach (SceneReference scene in playerGroup.SceneToLoad)
                {
                    var task = scene.Asset.UnLoadScene();
                    tasks.Add(task.Task);
                }

                await Task.WhenAll(tasks);
            }

            tasks.Clear();

            if (coreGroup != null && coreGroup.SceneToLoad != null && coreGroup.SceneToLoad.Count > 0)
            {
                foreach (SceneReference scene in coreGroup.SceneToLoad)
                {
                    var task = scene.Asset.UnLoadScene();
                    tasks.Add(task.Task);
                }

                await Task.WhenAll(tasks);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Build the dictioanry
        /// </summary>
        private void BuildDictionary()
        {
            if (allScene == null || allScene.ContainerList == null || allScene.ContainerList.Count == 0)
            {
                return;
            }

            foreach (SceneGroupSO group in allScene.ContainerList)
            {
                sceneGroupContainer[group.GroupName] = group;
            }
        }

        #endregion
    }
}
