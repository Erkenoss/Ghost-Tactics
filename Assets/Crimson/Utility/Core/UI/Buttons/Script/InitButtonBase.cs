using UnityEngine;

namespace Crimson.Core
{
    public abstract class InitButtonBase : ButtonBase
    {
        /// <summary>
        /// Ref of the component we want
        /// </summary>
        protected MonoBehaviour obj = null;

        public virtual void Init(MonoBehaviour _obj)
        {
            if (_obj == null)
            {
                return;
            }

            obj = _obj;
        }
    }
}
