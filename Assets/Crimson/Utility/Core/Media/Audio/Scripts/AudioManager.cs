using Crimson.Audio;
using Crimson.Core.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Crimson.Core.Audio
{
    public enum EAudio
    {
        None,
        Master,
        Effect,
        Music,
        UI,
        Ambiance,
        Voice,
        Cinematic,
        SFX
    }

    public enum EPlayerContext
    {
        None,
        Respawn,
        Death,
        Effort,
        Defense,
        Hurt
    }

    public enum EMusicContext
    {
        None,
        Startup,
        MainMenu,
        Exploration,
        Combat,
        Boss
    }

    public enum ESFXContext
    {
        None,
        Voice,
        UI,
        SFX,
        Effect
    }

    public class OnPlayerSoundEvent
    {
        public EPlayerContext Context = EPlayerContext.None;
        public EAudio Type = EAudio.None;
        
        public OnPlayerSoundEvent(EPlayerContext context, EAudio type)
        {
            Context = context;
            Type = type;
        }
    }

    public class OnNewMusicContainer
    {
        public EMusicContext Context = EMusicContext.None;
    
        public OnNewMusicContainer(EMusicContext context)
        {
            Context = context;
        }
    }

    public class OnMusicNearEnd
    {
        public AudioClip Clip = null;

        public OnMusicNearEnd(AudioClip clip)
        {
            Clip = clip;
        }
    }

    public class OnSetMixerValue
    {
        #region Public Fields

        public EAudio Type = EAudio.None;

        public float Value = 0;

        public bool IsSetting = false;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        public OnSetMixerValue(EAudio type, float value, bool isSetting = false)
        {
            Type = type;
            Value = value;
            IsSetting = isSetting;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    [Serializable]
    public class SourceContainer
    {
        #region Public Fields

        public EAudio Type { get { return type; } }

        #endregion

        #region Private Fields

        [Tooltip("Type of the different Audio Source")]
        [SerializeField]
        private EAudio type = EAudio.None;

        [Tooltip("All AudioSource of this type of sound")]
        [SerializeField]
        private List<AudioSource> sources = new List<AudioSource>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return a free AudioSource
        /// </summary>
        /// <returns></returns>
        public AudioSource GetFreeAudioSource()
        {
            return sources.Find(s => s.isPlaying == false);
        }

        /// <summary>
        /// Return the first AudioSource
        /// </summary>
        /// <returns></returns>
        public AudioSource GetFirstAudioSource()
        {
            return sources.Find(s => s != null);
        }

        #endregion

        #region Private Methods
        #endregion
    }

    [Serializable]
    public class MusicContainer
    {
        #region Public Fields

        public EMusicContext Type { get { return type; } }
        public MusicClipContainer Container { get { return container; } }

        #endregion

        #region Private Fields

        [Tooltip("Type of the different Audio Source")]
        [SerializeField]
        private EMusicContext type = EMusicContext.None;

        [Tooltip("All AudioSource of this type of sound")]
        [SerializeField]
        private MusicClipContainer container = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return a free AudioSource
        /// </summary>
        /// <returns></returns>
        public AudioClip GetClip(string name)
        {
            return container.MusicList.Find(m => m.name == name);
        }

        #endregion

        #region Private Methods
        #endregion
    }

    [Serializable]
    public class SFXContainer
    {
        #region Public Fields

        public ESFXContext Context { get { return context; } }
        public SFXClipContainer Container { get { return container; } }

        #endregion

        #region Private Fields

        [Tooltip("Type of SFX")]
        [SerializeField]
        private ESFXContext context = ESFXContext.None;

        [Tooltip("Container of SFX")]
        [SerializeField]
        private SFXClipContainer container = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return a free AudioSource
        /// </summary>
        /// <returns></returns>
        public AudioClip GetClip(string name)
        {
            return container.SfxList.Find(m => m.name == name);
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class OnPlaySoundEvent
    {
        #region Public Fields

        /// <summary>
        /// The type of the Audio
        /// </summary>
        public EAudio Type = EAudio.None;

        /// <summary>
        /// Clip we want to play
        /// </summary>
        public AudioClip Clip = null;

        /// <summary>
        /// This clip is looping or not.
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
        public OnPlaySoundEvent(EAudio type, AudioClip clip, bool isLoop = false)
        {
            Type = type;
            Clip = clip;
            IsLoop = isLoop;
        }

        #endregion

        #region Private Methods
        #endregion
    }

    public class AudioManager : Singleton<AudioManager>
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Audio Mixer of the project")]
        [SerializeField]
        private AudioMixer mixer = null;

        [Tooltip("How many time we want to fade the music before it's end?")]
        [SerializeField]
        private float timeFader = 0f;

        [Tooltip("All generic sources of the game here")]
        [SerializeField]
        private List<SourceContainer> allSources = new List<SourceContainer>();

        [Tooltip("All generics sounds in the game here")]
        [SerializeField]
        private List<MusicContainer> allSounds = new List<MusicContainer>();

        [Tooltip("All generic SFX of the game here")]
        [SerializeField]
        private List<SFXContainer> allSFX = new List<SFXContainer>();

        [Tooltip("The container of the player sound")]
        [SerializeField]
        private PlayerSoundContainer playerSound = null;

        /// <summary>
        /// AudioSource currently played music
        /// </summary>
        private AudioSource currentMusicSource = null;

        /// <summary>
        /// AudioSource currently played ambiance sound
        /// </summary>
        private AudioSource currentAmbianceSource = null;

        /// <summary>
        /// Coroutine to fade off the music
        /// </summary>
        private Coroutine musicFadeCoroutine = null;

        /// <summary>
        /// Coroutine used to fade the ambiance
        /// </summary>
        private Coroutine ambianceFadeCoroutine = null;

        /// <summary>
        /// Manage the coroutine when a music is on the end
        /// </summary>
        private Coroutine musicEndCoroutine = null;

        /// <summary>
        /// Current music container actually playing
        /// </summary>
        private MusicClipContainer currentMusicContainer = null;

        /// <summary>
        /// The index to play the music lits container
        /// </summary>
        private int currentIndexMusic = -1;

        /// <summary>
        /// Use to manage when the application focus or pause is suspended
        /// </summary>
        private bool isApplicationSuspended = false;

        #endregion

        #region MonoBehaviour Callbacks

        protected override void Awake()
        {
            base.Awake();

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion

        #region Public Methods


        /// <summary>
        /// Return the volume to set the slider
        /// </summary>
        /// <param name="type"></param>
        public float GetVolume(EAudio type)
        {
            if (!mixer.GetFloat(type.ToString(), out float decibel))
            {
                return 0f;
            }

            return Mathf.Pow(10f, decibel / 20f);
        }

        #endregion

        #region Private Methods


        /// <summary>
        /// Base on context, play a sound randomly
        /// </summary>
        /// <param name="context"></param>
        private void PlayRandomPlayerSound(OnPlayerSoundEvent e)
        {
            if (e.Context == EPlayerContext.None || playerSound == null)
            {
                return;
            }

            SoundContext ctx = playerSound.GetSoundContext(e.Context);

            if (ctx == null)
            {
                return;
            }

            PlayerSoundType playerSoundType = ctx.GetPlayerSoundType(e.Type);

            if (playerSoundType == null)
            {
                return;
            }

            AudioClip clip =  playerSoundType.ClipType[UnityEngine.Random.Range(0, playerSoundType.ClipType.Count)];

            if (clip == null)
            {
                return;
            }

            Play(new OnPlaySoundEvent(e.Type, clip, false));
        }


        /// <summary>
        /// Play an AudioClip on a source base on it's type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="clip"></param>
        private void Play(OnPlaySoundEvent play)
        {
            if (allSources == null || allSources.Count == 0|| play == null || play.Clip == null)
            {
                return;
            }

            switch(play.Type)
            {
                case EAudio.None:
                    break;
                case EAudio.Effect:
                    PlayEffect(play.Clip, play.IsLoop);
                    break;
                case EAudio.Music:
                    PlayMusic(play.Clip, play.IsLoop);
                    break;
                case EAudio.UI:
                    PlayUI(play.Clip, play.IsLoop);
                    break;
                case EAudio.Ambiance:
                    PlayAmbiance(play.Clip, play.IsLoop);
                    break;
                case EAudio.Voice:
                    PlayVoice(play.Clip, play.IsLoop);
                    break;
            }
        }

        /// <summary>
        /// Play a music
        /// </summary>
        /// <param name="clip"></param>
        private void PlayMusic(AudioClip clip, bool isLoop)
        {
            SourceContainer container = allSources.Find(s => s.Type == EAudio.Music);

            if (container == null)
            {
                return;
            }

            if (musicFadeCoroutine != null)
            {
                return;
            }

            AudioSource newMusicSource = container.GetFreeAudioSource();

            if (newMusicSource == null)
            {
                return;
            }

            if (currentMusicSource == null || currentMusicSource.isPlaying == false)
            {
                currentMusicSource = newMusicSource;
                currentMusicSource.clip = clip;
                currentMusicSource.loop = isLoop;
                currentMusicSource.volume = 1f;
                currentMusicSource.Play();
                StartMusicEndCoroutine(currentMusicSource);
                return;
            }

            musicFadeCoroutine = StartCoroutine(FadeAudio(currentMusicSource, newMusicSource, clip, isLoop, EAudio.Music));
        }

        /// <summary>
        /// Play an effect
        /// </summary>
        /// <param name="clip"></param>
        private void PlayEffect(AudioClip clip, bool isLoop)
        {
            SourceContainer container = allSources.Find(s => s.Type == EAudio.Effect);

            if (container == null)
            {
                return;
            }

            AudioSource source = container.GetFreeAudioSource();

            if (source == null)
            {
                return;
            }
            source.PlayOneShot(clip);
        }

        /// <summary>
        /// Play a UI Effect
        /// </summary>
        /// <param name="clip"></param>
        private void PlayUI(AudioClip clip, bool isLoop)
        {
            SourceContainer container = allSources.Find(s => s.Type == EAudio.UI);

            if (container == null)
            {
                return;
            }

            AudioSource source = container.GetFirstAudioSource();

            if (source == null)
            {
                return;
            }

            if (source.isPlaying)
            {
                source.Stop();
            }

            source.PlayOneShot(clip);
        }

        /// <summary>
        /// Play an ambiance sound
        /// </summary>
        /// <param name="clip"></param>
        private void PlayAmbiance(AudioClip clip, bool isLoop)
        {
            SourceContainer container = allSources.Find(s => s.Type == EAudio.Ambiance);

            if (container == null)
            {
                return;
            }

            if (ambianceFadeCoroutine != null)
            {
                return;
            }

            AudioSource newAmbianceSource = container.GetFreeAudioSource();

            if (newAmbianceSource == null)
            {
                return;
            }

            if (currentAmbianceSource == null || currentAmbianceSource.isPlaying == false)
            {
                currentAmbianceSource = newAmbianceSource;
                currentAmbianceSource.clip = clip;
                currentAmbianceSource.loop = isLoop;
                currentAmbianceSource.volume = 1f;
                currentAmbianceSource.Play();
                return;
            }

            ambianceFadeCoroutine = StartCoroutine(FadeAudio(currentAmbianceSource, newAmbianceSource, clip, isLoop, EAudio.Ambiance));
        }

        /// <summary>
        /// Play a voice sound
        /// </summary>
        /// <param name="clip"></param>
        private void PlayVoice(AudioClip clip, bool isLoop)
        {
            SourceContainer container = allSources.Find(s => s.Type == EAudio.Voice);

            if (container == null)
            {
                return;
            }

            AudioSource source = container.GetFreeAudioSource();

            if (source == null)
            {
                return;
            }

            source.clip = clip;
            source.loop = isLoop;

            source.Play();
        }

        /// <summary>
        /// Set the value in the mixer to manage sound settings
        /// </summary>
        /// <param name="mix"></param>
        private void SetMixer(OnSetMixerValue mix)
        {
            if (mix == null || mix.Type == EAudio.None || mixer == null)
            {
                return;
            }

            if (!mix.IsSetting)
            {
                EventBus.Publish<OnChangeSoundSetting>(new OnChangeSoundSetting(mix.Type, mix.Value));
            }

            float mixerValue = LinearToDb(mix.Value);

            mixer.SetFloat(mix.Type.ToString(), mixerValue);
        }

        /// <summary>
        /// Return the value Linear in decibel
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private float LinearToDb(float value)
        {
            return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        }

        /// <summary>
        /// Use to fade off the music
        /// </summary>
        /// <param name="oldSource"></param>
        /// <param name="newSource"></param>
        /// <param name="clip"></param>
        /// <param name="isLoop"></param>
        /// <returns></returns>
        private IEnumerator FadeAudio(AudioSource oldSource, AudioSource newSource, AudioClip clip, bool isLoop, EAudio type)
        {
            newSource.clip = clip;
            newSource.loop = isLoop;
            newSource.volume = 0f;
            newSource.Play();

            if (timeFader > 0f)
            {
                float timer = 0f;

                while (timer < timeFader)
                {
                    if (isApplicationSuspended)
                    {
                        yield return null;
                        continue;
                    }

                    timer += Time.unscaledDeltaTime;

                    float progression = Mathf.Clamp01(timer / timeFader);

                    oldSource.volume = 1f - progression;
                    newSource.volume = progression;

                    yield return null;
                }
            }

            oldSource.Stop();
            oldSource.clip = null;
            oldSource.volume = 1f;

            newSource.volume = 1f;

            switch (type)
            {
                case EAudio.Music:
                    currentMusicSource = newSource;
                    StartMusicEndCoroutine(currentMusicSource);
                    musicFadeCoroutine = null;
                    break;
                case EAudio.Ambiance:
                    currentAmbianceSource = newSource;
                    ambianceFadeCoroutine = null;
                    break;
            }
        }

        /// <summary>
        /// USe to start the coroutine when a music near to the end
        /// </summary>
        /// <param name="source"></param>
        private void StartMusicEndCoroutine(AudioSource source)
        {
            if (musicEndCoroutine != null)
            {
                StopCoroutine(musicEndCoroutine);
                musicEndCoroutine = null;
            }

            if (source == null || source.clip == null || source.loop)
            {
                return;
            }

            musicEndCoroutine = StartCoroutine(MusicEndCoroutine(source));
        }

        /// <summary>
        /// Coroutine to manage the end of a music
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private IEnumerator MusicEndCoroutine(AudioSource source)
        {
            float triggerTime = Mathf.Max(source.clip.length - Mathf.Abs(timeFader), 0f);

            while (source == currentMusicSource)
            {
                if (isApplicationSuspended)
                {
                    yield return null;
                    continue;
                }

                if (source.time >= triggerTime)
                {
                    break;
                }

                yield return null;
            }

            if (source != currentMusicSource)
            {
                musicEndCoroutine = null;
                yield break;
            }

            musicEndCoroutine = null;

            EventBus.Publish<OnMusicNearEnd>(new OnMusicNearEnd(source.clip));

            if (currentMusicContainer != null)
            {
                GetNextMusic();
            }
        }

        /// <summary>
        /// Play a musique base on the current MusicClipContainer
        /// </summary>
        /// <param name="container"></param>
        private void PlayMusicContainer(OnNewMusicContainer container)
        {
            if (allSounds == null || allSounds.Count == 0)
            {
                return;
            }

            MusicContainer containe = allSounds.Find(c => c != null && c.Type == container.Context);

            if (containe == null || containe.Container.MusicList == null || containe.Container.MusicList.Count == 0)
            {
                return;
            }

            currentMusicContainer = containe.Container;
            currentIndexMusic = -1;

            GetNextMusic();
        }

        /// <summary>
        /// Get the next music in the container
        /// </summary>
        private void GetNextMusic()
        {
            if (currentMusicContainer == null || currentMusicContainer.MusicList == null || currentMusicContainer.MusicList.Count == 0)
            {
                return;
            }

            if (currentMusicContainer.IsRandom)
            {
                int nextIndex = 0;

                do
                {
                    nextIndex = UnityEngine.Random.Range(0, currentMusicContainer.MusicList.Count);
                }
                while (currentMusicContainer.MusicList.Count > 1 && nextIndex == currentIndexMusic);

                currentIndexMusic = nextIndex;
            }
            else
            {
                currentIndexMusic++;

                if (currentIndexMusic >= currentMusicContainer.MusicList.Count)
                {
                    if (!currentMusicContainer.IsLooping)
                    {
                        return;
                    }

                    currentIndexMusic = 0;
                }
            }

            PlayMusic(currentMusicContainer.MusicList[currentIndexMusic], false);
        }

        /// <summary>
        /// Manage the suspension of the application
        /// </summary>
        /// <param name="focus"></param>
        private void SuspendedApplication(OnApplicationSuspensionChanged focus)
        {
            isApplicationSuspended = focus.IsSuspended;
        }

        /// <summary>
        /// Subscribe in the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnPlaySoundEvent>(Play);
            EventBus.Subscribe<OnSetMixerValue>(SetMixer);
            EventBus.Subscribe<OnNewMusicContainer>(PlayMusicContainer);
            EventBus.Subscribe<OnApplicationSuspensionChanged>(SuspendedApplication);
            EventBus.Subscribe<OnPlayerSoundEvent>(PlayRandomPlayerSound);
        }

        /// <summary>
        /// Unsubscribe with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnPlaySoundEvent>(Play);
            EventBus.Unsubscribe<OnSetMixerValue>(SetMixer);
            EventBus.Unsubscribe<OnNewMusicContainer>(PlayMusicContainer);
            EventBus.Unsubscribe<OnApplicationSuspensionChanged>(SuspendedApplication);
            EventBus.Unsubscribe<OnPlayerSoundEvent>(PlayRandomPlayerSound);
        }

        #endregion
    }
}