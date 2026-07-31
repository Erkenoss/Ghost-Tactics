using UnityEngine;

namespace Crimson.Core
{
    [ExecuteAlways]
    public sealed class ResponsiveDynamicUI : MonoBehaviour
    {
        [Tooltip("Zone use to calculate the size")]
        [SerializeField]
        private RectTransform sizeReference;

        [Tooltip("Size of the zone")]
        [SerializeField]
        private Vector2 referenceSize = new(1920f, 1080f);

        [Tooltip("Normal scale of the object")]
        [SerializeField]
        private Vector3 referenceScale = Vector3.one;

        /// <summary>
        /// The last Size of the object
        /// </summary>
        private Vector2 lastSize;

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (sizeReference == null)
            {
                return;
            }

            Vector2 currentSize = sizeReference.rect.size;

            if (currentSize != lastSize)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Refresh the display of the object
        /// </summary>
        private void Refresh()
        {
            if (sizeReference == null)
                return;

            if (referenceSize.x <= 0f || referenceSize.y <= 0f)
            if (referenceSize.x <= 0f || referenceSize.y <= 0f)
                return;

            Vector2 currentSize = sizeReference.rect.size;

            float widthRatio = currentSize.x / referenceSize.x;
            float heightRatio = currentSize.y / referenceSize.y;

            float scaleFactor = Mathf.Min(widthRatio, heightRatio);

            transform.localScale = referenceScale * scaleFactor;

            lastSize = currentSize;
        }

        /// <summary>
        /// Keep the actual size
        /// </summary>
        [ContextMenu("Capturer la taille actuelle")]
        private void CaptureCurrentSize()
        {
            if (sizeReference == null)
            {
                return;
            }

            referenceSize = sizeReference.rect.size;
            referenceScale = transform.localScale;
            lastSize = referenceSize;
        }
    }
}