using System;
using System.Reflection;
using Tutorial.Runtime.Components;
using Tutorial.Runtime.Data;
using UnityEngine;

namespace Tutorial.Runtime.Resolution
{
    /// <summary>
    /// Contains every runtime reference resolved from one StepSO method binding
    /// </summary>
    public sealed class TutorialResolvedMethod
    {
        #region Private Fields

        /// <summary>
        /// Runtime StepSO associated with this resolved method binding
        /// </summary>
        private readonly StepSO runtimeStep = null;

        /// <summary>
        /// TutoIdentifier associated with the target tutorial GameObject
        /// </summary>
        private readonly TutoIdentifier identifier = null;

        /// <summary>
        /// MonoBehaviour containing the resolved tutorial method
        /// </summary>
        private readonly MonoBehaviour script = null;

        /// <summary>
        /// Method resolved from the StepSO method binding
        /// </summary>
        private readonly MethodInfo method = null;

        #endregion

        #region Properties

        public StepSO RuntimeStep => runtimeStep;
        public TutoIdentifier Identifier => identifier;
        public MonoBehaviour Script => script;
        public MethodInfo Method => method;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a resolved runtime method binding from its StepSO, target identifier, script and method
        /// </summary>
        /// <param name="runtimeStep"></param>
        /// <param name="identifier"></param>
        /// <param name="script"></param>
        /// <param name="method"></param>
        public TutorialResolvedMethod(StepSO runtimeStep, TutoIdentifier identifier, MonoBehaviour script, MethodInfo method)
        {
            this.runtimeStep = runtimeStep != null ? runtimeStep : throw new ArgumentNullException(nameof(runtimeStep));
            this.identifier = identifier != null ? identifier : throw new ArgumentNullException(nameof(identifier));
            this.script = script != null ? script : throw new ArgumentNullException(nameof(script));
            this.method = method ?? throw new ArgumentNullException(nameof(method));
        }

        #endregion
    }
}