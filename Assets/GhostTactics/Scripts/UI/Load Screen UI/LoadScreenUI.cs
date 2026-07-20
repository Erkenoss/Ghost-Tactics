using Crimson.Core.Scenes;
using GhostTactics.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.UI
{
    [Serializable]
    public class VisualSet
    {
        public int PlayerGender { get { return playerGender; } }
        public List<Sprite> Sprites { get {  return sprites; }  }


        [Tooltip("Use to know the what gender will be display. Base on the player gender")]
        [SerializeField]
        private int playerGender = 0;

        [Tooltip("List of sprite for the load bar visual")]
        [SerializeField]
        private List<Sprite> sprites = new List<Sprite>();
    }

    public class LoadScreenUI : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Contains the visual for the load bar")]
        [SerializeField]
        private List<VisualSet> visualList = new List<VisualSet>(); 

        [Tooltip("Image where the background will display")]
        [SerializeField]
        private Image backgroundImage = null;

        [Tooltip("List of background will display during the load screen")]
        [SerializeField]
        private List<Sprite> backgroundListImages = new List<Sprite>();

        [Tooltip("The minimum time pass in the Load Scene. Use to load what we need in the game")]
        [SerializeField]
        private float minTimeToLoad = 0;

        [Tooltip("Image of the load bad")]
        [SerializeField]
        private List<Image> loadBarImage = new List<Image>();

        /// <summary>
        /// Coroutine to manage the load screen scene
        /// </summary>
        private Coroutine loadCoroutine = null;

        /// <summary>
        /// Use to calculate the progress of the load bar
        /// </summary>
        private float loadingProgress = 0f;

        /// <summary>
        /// Progress currently displayed by the load bar.
        /// </summary>
        private float displayedProgress = 0f;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            SetGender();
            LoadScreen();
            loadCoroutine = StartCoroutine(LoadCoroutine());
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Set the gender of the image we will have
        /// </summary>
        private void SetGender()
        {
            if (GameManager.Instance == null || GameManager.Instance.Player == null || visualList == null || visualList.Count == 0 || loadBarImage == null || loadBarImage.Count == 0)
            {
                return;
            }

            List<Sprite> tmpList = visualList.Find(v => v.PlayerGender == GameManager.Instance.Player.Gender).Sprites;

            if (tmpList == null || tmpList.Count == 0)
            {
                return;
            }

            int count = Mathf.Min(loadBarImage.Count, tmpList.Count);

            for (int i = 0; i < count; i++)
            {
                if (loadBarImage[i] == null || tmpList[i] == null)
                {
                    continue;
                }

                loadBarImage[i].sprite = tmpList[i];
                loadBarImage[i].preserveAspect = true;
            }
        }

        /// <summary>
        /// Use to display the the background
        /// </summary>
        /// <param name="ls"></param>
        private void LoadScreen()
        {
            if (backgroundListImages == null || backgroundListImages.Count == 0)
            {
                return;
            }
        
            Sprite bg = backgroundListImages[UnityEngine.Random.Range(0, backgroundListImages.Count)];
            SwitchBackground(bg);
        }

        /// <summary>
        /// Change the sprite of the background
        /// </summary>
        /// <param name="spr"></param>
        private void SwitchBackground(Sprite spr)
        {
            if(backgroundImage == null || spr == null)
            {
                return;
            }

            backgroundImage.sprite = spr;
        }

        /// <summary>
        /// Use to load the game.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadCoroutine()
        {
            loadingProgress = 0f;
            displayedProgress = 0f;

            UpdateLoadBar(0f);

            Progress<float> progress = new Progress<float>(value => loadingProgress = Mathf.Clamp01(value));
            Task loadingTask = CrimsonSceneManager.Instance.LoadPendingGroups(minTimeToLoad, progress);

            float visualDuration = Mathf.Max(minTimeToLoad, 0.1f);
            float visualSpeed = 1f / visualDuration;

            while (!loadingTask.IsCompleted || displayedProgress < 1f)
            {
                if (loadingTask.IsFaulted)
                {
                    Debug.LogException(loadingTask.Exception);
                    loadCoroutine = null;
                    yield break;
                }

                float targetProgress = loadingTask.IsCompleted ? 1f : loadingProgress;

                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, visualSpeed * Time.unscaledDeltaTime);

                UpdateLoadBar(displayedProgress);

                yield return null;
            }

            UpdateLoadBar(1f);
            loadCoroutine = null;
        }

        /// <summary>
        /// Update load bar visual
        /// </summary>
        /// <param name="progress"></param>
        private void UpdateLoadBar(float progress)
        {
            if (loadBarImage == null || loadBarImage.Count == 0)
            {
                return;
            }

            int currentIndex = Mathf.Clamp(Mathf.FloorToInt(progress * loadBarImage.Count), 0, loadBarImage.Count - 1);

            for (int i = 0; i < loadBarImage.Count; i++)
            {
                if (loadBarImage[i] == null)
                {
                    continue;
                }

                Color color = loadBarImage[i].color;
                color.a = i < currentIndex ? 0.6f : i == currentIndex ? 1f : 0.3f;
                loadBarImage[i].color = color;
            }
        }


        #endregion
    }
}
