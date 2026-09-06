namespace Tutorial.Runtime.Data.Completion.UI
{
    /// <summary>
    /// UI element categories supported by tutorial completion data
    /// </summary>
    public enum EUIElementType
    {
        None,
        Button,
        Toggle,
        Slider,
        Scrollbar,
        Dropdown,
        InputField,
        ScrollRect
    }

    /// <summary>
    /// Define whether a value change alone is sufficient or whether a specific value must be reached
    /// </summary>
    public enum EUIValueCompletionMode
    {
        AnyChange,
        ExpectedValue
    }

    /// <summary>
    /// Comparison operations available for numeric UI completion conditions
    /// </summary>
    public enum EUIComparisonType
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    /// <summary>
    /// Completion modes available for text input UI elements
    /// </summary>
    public enum EUITextCompletionMode
    {
        AnySubmission,
        ExpectedText
    }

    /// <summary>
    /// Completion modes available for ScrollRect UI elements
    /// </summary>
    public enum EUIScrollCompletionMode
    {
        AnyScroll,
        ExpectedPosition
    }

    /// <summary>
    /// Axis used when evaluating a ScrollRect normalized position
    /// </summary>
    public enum EUIScrollAxis
    {
        Horizontal,
        Vertical
    }
}