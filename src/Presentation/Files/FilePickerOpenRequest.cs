namespace Proxyfan.Presentation.Files;

/// <summary>
///     Describes a request to open a file via the platform file picker.
/// </summary>
public sealed class FilePickerOpenRequest
{
    /// <summary>
    ///     Gets the human-readable description of the file type (for example, <c>"HTTP Archive (HAR) file"</c>).
    /// </summary>
    public required string ExtensionDescription { get; init; }

    /// <summary>
    ///     Gets the required file extension without the leading dot (for example, <c>"har"</c>).
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    ///     Gets the dialog title displayed to the user.
    /// </summary>
    public required string Title { get; init; }
}
