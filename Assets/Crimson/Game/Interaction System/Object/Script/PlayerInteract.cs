using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crimson.Interaction
{
    public class PlayerInteract : MonoBehaviour
    {
        #region Public Fields
        #endregion

        #region Events

        public Action<IInteractable> ShowInteract;

        #endregion

        #region Private Fields

        /// <summary>
        /// Layer of the object to be detect by the ray
        /// </summary>
        [SerializeField]
        private LayerMask interactableMask = new LayerMask();

        /// <summary>
        /// Main Camera of the player
        /// </summary>
        [SerializeField]
        private Camera mainCam = null;

        /// <summary>
        /// Current IInteractable object detect by the ray
        /// </summary>
        private IInteractable current = null;

        #endregion

        #region MonoBehaviour Callbacks

        private void Update()
        {
            InteractionRay();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Interact with an object
        /// </summary>
        public void Interact()
        {
            if (current != null)
            {
                current.Interact(transform);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create a ray cast to check collision with an interactable object
        /// </summary>
        private void InteractionRay()
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 6.0f, interactableMask)) //Check trigger setting to up the detection size if possible
            {
                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    current = interactable;
                    ShowInteract?.Invoke(interactable);
                }
            }
            else if (current != null)
            {
                current = null;
                ShowInteract?.Invoke(null);
            }
            else
            {
                ShowInteract?.Invoke(null);
            }
        }

        #endregion
    }
}