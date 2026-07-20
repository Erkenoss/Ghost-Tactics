using Crimson.Core;
using Crimson.Core.Media.Video;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using Crimson.Core.Scenes;
using System.Threading.Tasks;

namespace GhostTactics.Media
{
    public class StarterController : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Trailer of the game")]
        [SerializeField]
        private VideoClip trailer = null;

        [Tooltip("Where the video player woill display the trailer")]
        [SerializeField]
        private RenderTexture videoPlayertextureTarget = null;

        [Tooltip("Black overlay used to fade the startup screen")]
        [SerializeField]
        private CanvasGroup fadeCanvasGroup = null;

        [Tooltip("Logo displayed before the trailer")]
        [SerializeField]
        private GameObject logoDisplay = null;

        [Tooltip("RawImage container used to display the trailer")]
        [SerializeField]
        private GameObject videoDisplay = null;

        [Tooltip("Duration of a fade")]
        [SerializeField]
        private float fadeDuration = 0.75f;

        [Tooltip("Duration during which the logo remains visible")]
        [SerializeField]
        private float logoDuration = 2f;

        [Tooltip("The main meenu scene we want to load")]
        [SerializeField]
        private SceneGroupSO mainMenuScene = null;

        #endregion

        #region MonoBehaviour Callbacks

        private IEnumerator Start()
        {
            Subscribe();

            if (fadeCanvasGroup == null || logoDisplay == null || videoDisplay == null)
            {
                yield break;
            }

            fadeCanvasGroup.alpha = 1f;

            logoDisplay.SetActive(true);
            videoDisplay.SetActive(false);

            yield return Fade(0f);
            yield return new WaitForSecondsRealtime(logoDuration);
            yield return Fade(1f);

            logoDisplay.SetActive(false);
            videoDisplay.SetActive(true);

            EventBus.Publish<OnNewVideoEnvironement>(new OnNewVideoEnvironement(videoPlayertextureTarget));
            EventBus.Publish<OnPlayVideo>(new OnPlayVideo(trailer, false));

            yield return Fade(0f);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// use to know when the trailer is finished
        /// </summary>
        /// <param name="evt"></param>
        private void EndTrailer(OnVideoEndedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            StartCoroutine(EndStartup());
        }

        /// <summary>
        /// Fade the startup screen to the target alpha
        /// </summary>
        /// <param name="targetAlpha"></param>
        /// <returns></returns>
        private IEnumerator Fade(float targetAlpha)
        {
            if (fadeCanvasGroup == null)
            {
                yield break;
            }

            if (fadeDuration <= 0f)
            {
                fadeCanvasGroup.alpha = targetAlpha;
                yield break;
            }

            float startAlpha = fadeCanvasGroup.alpha;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;

                float progression = Mathf.Clamp01(timer / fadeDuration);

                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progression);

                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// Fade the trailer and continue to the next scene
        /// </summary>
        /// <returns></returns>
        private IEnumerator EndStartup()
        {
            yield return Fade(1f);
            EventBus.Publish<OnSceneToLoad>(new OnSceneToLoad(mainMenuScene));
        }

        /// <summary>
        /// Sub in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnVideoEndedEvent>(EndTrailer);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnVideoEndedEvent>(EndTrailer);
        }

        #endregion
    }
}