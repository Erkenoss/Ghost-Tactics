using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Crimson.Core.Scenes
{
    public class OnSceneGroupLoaded
    {
        public SceneGroupSO Group = null;

        public OnSceneGroupLoaded(SceneGroupSO group)
        {
            Group = group;
        }
    }

    public class OnSceneToLoad
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// The groups we need to load
        /// </summary>
        public List<SceneGroupSO> ToLoad = new List<SceneGroupSO>();

        /// <summary>
        /// The group  we want to load
        /// </summary>
        public SceneGroupSO SceneToLoad = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor with list
        /// </summary>
        /// <param name="toLoad"></param>
        public OnSceneToLoad(List<SceneGroupSO> toLoad)
        {
            ToLoad = toLoad;
        }

        /// <summary>
        /// Constructor with group
        /// </summary>
        /// <param name="sceneToLoad"></param>
        public OnSceneToLoad(SceneGroupSO sceneToLoad)
        {
            SceneToLoad = sceneToLoad;
        }

        #endregion

        #region Private Methods
        #endregion
    }

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

        [Tooltip("Player group scene. Sometime unloaded")]
        [SerializeField]
        protected SceneGroupSO playerGroup = null;

        [Tooltip("The load scene group of the game")]
        [SerializeField]
        protected SceneGroupSO loadGroup = null;

        /// <summary>
        /// Current group actually running
        /// </summary>
        protected SceneGroupSO currentGroup = null;

        /// <summary>
        /// All scene container
        /// </summary>
        private Dictionary<string, SceneGroupSO> sceneGroupContainer = new Dictionary<string, SceneGroupSO>();

        /// <summary>
        /// Groups we want to load
        /// </summary>
        private List<SceneGroupSO> pendingGroups = new List<SceneGroupSO>();

        private SceneGroupSO previousGroup = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();
            BuildDictionary();

            Subscribe();
        }

        protected virtual void Start()
        {
            EventBus.Publish<OnSceneToLoad>(new OnSceneToLoad(firstGroup));
        }

        protected virtual void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Use to load the load scene screen
        /// </summary>
        /// <param name="scene"></param>
        public async void LoadLoadGroupEvent(OnSceneToLoad scene)
        {
            await LoadLoadGroup(scene);
        }

        /// <summary>
        /// Use to load the pending groups
        /// </summary>
        /// <param name="minimumDuration"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task LoadPendingGroups(float minimumDuration, IProgress<float> progress = null)
        {
            if (pendingGroups == null || pendingGroups.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            List<SceneGroupSO> loadedGroups = new List<SceneGroupSO>(pendingGroups);
            Task minimumDurationTask = Task.Delay(Mathf.CeilToInt(minimumDuration * 1000f));

            progress?.Report(0f);

            Progress<float> sceneProgress = new Progress<float>(
                value => progress?.Report(value * 0.85f)
            );

            await LoadMultipleGroup(pendingGroups, sceneProgress);
            await minimumDurationTask;

            progress?.Report(0.85f);

            if (previousGroup != null && previousGroup != currentGroup)
            {
                await UnLoadGroupScene(previousGroup);
            }

            previousGroup = null;

            progress?.Report(0.9f);

            await UnLoadGroupScene(loadGroup);

            progress?.Report(0.95f);

            pendingGroups.Clear();

            foreach (SceneGroupSO group in loadedGroups)
            {
                EventBus.Publish(new OnSceneGroupLoaded(group));
            }

            progress?.Report(1f);
        }

        /// <summary>
        /// Use to load the Load Scene before all other loads
        /// </summary>
        /// <returns></returns>
        public async Task LoadLoadGroup(OnSceneToLoad scene)
        {
            if (scene == null)
            {
                return;
            }

            pendingGroups.Clear();

            if (scene.SceneToLoad != null)
            {
                pendingGroups.Add(scene.SceneToLoad);
            }

            if (scene.ToLoad != null && scene.ToLoad.Count > 0)
            {
                pendingGroups.AddRange(scene.ToLoad);
            }

            if (pendingGroups.Count == 0)
            {
                return;
            }

            previousGroup = currentGroup;
            await LoadGroupAsync(loadGroup);
        }

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
        /// Load multiple groups asynchronously and report their completion.
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task LoadMultipleGroup(List<SceneGroupSO> groups, IProgress<float> progress = null)
        {
            if (groups == null || groups.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            List<SceneGroupSO> validGroups = groups.FindAll(group => group != null && group.SceneToLoad != null && group.SceneToLoad.Count > 0);

            if (validGroups.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            int completedGroups = 0;
            int totalGroups = validGroups.Count;

            if (validGroups.Contains(playerGroup))
            {
                await LoadGroupAsync(playerGroup);

                completedGroups++;
                progress?.Report((float)completedGroups / totalGroups);
            }

            List<Task> tasks = new List<Task>();

            foreach (SceneGroupSO group in validGroups)
            {
                if (group == playerGroup)
                {
                    continue;
                }

                tasks.Add(LoadGroupAsync(group));
            }

            while (tasks.Count > 0)
            {
                Task completedTask = await Task.WhenAny(tasks);

                tasks.Remove(completedTask);
                await completedTask;

                completedGroups++;
                progress?.Report((float)completedGroups / totalGroups);
            }
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

            foreach (SceneReference scene in group.SceneToLoad)
            {
                var handle = scene.Asset.LoadSceneAsync(LoadSceneMode.Additive);
                tasks.Add(handle.Task);
            }

            await Task.WhenAll(tasks);

            if (group != playerGroup && group != loadGroup)
            {
                currentGroup = group;
            }
        }

        /// <summary>
        /// Load a group in async task by name
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public async Task LoadGroupAsync(string name)
        {
            if (!sceneGroupContainer.TryGetValue(name, out SceneGroupSO group))
            {
                return;
            }

            await LoadGroupAsync(group);
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

        /// <summary>
        /// Subscribe in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnSceneToLoad>(LoadLoadGroupEvent);
        }

        /// <summary>
        /// Unsubscribe with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnSceneToLoad>(LoadLoadGroupEvent);
        }

        #endregion
    }
}
