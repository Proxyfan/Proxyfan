using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Files;

/// <summary>
///     Abstraction over the platform file picker so view models can ask the user to choose
///     a file to open or to save without coupling to a UI framework.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    ///     Prompts the user to select an existing file to open. Returns a read-only stream
    ///     opened on the chosen file, or <c>null</c> when the user cancels the dialog.
    /// </summary>
    /// <param name="request">The file open request describing the desired file type and dialog title.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A readable stream of the selected file, or <c>null</c> if cancelled.</returns>
    Task<Stream?> OpenForReadAsync(FilePickerOpenRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///     Prompts the user to select an existing file to open and returns both the stream
    ///     and the picker's suggested display name (the file name as reported by the OS
    ///     dialog). Returns <see langword="null" /> when the user cancels the dialog.
    /// </summary>
    /// <param name="request">The file open request describing the desired file type and dialog title.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A result with stream + display name, or <see langword="null" /> if cancelled.</returns>
    Task<FilePickerOpenResult?> OpenForReadWithMetadataAsync(FilePickerOpenRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///     Prompts the user to choose a destination path for a new file. Returns a writable
    ///     stream targeting the chosen path, or <c>null</c> when the user cancels the dialog.
    /// </summary>
    /// <param name="request">The file save request describing the desired file type and dialog title.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A writable stream targeting the chosen file, or <c>null</c> if cancelled.</returns>
    Task<Stream?> OpenForWriteAsync(FilePickerSaveRequest request, CancellationToken cancellationToken);
}
