using Crimson.Core;
using Crimson.Core.Media.Video;
using Crimson.Core.Scenes;
using GhostTactics.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace GhostTactics.Media
{
    public class StarterController : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Button we want to skip the trailer")]
        [SerializeField]
        private Button skipButton = null;

        [Tooltip("Trailer of the game")]
        [SerializeField]
        private VideoClip trailer = null;

        [Tooltip("Where the video player will display the trailer")]
        [SerializeField]
        private RenderTexture videoPlayertextureTarget = null;

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

        [Tooltip("The main menu scene we want to load")]
        [SerializeField]
        private SceneGroupSO mainMenuScene = null;

        /// <summary>
        /// Used to prevent the startup ending coroutine from running several times.
        /// </summary>
        private bool startupEnding = false;

        #endregion

        #region MonoBehaviour Callbacks

        private IEnumerator Start()
        {
            Subscribe();

            if (logoDisplay == null || videoDisplay == null || UIManager.Instance == null)
            {
                yield break;
            }

            UIManager.Instance.SetFadeInstantly(1f);

            logoDisplay.SetActive(true);
            videoDisplay.SetActive(false);

            yield return UIManager.Instance.FadeFromBlack(fadeDuration);
            yield return new WaitForSecondsRealtime(logoDuration);
            yield return UIManager.Instance.FadeToBlack(fadeDuration);

            logoDisplay.SetActive(false);
            videoDisplay.SetActive(true);

            if (skipButton != null)
            {
                skipButton.interactable = true;
            }

            EventBus.Publish(new OnNewVideoEnvironement(videoPlayertextureTarget));
            EventBus.Publish(new OnPlayVideo(trailer, false));

            yield return UIManager.Instance.FadeFromBlack(fadeDuration);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Skip the trailer and continue to the main menu.
        /// </summary>
        public void SkipTrailer()
        {
            if (startupEnding)
            {
                return;
            }

            StartCoroutine(EndStartup());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Use to know when the trailer is finished.
        /// </summary>
        /// <param name="evt"></param>
        private void EndTrailer(OnVideoEndedEvent evt)
        {
            if (evt == null || startupEnding)
            {
                return;
            }

            StartCoroutine(EndStartup());
        }

        /// <summary>
        /// Fade the trailer and continue to the next scene.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EndStartup()
        {
            if (startupEnding)
            {
                yield break;
            }

            startupEnding = true;

            if (skipButton != null)
            {
                skipButton.interactable = false;
            }

            yield return UIManager.Instance.FadeToBlack(fadeDuration);

            EventBus.Publish(new OnSceneToLoad(mainMenuScene));
        }

        /// <summary>
        /// Subscribe to the EventBus.
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnVideoEndedEvent>(EndTrailer);
        }

        /// <summary>
        /// Unsubscribe from the EventBus.
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnVideoEndedEvent>(EndTrailer);
        }

        #endregion
    }
}