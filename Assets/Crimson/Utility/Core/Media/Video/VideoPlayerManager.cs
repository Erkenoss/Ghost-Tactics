using UnityEngine;
using UnityEngine.Video;

namespace Crimson.Core.Media.Video
{
    public class OnVideoEndedEvent
    {

    }

    public class OnPauseVideoEvent
    {

    }
    
    public class OnPlayVideo
    {
        #region Public Fields

        /// <summary>
        /// RenderTexture where the video will displayed
        /// </summary>
        public VideoClip Clip = null;

        /// <summary>
        /// Do we need to have a loop on the video clip?
        /// </summary>
        public bool IsLoop = false;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="render"></param>
        public OnPlayVideo(VideoClip clip, bool isLoop)
        {
            Clip = clip;
            IsLoop = isLoop;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class OnNewVideoEnvironement
    {
        #region Public Fields
        
        /// <summary>
        /// RenderTexture where the video will displayed
        /// </summary>
        public RenderTexture RenderTexture = null;
        
        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="render"></param>
        public OnNewVideoEnvironement(RenderTexture render)
        {
            RenderTexture = render;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class VideoPlayerManager : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("VideoPlayer of the game")]
        [SerializeField]
        private VideoPlayer videoPlayer = null;

        [Tooltip("Audio Source use with the VideoPlayer")]
        [SerializeField]
        private AudioSource videoSource = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted += VideoPrepared;
                videoPlayer.loopPointReached += VideoEnded;
            }

            Subscribe();
            InitAudioSource();
        }

        private void OnDestroy()
        {
            if ( videoPlayer != null )
            {
                videoPlayer.prepareCompleted -= VideoPrepared;
                videoPlayer.loopPointReached -= VideoEnded;
            }

            Unsubscribe();
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods

        /// <summary>
        /// Assign the AudioSource used by the VideoPlayer.
        /// </summary>
        private void InitAudioSource()
        {
            if (videoPlayer == null || videoSource == null || videoPlayer.audioOutputMode != VideoAudioOutputMode.AudioSource)
            {
                return;
            }

            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoSource);
        }

        /// <summary>
        /// Swithc the displayer of the video player
        /// </summary>
        /// <param name="evn"></param>
        private void InitVideoPlayerTexture(OnNewVideoEnvironement evn)
        {
            if (evn == null || evn.RenderTexture == null || videoPlayer == null)
            {
                return;
            }

            videoPlayer.targetTexture = evn.RenderTexture;
        }

        /// <summary>
        /// Switch the video we want to play
        /// </summary>
        /// <param name="vid"></param>
        private void PlayVideoClip(OnPlayVideo vid)
        {
            if (vid == null || vid.Clip == null || videoPlayer == null || videoSource == null)
            {
                return;
            }

            videoPlayer.clip = vid.Clip;
            videoPlayer.isLooping = vid.IsLoop;
            
            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource && videoPlayer.GetTargetAudioSource(0) == null)
            {
                InitAudioSource();
            }

            videoPlayer.Prepare();
        }

        /// <summary>
        /// Put the videoPlayer to pause
        /// </summary>
        /// <param name="p"></param>
        private void Pause(OnPauseVideoEvent p)
        {
            if (videoPlayer == null || !videoPlayer.isPlaying)
            {
                return;
            }

            videoPlayer.Pause();
        }

        /// <summary>
        /// Skip the video
        /// </summary>
        /// <param name="vid"></param>
        private void SkipVideo(OnSkipVideo vid)
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.Stop();
            videoPlayer.clip = null;

            VideoEnded(videoPlayer);
        }

        /// <summary>
        /// Use to start a vidéo
        /// </summary>
        /// <param name="vidPlayer"></param>
        private void VideoPrepared(VideoPlayer vidPlayer)
        {
            vidPlayer.Play();
        }

        /// <summary>
        /// Call when the video has finished
        /// </summary>
        /// <param name="player"></param>
        private void VideoEnded(VideoPlayer player)
        {
            EventBus.Publish<OnVideoEndedEvent>(new OnVideoEndedEvent());
        }

        /// <summary>
        /// Subscribe with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnNewVideoEnvironement>(InitVideoPlayerTexture);
            EventBus.Subscribe<OnPlayVideo>(PlayVideoClip);
            EventBus.Subscribe<OnPauseVideoEvent>(Pause);
            EventBus.Subscribe<OnSkipVideo>(SkipVideo);
        }

        /// <summary>
        /// Unsubscribe with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnNewVideoEnvironement>(InitVideoPlayerTexture);
            EventBus.Unsubscribe<OnPlayVideo>(PlayVideoClip);
            EventBus.Unsubscribe<OnPauseVideoEvent>(Pause);
            EventBus.Unsubscribe<OnSkipVideo>(SkipVideo);
        }


        #endregion
    }
}