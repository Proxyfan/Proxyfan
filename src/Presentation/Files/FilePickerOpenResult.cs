using System.IO;

namespace Proxyfan.Presentation.Files;

/// <summary>
///     Result of <see cref="IFilePickerService.OpenForReadWithMetadataAsync" /> bundling the
///     selected file's open stream and its suggested display name (typically the file name
///     reported by the operating system file picker).
/// </summary>
public sealed class FilePickerOpenResult
{
    /// <summary>
    ///     Gets the suggested display name (e.g. <c>"hello.pb"</c>). Never <see langword="null" />;
    ///     defaults to an empty string when the picker did not surface a name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets the readable stream opened on the selected file.
    /// </summary>
    public required Stream Stream { get; init; }
}
