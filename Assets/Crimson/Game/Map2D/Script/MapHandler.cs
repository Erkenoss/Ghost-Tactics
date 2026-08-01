using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

#if UNITY_STANDALONE
using UnityEngine.EventSystems;
#endif

namespace Crimson.Map
{
    public class MapHandler : MonoBehaviour
#if UNITY_STANDALONE
        , IDragHandler
#endif
    {
        #region Public Fields
        #endregion

        #region Private Fields

        [Tooltip("The height margin of the map when the player drag before to reach the limit")]
        [SerializeField]
        private float verticalPadding = 0f;

        [Tooltip("The width margin of the map when the player drag before to reach the limit")]
        [SerializeField]
        private float horizontalPadding = 0f;

        [Tooltip("The transform of the map viewport to be used to calculate the limits of the map content")]
        [SerializeField]
        private RectTransform mapViewport = null;

        [Tooltip("The transform of the map content to be moved when the player drag")]
        [SerializeField]
        private RectTransform mapContent = null;

        [Tooltip("The minimum zoom of the map content")]
        [SerializeField]
        private float minZoom = 1f;

        [Tooltip("The maximum zoom of the map content")]
        [SerializeField]
        private float maxZoom = 1f;

        /// <summary>
        /// Initiale position of the map
        /// </summary>
        private Vector2 mapContentInitialPosition = Vector2.zero;

        /// <summary>
        /// Use to store the local position of the first finger when the player start to drag the map content
        /// </summary>
        private Vector2 currentPointerLocalPosition = Vector2.zero;

        /// <summary>
        /// Use to store the initial zoom scale of the map content when the player start to zoom/unzoom the map content
        /// </summary>
        private float InitialZoomScale = 1f;

#if UNITY_ANDROID
        [Tooltip("The threshold before the player can start to drag the map content. ON ANDROID ONLY")]
        [SerializeField]
        private float dragThreshold = 0;

        /// <summary>
        /// The current finger that is being used to drag the map content
        /// </summary>
        private Finger firstFinger = null;

        /// <summary>
        /// The second finger that is being used to zoom/unzoom the map content
        /// </summary>
        private Finger secondFinger = null;

        /// <summary>
        /// Current position of the finger on the screen
        /// </summary>
        private Vector2 startFirstFinger = Vector2.zero;

        /// <summary>
        /// Current position of the second finger on the screen
        /// </summary>
        private Vector2 startSecondFinger = Vector2.zero;

        /// <summary>
        /// Second position to out for the second finger when the player start to zoom/unzoom the map content
        /// </summary>
        private Vector2 secondCurrentPointerLocalPosition = Vector2.zero;

        /// <summary>
        /// Initiali distance between two finder at the start of the zoom/unzoom.
        /// </summary>
        private float initialDistanceBetweenFingers = 0f;

        /// <summary>
        /// Use to know if the player is zooming or not
        /// </summary>
        private bool isZooming = false;

        /// <summary>
        /// Use to know if the player can drag the map content or not. The player can drag the map content when the finger reach the threshold
        /// </summary>
        private bool isThresholdReach = false;
#endif

        #endregion

        #region MonoBehaviour Callbacks

        private void Awake()
        {
#if UNITY_ANDROID
            EnhancedTouchSupport.Enable();

            EnhancedTouch.onFingerDown += OnFingerDown;
            EnhancedTouch.onFingerMove += OnFingerMove;
            EnhancedTouch.onFingerUp += OnFingerUp;
#endif
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID
            EnhancedTouch.onFingerDown -= OnFingerDown;
            EnhancedTouch.onFingerMove -= OnFingerMove;
            EnhancedTouch.onFingerUp -= OnFingerUp;

            EnhancedTouchSupport.Disable();
#endif
        }

        #endregion

        #region Public Methods

#if UNITY_STANDALONE
        /// <summary>
        /// When the player drags the map
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            // Handle drag logic here
        }
#endif
        #endregion

        #region Private Methods

#if UNITY_ANDROID
        /// <summary>
        /// Use to know when the player touch the screen with a finger
        /// </summary>
        /// <param name="finger"></param>
        private void OnFingerDown(Finger finger)
        {
            if (EnhancedTouch.activeFingers.Count > 2)
            {
                firstFinger = null;
                secondFinger = null;

                isThresholdReach = false;
                isZooming = false;

                initialDistanceBetweenFingers = 0f;

                return;
            }

            if (EnhancedTouch.activeFingers.Count == 1)
            {
                firstFinger = finger;
                startFirstFinger = finger.screenPosition;
            }
            else if (EnhancedTouch.activeFingers.Count == 2)
            {
                secondFinger = finger;
                startSecondFinger = finger.screenPosition;

                initialDistanceBetweenFingers = Vector2.Distance(firstFinger.screenPosition, secondFinger.screenPosition);
                
                isZooming = false;
                isThresholdReach = false;
            }
        }


