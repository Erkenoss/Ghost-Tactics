using Crimson.Core;
using Crimson.Core.Scenes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GhostTactics.Core
{
    public class UIManager : Singleton<UIManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Background image of the game scene, used to change the background when the player enters a level.
        /// </summary>
        private Image backgroundGameSceneImage = null;

        /// <summary>
        /// CanvasGroup used to fade the entire screen.
        /// </summary>
        [SerializeField]
        private CanvasGroup fadeCanvasGroup = null;

        /// <summary>
        /// Default duration used by screen fades.
        /// </summary>
        [SerializeField]
        private float defaultFadeDuration = 0.4f;

        /// <summary>
        /// Currently running fade coroutine.
        /// </summary>
        private Coroutine fadeCoroutine = null;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            InitializeFadeCanvas();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the background image reference of the game scene.
        /// </summary>
        /// <param name="image"></param>
        public void UpdateImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            backgroundGameSceneImage = image;
        }

        /// <summary>
        /// Starts a fade toward the requested alpha.
        /// Alpha 0 means transparent and alpha 1 means fully visible.
        /// </summary>
        /// <param name="targetAlpha"></param>
        /// <param name="duration"></param>
        /// <param name="useUnscaledTime"></param>
        /// <returns></returns>
        public Coroutine FadeTo(float targetAlpha, float duration = -1f, bool useUnscaledTime = true)
        {
            if (fadeCanvasGroup == null)
            {
                return null;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            float fadeDuration = duration >= 0f ? duration : defaultFadeDuration;
            fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, fadeDuration, useUnscaledTime));

            return fadeCoroutine;
        }

        /// <summary>
        /// Fades the screen to black.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="useUnscaledTime"></param>
        /// <returns></returns>
        public Coroutine FadeToBlack(float duration = -1f, bool useUnscaledTime = true)
        {
            return FadeTo(1f, duration, useUnscaledTime);
        }

        /// <summary>
        /// Fades the screen from black to transparent.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="useUnscaledTime"></param>
        /// <returns></returns>
        public Coroutine FadeFromBlack(float duration = -1f, bool useUnscaledTime = true)
        {
            return FadeTo(0f, duration, useUnscaledTime);
        }

        /// <summary>
        /// Immediately sets the fade canvas alpha without animation.
        /// </summary>
        /// <param name="alpha"></param>
        public void SetFadeInstantly(float alpha)
        {
            if (fadeCanvasGroup == null)
            {
                return;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            float targetAlpha = Mathf.Clamp01(alpha);

            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the fade CanvasGroup according to its current alpha.
        /// </summary>
        private void InitializeFadeCanvas()
        {
            if (fadeCanvasGroup == null)
            {
                return;
            }

            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = fadeCanvasGroup.alpha > 0f;
        }

        /// <summary>
        /// Progressively changes the fade CanvasGroup alpha.
        /// </summary>
        /// <param name="targetAlpha"></param>
        /// <param name="duration"></param>
        /// <param name="useUnscaledTime"></param>
        /// <returns></returns>
        private IEnumerator FadeCoroutine(float targetAlpha, float duration, bool useUnscaledTime)
        {
            float startAlpha = fadeCanvasGroup.alpha;
            float finalAlpha = Mathf.Clamp01(targetAlpha);

            fadeCanvasGroup.blocksRaycasts = true;

            if (duration <= 0f)
            {
                fadeCanvasGroup.alpha = finalAlpha;
                fadeCanvasGroup.blocksRaycasts = finalAlpha > 0f;
                fadeCoroutine = null;
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float progression = Mathf.Clamp01(elapsedTime / duration);
                progression = Mathf.SmoothStep(0f, 1f, progression);

                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, finalAlpha, progression);

                yield return null;
            }

            fadeCanvasGroup.alpha = finalAlpha;
            fadeCanvasGroup.blocksRaycasts = finalAlpha > 0f;
            fadeCoroutine = null;
        }

        /// <summary>
        /// Updates the background sprite of the game scene with the new sprite from the level data.
        /// </summary>
        /// <param name="lvl"></param>
        private void UpdateBackgroundSprite(NextLevel lvl)
        {
            if (backgroundGameSceneImage == null)
            {
                return;
            }

            backgroundGameSceneImage.sprite = lvl.Data.LevelImage;
        }

        /// <summary>
        /// Unfade if the screen is fade when a new scene is load
        /// </summary>
        /// <param name="toLoad"></param>
        private void SceneLoaded(OnSceneGroupLoaded toLoad)
        {
            FadeFromBlack();
        }

        /// <summary>
        /// Subscribes the different events to the EventBus.
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<NextLevel>(UpdateBackgroundSprite);
            EventBus.Subscribe<OnSceneGroupLoaded>(SceneLoaded);
        }

        /// <summary>
        /// Unsubscribes the different events from the EventBus to avoid memory leaks and unwanted behavior.
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<NextLevel>(UpdateBackgroundSprite);
            EventBus.Unsubscribe<OnSceneGroupLoaded>(SceneLoaded);
        }

        #endregion
    }
}