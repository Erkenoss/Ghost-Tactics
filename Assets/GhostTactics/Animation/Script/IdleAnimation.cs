using UnityEngine;

namespace GhostTactics.Animation
{
    public class IdleAnimation : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Private Fields

        public Sprite[] frames;   // Mets tes sprites ici dans l’inspecteur
        public float fps = 12f;

        private SpriteRenderer sr;
        private int currentFrame;
        private float timer;

        #endregion

        #region MonoBehaviour Callbacks

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (frames.Length == 0) return;

            timer += Time.deltaTime;

            if (timer >= 1f / fps)
            {
                timer = 0f;
                currentFrame = (currentFrame + 1) % frames.Length;
                sr.sprite = frames[currentFrame];
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}