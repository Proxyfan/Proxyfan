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
    private const int MaxDescriptorFileSizeInBytes = 10 * 1024 * 1024;
    private const int ReadBufferSizeInBytes = 64 * 1024;
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

    private bool HasValidInitialBufferSize(Stream stream, out int initialBufferSize)
    {
        initialBufferSize = ReadBufferSizeInBytes;
        if (!stream.CanSeek)
        {
            return true;
        }

        var remaining = stream.Length - stream.Position;
        if (remaining > MaxDescriptorFileSizeInBytes)
        {
            return false;
        }

        if (remaining > 0)
        {
            initialBufferSize = (int)remaining;
        }

        return true;
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

    private byte[] TryGrowPayload(byte[] payload, int currentSize)
    {
        var nextSize = Math.Min(payload.Length * 2, MaxDescriptorFileSizeInBytes);
        var grown = new byte[nextSize];
        Buffer.BlockCopy(payload, 0, grown, 0, currentSize);
        return grown;
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

                _library.Load(sourcePath, payload);
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

    private async Task<byte[]?> TryReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!HasValidInitialBufferSize(stream, out var initialBufferSize))
        {
            return null;
        }

        var payload = new byte[initialBufferSize];
        var total = 0;
        while (true)
        {
            if (total == MaxDescriptorFileSizeInBytes)
            {
                return await TryReadLimitProbeAsync(stream, cancellationToken).ConfigureAwait(true) ? payload : null;
            }

            if (total == payload.Length)
            {
                payload = TryGrowPayload(payload, total);
            }

            var readSlice = new Memory<byte>(payload, total, payload.Length - total);
            var bytesRead = await stream.ReadAsync(readSlice, cancellationToken).ConfigureAwait(true);
            if (bytesRead == 0)
            {
                break;
            }

            total += bytesRead;
            if (total > MaxDescriptorFileSizeInBytes)
            {
                return null;
            }
        }

        if (total == payload.Length)
        {
            return payload;
        }

        return TryTrimPayload(payload, total);
    }

    private async Task<bool> TryReadLimitProbeAsync(Stream stream, CancellationToken cancellationToken)
    {
        var probe = new byte[1];
        var probeRead = await stream.ReadAsync(probe, cancellationToken).ConfigureAwait(true);
        return probeRead == 0;
    }

    private byte[] TryTrimPayload(byte[] payload, int count)
    {
        var trimmed = new byte[count];
        Buffer.BlockCopy(payload, 0, trimmed, 0, count);
        return trimmed;
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
