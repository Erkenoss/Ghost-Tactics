using Crimson.Core;
using UnityEngine;

namespace GhostTactics.Core.Combat
{
    public enum EGhostAnimation
    {
        None,
        Interupt
    }

    public class OnGhostAnimationStarted
    {
        #region Public Fields
        #endregion

        #region Private Fields

        public EGhostAnimation Animation = EGhostAnimation.None;

        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="animation"></param>
        public OnGhostAnimationStarted(EGhostAnimation animation)
        {
            Animation = animation;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class OnGhostAnimationPlay
    {
        #region Public Fields
        #endregion

        #region Private Fields

        public EGhostAnimation Animation = EGhostAnimation.None;

        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="animation"></param>
        public OnGhostAnimationPlay(EGhostAnimation animation)
        {
            Animation = animation;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class OnGhostAnimationEnded
    {
        #region Public Fields
        #endregion

        #region Private Fields

        public EGhostAnimation Animation = EGhostAnimation.None;

        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="animation"></param>
        public OnGhostAnimationEnded(EGhostAnimation animation)
        {
            Animation = animation;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class GhostAnimatorController : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Animator used by the Ghost.
        /// </summary>
        [SerializeField]
        private Animator animator = null;

        /// <summary>
        /// Current animation of the ghost
        /// </summary>
        private EGhostAnimation currentAnimation = EGhostAnimation.None;

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            Subscribe();
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
        /// Activates the Ghost and plays its interception animation.
        /// </summary>
        private void PlayAnimation(OnGhostAnimationPlay play)
        {
            if (animator == null || play == null || play.Animation == EGhostAnimation.None)
            {
                return;
            }

            currentAnimation = play.Animation;
            animator.Play(currentAnimation.ToString(), 0, 0f);
            EventBus.Publish<OnGhostAnimationStarted>(new OnGhostAnimationStarted(currentAnimation));
        }

        /// <summary>
        /// Called by an Animation Event at the end of the interception animation.
        /// </summary>
        private void NotifyInterceptionEnded()
        {
            EventBus.Publish<OnGhostAnimationEnded>(new OnGhostAnimationEnded(currentAnimation));
            currentAnimation = EGhostAnimation.None;
        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnGhostAnimationPlay>(PlayAnimation);
        }


        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnGhostAnimationPlay>(PlayAnimation);
        }


        #endregion
    }
}