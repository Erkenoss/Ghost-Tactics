using UnityEngine;
//using Crimson.Inventory;

namespace Crimson.Interaction
{
    public class BaseInteractable : MonoBehaviour, IInteractable
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Data of the object
        /// </summary>
        //[SerializeField]
        //private ObjectSO data = null;

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Return the transform of the object
        /// </summary>
        /// <returns></returns>
        public virtual Transform GetTransform()
        {
            return transform;
        }

        /// <summary>
        /// return the key to display the name on the interact panel
        /// </summary>
        /// <returns></returns>

        public virtual string GetInteractText()
        {
            return null; //data.TranslationNameKey;
        }

        /// <summary>
        /// Interaction with the object
        /// </summary>
        /// <param name="transform"></param>
        public virtual void Interact(Transform transform)
        {
            //if (InventoryManager.Instance != null && data != null)
            //{
            //    bool pickUp = InventoryManager.Instance.AddToInventory(data);

            //    if (pickUp)
            //    {
            //        gameObject.SetActive(false);
            //    }
            //}
        }

        #endregion

        #region Private Methods
        #endregion
    }
}
