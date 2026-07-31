using UnityEngine;

namespace Crimson.Core
{
    public enum ECharacterSide
    {
        None,
        Player,
        Enemy
    }

    public enum ECharacterAnimation
    {
        None,
        Idle,
        Respawn,
        Attack,
        Dodge,
        Dash,
        BackDash,
        Hit,
        Death,
        Protection
    }

    public class OnAnimationStarted
    {
        #region Public Fields

        public ECharacterAnimation Animation = ECharacterAnimation.None;
        public ECharacterSide Side = ECharacterSide.None;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="characAnim"></param>
        public OnAnimationStarted(ECharacterAnimation anim, ECharacterSide side)
        {
            Animation = anim;
            Side = side;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class OnAnimationEnded
    {
        #region Public Fields

        public ECharacterAnimation Animation = ECharacterAnimation.None;
        public ECharacterSide Side = ECharacterSide.None;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        public OnAnimationEnded(ECharacterAnimation anim, ECharacterSide side)
        {
            Animation = anim;
            Side = side;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class OnAnimationPlay
    {
        #region Public Fields

        public ECharacterAnimation Animation = ECharacterAnimation.None;
        public ECharacterSide Side = ECharacterSide.None;

        #endregion

        #region Private Fields
        #endregion

        #region MonoBehaviour Callbacks

        /// <summary>
        /// Constructor
        /// </summary>
        public OnAnimationPlay(ECharacterAnimation animation, ECharacterSide  side)
        {
            Animation = animation;
            Side = side;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }

    public class CharacterAnimationController : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("Side of the character")]
        [SerializeField]
        private ECharacterSide side = ECharacterSide.None;

        [Tooltip("Animator of the character")]
        [SerializeField]
        private Animator animator = null;

        /// <summary>
        /// Animation currently played
        /// </summary>
        private ECharacterAnimation currentAnimation = ECharacterAnimation.None;
        
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
        /// Play an Animation base on enum
        /// </summary>
        private void PlayAnimation(OnAnimationPlay play)
        {
            if (animator == null || play == null || play.Animation == ECharacterAnimation.None || play.Side != side)
            {
                return;
            }

            currentAnimation = play.Animation;
            animator.Play(play.Animation.ToString(), 0, 0f);
            EventBus.Publish<OnAnimationStarted>(new OnAnimationStarted(currentAnimation, side));
        }

        /// <summary>
        /// When the current animation finished.
        /// </summary>
        private void NotifyEnded()
        {
            EventBus.Publish<OnAnimationEnded>(new OnAnimationEnded(currentAnimation, side));
            currentAnimation = ECharacterAnimation.None;
        }

        /// <summary>
        /// Sub with the EventBus
        /// </summary>
        private void Subscribe()
        {
            EventBus.Subscribe<OnAnimationPlay>(PlayAnimation);
        }

        /// <summary>
        /// Unsub with the EventBus
        /// </summary>
        private void Unsubscribe()
        {
            EventBus.Unsubscribe<OnAnimationPlay>(PlayAnimation);
        }

        #endregion
    }
}