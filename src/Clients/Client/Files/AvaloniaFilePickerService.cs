using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Proxyfan.Presentation.Files;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Files;

/// <summary>
///     Avalonia implementation of <see cref="IFilePickerService" /> backed by
///     <see cref="IStorageProvider" />. The active top-level (window) must be
///     registered via <see cref="RegisterTopLevel" /> before the picker can be shown.
/// </summary>
public sealed class AvaloniaFilePickerService : IFilePickerService
{
    private TopLevel? _topLevel;

    /// <inheritdoc />
    public async Task<Stream?> OpenForReadAsync(FilePickerOpenRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var topLevel = _topLevel;
        if (topLevel is null)
        {
            return null;
        }

        List<string> patterns = ["*." + request.FileExtension];
        var fileType = new FilePickerFileType(request.ExtensionDescription)
        {
            Patterns = patterns,
        };
        List<FilePickerFileType> filters = [fileType];
        var options = new FilePickerOpenOptions
        {
            Title = request.Title,
            AllowMultiple = false,
            FileTypeFilter = filters,
        };

        var results = await topLevel.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(false);
        if (results is null || results.Count == 0)
        {
            return null;
        }

        return await results[0].OpenReadAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream?> OpenForWriteAsync(FilePickerSaveRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var topLevel = _topLevel;
        if (topLevel is null)
        {
            return null;
        }

        List<string> patterns = ["*." + request.FileExtension];
        var fileType = new FilePickerFileType(request.ExtensionDescription)
        {
            Patterns = patterns,
        };
        List<FilePickerFileType> choices = [fileType];
        var options = new FilePickerSaveOptions
        {
            Title = request.Title,
            SuggestedFileName = request.DefaultFileName,
            DefaultExtension = request.FileExtension,
            FileTypeChoices = choices,
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(false);
        if (file is null)
        {
            return null;
        }

        return await file.OpenWriteAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Registers the active <see cref="TopLevel" /> used to display the picker dialogs.
    ///     Call this from the main window's <c>OnAttachedToVisualTree</c> handler.
    /// </summary>
    /// <param name="topLevel">The top-level visual hosting the application.</param>
    public void RegisterTopLevel(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }
}
