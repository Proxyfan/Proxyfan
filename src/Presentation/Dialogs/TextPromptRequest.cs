namespace Proxyfan.Presentation.Dialogs;

/// <summary>
///     Describes a modal text prompt to display to the user.
/// </summary>
public sealed class TextPromptRequest
{
    /// <summary>
    ///     Gets the initial value that pre-fills the input. <c>null</c> or empty leaves the
    ///     input blank.
    /// </summary>
    public required string? InitialValue { get; init; }

    /// <summary>
    ///     Gets the descriptive label rendered above the input, e.g. "Comment:".
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    ///     Gets the dialog window title rendered in the title bar.
    /// </summary>
    public required string Title { get; init; }
}
