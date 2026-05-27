namespace Proxyfan.Presentation.Files;

/// <summary>
///     Describes a request to choose a destination path for a new file via the platform file picker.
/// </summary>
public sealed class FilePickerSaveRequest
{
    /// <summary>
    ///     Gets the default file name suggested to the user (for example, <c>"proxyfan-session.har"</c>).
    /// </summary>
    public required string DefaultFileName { get; init; }

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
