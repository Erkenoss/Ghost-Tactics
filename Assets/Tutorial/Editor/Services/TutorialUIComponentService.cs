using System;
using UnityEngine;
using UnityEngine.UI;

using Tutorial.Runtime.Data.Completion.UI;

namespace Tutorial.Editor.Services
{
    /// <summary>
    /// Resolve and validate Component types supported by UI tutorial completion data
    /// </summary>
    internal static class TutorialUIComponentService
    {
        #region Public Methods

        /// <summary>
        /// Check whether a Component is compatible with a UI element type
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public static bool IsCompatible(EUIElementType elementType, Component component)
        {
            if (component == null)
            {
                return false;
            }

            switch (elementType)
            {
                case EUIElementType.Button:
                    return component is Button;

                case EUIElementType.Toggle:
                    return component is Toggle;

                case EUIElementType.Slider:
                    return component is Slider;

                case EUIElementType.Scrollbar:
                    return component is Scrollbar;

                case EUIElementType.Dropdown:
                    return component is Dropdown || IsTypeOrBaseTypeNamed(component.GetType(), "TMPro.TMP_Dropdown");

                case EUIElementType.InputField:
                    return component is InputField || IsTypeOrBaseTypeNamed(component.GetType(), "TMPro.TMP_InputField");

                case EUIElementType.ScrollRect:
                    return component is ScrollRect;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the expected Component description associated with a UI element type
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public static string GetExpectedComponentName(EUIElementType elementType)
        {
            switch (elementType)
            {
                case EUIElementType.Button:
                    return nameof(Button);

                case EUIElementType.Toggle:
                    return nameof(Toggle);

                case EUIElementType.Slider:
                    return nameof(Slider);

                case EUIElementType.Scrollbar:
                    return nameof(Scrollbar);

                case EUIElementType.Dropdown:
                    return "Dropdown or TMP_Dropdown";

                case EUIElementType.InputField:
                    return "InputField or TMP_InputField";

                case EUIElementType.ScrollRect:
                    return nameof(ScrollRect);

                default:
                    return "None";
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check whether a Type or one of its base types matches a complete type name
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        private static bool IsTypeOrBaseTypeNamed(Type type, string fullTypeName)
        {
            if (type == null || string.IsNullOrWhiteSpace(fullTypeName))
            {
                return false;
            }

            Type currentType = type;

            while (currentType != null)
            {
                if (string.Equals(currentType.FullName, fullTypeName, StringComparison.Ordinal))
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        #endregion
    }
}