using UnityEngine;

namespace Crimson.Interaction
{
    public interface IInteractable
    {
        Transform GetTransform();
        string GetInteractText();
        void Interact(Transform transform);
    }
}
