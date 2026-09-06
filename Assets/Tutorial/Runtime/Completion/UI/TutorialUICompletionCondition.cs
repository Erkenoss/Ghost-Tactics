using System;
using UnityEngine;
using UnityEngine.UI;

using Tutorial.Runtime.Data.Completion.UI;

namespace Tutorial.Runtime.Completion.UI
{
    /// <summary>
    /// Runtime condition responsible for observing supported Unity UI interactions
    /// </summary>
    public sealed class TutorialUICompletionCondition : TutorialCompletionCondition
    {
        #region Private Fields

        /// <summary>
        /// Serialized configuration used by the UI completion condition
        /// </summary>
        private readonly TutorialUICompletionData completionData = null;

        /// <summary>
        /// Scene Component observed by the UI completion condition
        /// </summary>
        private readonly Component targetComponent = null;

        /// <summary>
        /// Action used to remove the currently active UI listener
        /// </summary>
        private Action unsubscribeAction = null;

        #endregion

        #region Properties

        public TutorialUICompletionData CompletionData => completionData;
        public Component TargetComponent => targetComponent;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a UI completion condition from its serialized configuration and target Component
        /// </summary>
        /// <param name="completionData"></param>
        /// <param name="targetComponent"></param>
        public TutorialUICompletionCondition(TutorialUICompletionData completionData, Component targetComponent)
        {
            this.completionData = completionData;
            this.targetComponent = targetComponent;
        }

        #endregion

        #region Protected Methods

        protected override bool OnArm(out string error)
        {
            error = string.Empty;

            if (completionData == null)
            {
                error = "The UI completion condition contains no completion data.";
                return false;
            }

            if (targetComponent == null)
            {
                error = $"The UI completion condition '{completionData.ElementType}' contains no target Component.";
                return false;
            }

            switch (completionData.ElementType)
            {
                case EUIElementType.Button:
                    return TryArmButton(out error);

                default:
                    error = $"The UI element type '{completionData.ElementType}' is not supported at runtime yet.";
                    return false;
            }
        }

        protected override void OnDisarm()
        {
            unsubscribeAction?.Invoke();
            unsubscribeAction = null;
        }

        #endregion

        #region Button

        /// <summary>
        /// Subscribe to a Button click
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryArmButton(out string error)
        {
            error = string.Empty;

            if (!TryGetData(out TutorialUIButtonCompletionData _, out error))
            {
                return false;
            }

            if (!TryGetTarget(out Button button, out error))
            {
                return false;
            }

            button.onClick.AddListener(OnButtonClicked);

            unsubscribeAction = () =>
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(OnButtonClicked);
                }
            };

            return true;
        }

        /// <summary>
        /// Complete the current condition when the targeted Button is clicked
        /// </summary>
        private void OnButtonClicked()
        {
            Complete();
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate and retrieve the target Component expected by the current UI condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryGetTarget<T>(out T target, out string error) where T : Component
        {
            target = null;
            error = string.Empty;

            if (targetComponent == null)
            {
                error = $"The UI completion condition requires a {typeof(T).Name}, but no target Component was provided.";
                return false;
            }

            target = targetComponent as T;

            if (target == null)
            {
                error = $"The UI completion condition requires a {typeof(T).Name}, but received {targetComponent.GetType().Name}.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate and retrieve the completion data expected by the current UI condition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool TryGetData<T>(out T data, out string error) where T : TutorialUICompletionData
        {
            data = completionData as T;
            error = string.Empty;

            if (data != null)
            {
                return true;
            }

            error = completionData == null ? $"The UI completion condition requires {typeof(T).Name}, but no completion data was provided." : $"The UI completion condition requires {typeof(T).Name}, but received {completionData.GetType().Name}.";

            return false;
        }

        #endregion
    }
}