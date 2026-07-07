using UnityEngine;

namespace Crimson.Core
{
    public abstract class ButtonBase : ScriptableObject, IButtonAction
    {
        public abstract void Execute();
    }
}