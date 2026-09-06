using System;
using UnityEngine;

using Tutorial.Runtime.Data.Completion;

namespace Tutorial.Runtime.Data.Completion.UI
{
    /// <summary>
    /// Base serialized data used by UI tutorial completion conditions
    /// </summary>
    [Serializable]
    public abstract class TutorialUICompletionData : TutorialCompletionData
    {
        public abstract EUIElementType ElementType { get; }
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects a Button click
    /// </summary>
    [Serializable]
    public sealed class TutorialUIButtonCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.Button;
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects a Toggle interaction
    /// </summary>
    [Serializable]
    public sealed class TutorialUIToggleCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.Toggle;

        /// <summary>
        /// Define whether any Toggle change is accepted or a specific value is required
        /// </summary>
        [SerializeField]
        private EUIValueCompletionMode completionMode = EUIValueCompletionMode.ExpectedValue;

        /// <summary>
        /// Toggle value required to complete the Step
        /// </summary>
        [SerializeField]
        private bool expectedValue = true;

        public EUIValueCompletionMode CompletionMode { get { return completionMode; } set { completionMode = value; } }
        public bool ExpectedValue { get { return expectedValue; } set { expectedValue = value; } }
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects a Slider interaction
    /// </summary>
    [Serializable]
    public sealed class TutorialUISliderCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.Slider;

        /// <summary>
        /// Define whether any Slider change is accepted or a specific value is required
        /// </summary>
        [SerializeField]
        private EUIValueCompletionMode completionMode = EUIValueCompletionMode.AnyChange;

        /// <summary>
        /// Comparison used when a specific Slider value is required
        /// </summary>
        [SerializeField]
        private EUIComparisonType comparisonType = EUIComparisonType.GreaterOrEqual;

        /// <summary>
        /// Slider value required to complete the Step
        /// </summary>
        [SerializeField]
        private float expectedValue = 0f;

        public EUIValueCompletionMode CompletionMode { get { return completionMode; } set { completionMode = value; } }
        public EUIComparisonType ComparisonType { get { return comparisonType; } set { comparisonType = value; } }
        public float ExpectedValue { get { return expectedValue; } set { expectedValue = value; } }
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects a Scrollbar interaction
    /// </summary>
    [Serializable]
    public sealed class TutorialUIScrollbarCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.Scrollbar;

        /// <summary>
        /// Define whether any Scrollbar change is accepted or a specific value is required
        /// </summary>
        [SerializeField]
        private EUIValueCompletionMode completionMode = EUIValueCompletionMode.AnyChange;

        /// <summary>
        /// Comparison used when a specific Scrollbar value is required
        /// </summary>
        [SerializeField]
        private EUIComparisonType comparisonType = EUIComparisonType.GreaterOrEqual;

        /// <summary>
        /// Scrollbar value required to complete the Step
        /// </summary>
        [SerializeField]
        private float expectedValue = 0f;

        public EUIValueCompletionMode CompletionMode { get { return completionMode; } set { completionMode = value; } }
        public EUIComparisonType ComparisonType { get { return comparisonType; } set { comparisonType = value; } }
        public float ExpectedValue { get { return expectedValue; } set { expectedValue = value; } }
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects a Dropdown interaction
    /// </summary>
    [Serializable]
    public sealed class TutorialUIDropdownCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.Dropdown;

        /// <summary>
        /// Define whether any Dropdown change is accepted or a specific index is required
        /// </summary>
        [SerializeField]
        private EUIValueCompletionMode completionMode = EUIValueCompletionMode.AnyChange;

        /// <summary>
        /// Dropdown index required to complete the Step
        /// </summary>
        [SerializeField]
        private int expectedIndex = 0;

        public EUIValueCompletionMode CompletionMode { get { return completionMode; } set { completionMode = value; } }
        public int ExpectedIndex { get { return expectedIndex; } set { expectedIndex = value; } }
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects an InputField submission
    /// </summary>
    [Serializable]
    public sealed class TutorialUIInputFieldCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.InputField;

        /// <summary>
        /// Define whether any submitted text is accepted or a specific text is required
        /// </summary>
        [SerializeField]
        private EUITextCompletionMode completionMode = EUITextCompletionMode.AnySubmission;

        /// <summary>
        /// Text required to complete the Step
        /// </summary>
        [SerializeField]
        private string expectedText = string.Empty;

        public EUITextCompletionMode CompletionMode { get { return completionMode; } set { completionMode = value; } }
        public string ExpectedText { get { return expectedText; } set { expectedText = value; } }
    }

    /// <summary>
    /// Completion data used when a tutorial Step expects a ScrollRect interaction
    /// </summary>
    [Serializable]
    public sealed class TutorialUIScrollRectCompletionData : TutorialUICompletionData
    {
        public override EUIElementType ElementType => EUIElementType.ScrollRect;

        /// <summary>
        /// Define whether any scrolling is accepted or a specific normalized position is required
        /// </summary>
        [SerializeField]
        private EUIScrollCompletionMode completionMode = EUIScrollCompletionMode.AnyScroll;

        /// <summary>
        /// ScrollRect axis evaluated when a specific position is required
        /// </summary>
        [SerializeField]
        private EUIScrollAxis axis = EUIScrollAxis.Vertical;

        /// <summary>
        /// Comparison used against the expected normalized position
        /// </summary>
        [SerializeField]
        private EUIComparisonType comparisonType = EUIComparisonType.GreaterOrEqual;

        /// <summary>
        /// Normalized ScrollRect position required to complete the Step
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float expectedPosition = 1f;

        public EUIScrollCompletionMode CompletionMode { get { return completionMode; } set { completionMode = value; } }
        public EUIScrollAxis Axis { get { return axis; } set { axis = value; } }
        public EUIComparisonType ComparisonType { get { return comparisonType; } set { comparisonType = value; } }
        public float ExpectedPosition { get { return expectedPosition; } set { expectedPosition = value; } }
    }
}