using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Framework.Serialization;
using Proxyfan.Presentation.Files;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the gRPC Descriptors tool window. Lets the user load, list, and unload
///     binary protobuf <c>FileDescriptorSet</c> files (typically produced by
///     <c>protoc --descriptor_set_out=...</c>) that the gRPC inspector uses to render
///     captured payloads with named fields and enum value labels.
/// </summary>
public sealed partial class RemoteProcedureCallDescriptorsViewModel : ObservableObject
{
    private const int DescriptorReadBufferSizeInBytes = 81920;
    private const int MaxDescriptorFileSizeInBytes = 10 * 1024 * 1024;
    private readonly IFilePickerService _filePickerService;
    private readonly IRemoteProcedureCallDescriptorLibrary _library;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string? _selectedFilePath;
    [ObservableProperty]
    private string _statusText;

    /// <summary>
    ///     Gets the displayed list of loaded descriptor file paths.
    /// </summary>
    public ObservableCollection<string> LoadedFilePaths { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallDescriptorsViewModel" /> bound to
    ///     the supplied library and file picker.
    /// </summary>
    /// <param name="library">The descriptor library to mutate.</param>
    /// <param name="filePickerService">The file picker used to choose <c>.pb</c> files.</param>
    /// <param name="userInterfaceScheduler">The UI-thread scheduler.</param>
    public RemoteProcedureCallDescriptorsViewModel(
        IRemoteProcedureCallDescriptorLibrary library,
        IFilePickerService filePickerService,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _library = library;
        _filePickerService = filePickerService;
        _userInterfaceScheduler = userInterfaceScheduler;
        var loaded = new ObservableCollection<string>();
        LoadedFilePaths = loaded;
        _statusText = string.Empty;
        _selectedFilePath = null;
        RefreshLoadedFiles();
    }

    [RelayCommand]
    private void Clear()
    {
        _library.Clear();
        RefreshLoadedFiles();
        StatusText = "All descriptor files unloaded.";
    }

    private bool HasRemainingLength(Stream stream, out long remainingLength)
    {
        if (!stream.CanSeek)
        {
            remainingLength = 0;
            return false;
        }

        try
        {
            remainingLength = stream.Length - stream.Position;
            return remainingLength >= 0;
        }
        catch (IOException)
        {
            remainingLength = 0;
            return false;
        }
        catch (NotSupportedException)
        {
            remainingLength = 0;
            return false;
        }
    }

    [RelayCommand]
    private async Task LoadFromFileAsync(CancellationToken cancellationToken)
    {
        var picked = await PickDescriptorAsync(cancellationToken).ConfigureAwait(true);
        if (picked is null)
        {
            return;
        }

        await TryLoadPickedAsync(picked, cancellationToken).ConfigureAwait(true);
    }

    private async Task<FilePickerOpenResult?> PickDescriptorAsync(CancellationToken cancellationToken)
    {
        var request = new FilePickerOpenRequest
        {
            ExtensionDescription = "Protobuf descriptor set",
            FileExtension = "pb",
            Title = "Load descriptor set",
        };
        try
        {
            return await _filePickerService.OpenForReadWithMetadataAsync(request, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private void RefreshLoadedFiles()
    {
        LoadedFilePaths.Clear();
        foreach (var path in _library.LoadedFilePaths)
        {
            LoadedFilePaths.Add(path);
        }

        if (SelectedFilePath is not null && !LoadedFilePaths.Contains(SelectedFilePath))
        {
            SelectedFilePath = null;
        }
    }

    private async Task TryLoadPickedAsync(FilePickerOpenResult picked, CancellationToken cancellationToken)
    {
        try
        {
            await using (picked.Stream.ConfigureAwait(true))
            {
                var sourcePath = string.IsNullOrEmpty(picked.DisplayName) ? "descriptor.pb" : picked.DisplayName;
                var payload = await TryReadBoundedAsync(picked.Stream, cancellationToken).ConfigureAwait(true);
                if (payload is null)
                {
                    _userInterfaceScheduler.Post(() => StatusText = "Descriptor file exceeds the 10 MB size limit.");
                    return;
                }

                _library.Load(sourcePath, payload.Value);
                _userInterfaceScheduler.Post(() =>
                {
                    RefreshLoadedFiles();
                    StatusText = "Loaded " + sourcePath + ".";
                });
            }
        }
        catch (InvalidDataException ex)
        {
            _userInterfaceScheduler.Post(() => StatusText = "Failed to parse descriptor set: " + ex.Message);
        }
        catch (IOException ex)
        {
            _userInterfaceScheduler.Post(() => StatusText = "Failed to read descriptor file: " + ex.Message);
        }
    }

    private async Task<ReadOnlyMemory<byte>?> TryReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (HasRemainingLength(stream, out var remainingLength))
        {
            if (remainingLength > MaxDescriptorFileSizeInBytes)
            {
                return null;
            }

            using var exactBuffer = new MemoryStream(checked((int)remainingLength));
            await stream.CopyToAsync(exactBuffer, DescriptorReadBufferSizeInBytes, cancellationToken).ConfigureAwait(true);
            return exactBuffer.GetBuffer().AsMemory(0, (int)exactBuffer.Length);
        }

        using var boundedBuffer = new MemoryStream();
        var buffer = new byte[DescriptorReadBufferSizeInBytes];
        var total = 0;
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(true);
            if (bytesRead == 0)
            {
                break;
            }

            total += bytesRead;
            if (total > MaxDescriptorFileSizeInBytes)
            {
                return null;
            }

            var writeBuffer = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            await boundedBuffer.WriteAsync(writeBuffer, cancellationToken).ConfigureAwait(true);
        }

        return boundedBuffer.GetBuffer().AsMemory(0, (int)boundedBuffer.Length);
    }

    [RelayCommand]
    private void UnloadSelected()
    {
        var selected = SelectedFilePath;
        if (string.IsNullOrEmpty(selected))
        {
            return;
        }

        _library.Unload(selected);
        RefreshLoadedFiles();
        StatusText = "Unloaded " + selected + ".";
    }
}