        /// <summary>
        /// Use to know when the player move the finger on the screen
        /// </summary>
        /// <param name="finger"></param>
        private void OnFingerMove(Finger finger)
        {
            if (EnhancedTouch.activeFingers.Count > 2)
            {
                firstFinger = null;
                secondFinger = null;

                isThresholdReach = false;
                isZooming = false;
                
                initialDistanceBetweenFingers = 0f;

                return;
            }

            if (EnhancedTouch.activeFingers.Count == 1 && finger == firstFinger)
            {
                if (!isThresholdReach && (startFirstFinger - finger.screenPosition).magnitude >= dragThreshold)
                {
                    isThresholdReach = true;
                    startFirstFinger = finger.screenPosition;
                    mapContentInitialPosition = mapContent.anchoredPosition;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport, startFirstFinger, null, out currentPointerLocalPosition);
                }

                if (isThresholdReach)
                {
                    MovementHandler(finger.screenPosition);
                }
            }
            else if(EnhancedTouch.activeFingers.Count == 2)
            {
                isThresholdReach = false;

                startFirstFinger = firstFinger.screenPosition;
                startSecondFinger = secondFinger.screenPosition;

                float distance = Vector2.Distance(startFirstFinger, startSecondFinger);

                if (!isZooming && Mathf.Abs(initialDistanceBetweenFingers - distance) >= dragThreshold)
                {

                    isZooming = true;
                    InitialZoomScale = mapContent.localScale.x;
                    initialDistanceBetweenFingers = Vector2.Distance(startFirstFinger, startSecondFinger);

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport, startFirstFinger, null, out currentPointerLocalPosition);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport, startSecondFinger, null, out secondCurrentPointerLocalPosition);
                }

                if (isZooming)
                {
                    float scaleFactor = distance / initialDistanceBetweenFingers;
                    ZoomHandler(scaleFactor);
                }
            }
        }

        /// <summary>
        /// Use to know when the player release the finger on the screen
        /// </summary>
        /// <param name="finger"></param>
        private void OnFingerUp(Finger finger)
        {
            if (finger == firstFinger)
            {
                firstFinger = null;
                startFirstFinger = Vector2.zero;
                isThresholdReach = false;
            }
            else if (finger == secondFinger)
            {
                secondFinger = null;
                startSecondFinger = Vector2.zero;
            }

            isZooming = false;
            initialDistanceBetweenFingers = 0f;
        }

#endif

        /// <summary>
        /// Use to move the mapContent when drag
        /// </summary>
        private void MovementHandler(Vector2 target)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport, target, null, out Vector2 localPosition);

            float contentWidth = mapContent.rect.width * Mathf.Abs(mapContent.localScale.x);
            float contentHeight = mapContent.rect.height * Mathf.Abs(mapContent.localScale.y);
            float horizontalLimit = (contentWidth - mapViewport.rect.width) / 2 - horizontalPadding;
            float verticalLimit = (contentHeight - mapViewport.rect.height) / 2 - verticalPadding;

            Vector2 drag = localPosition - currentPointerLocalPosition;
            Vector2 targetPosition = mapContentInitialPosition + drag;
            mapContent.anchoredPosition = new Vector2(Mathf.Clamp(targetPosition.x, -horizontalLimit, horizontalLimit), Mathf.Clamp(targetPosition.y, -verticalLimit, verticalLimit));
        }

        /// <summary>
        /// Use to zoom on the map
        /// </summary>
        private void ZoomHandler(float zoomFactor)
        {
            float targetScale = Mathf.Clamp(InitialZoomScale * zoomFactor, minZoom, maxZoom);
            mapContent.localScale = new Vector3(targetScale, targetScale, 1f);

            float contentWidth = mapContent.rect.width * Mathf.Abs(targetScale);
            float contentHeight = mapContent.rect.height * Mathf.Abs(targetScale);

            float horizontalLimit = Mathf.Max(0f, (contentWidth - mapViewport.rect.width) / 2f - horizontalPadding);
            float verticalLimit = Mathf.Max(0f, (contentHeight - mapViewport.rect.height) / 2f - verticalPadding);

            Vector2 currentPosition = mapContent.anchoredPosition;
            mapContent.anchoredPosition = new Vector2(Mathf.Clamp(currentPosition.x, -horizontalLimit, horizontalLimit), Mathf.Clamp(currentPosition.y, -verticalLimit, verticalLimit));
        }

        #endregion
    }
}