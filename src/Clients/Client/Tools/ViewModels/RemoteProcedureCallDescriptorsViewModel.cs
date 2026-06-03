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
    private const int ReadBufferSizeInBytes = 8192;
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

    private bool HasRemainingByteCount(Stream stream, out int remainingByteCount)
    {
        remainingByteCount = 0;
        if (!stream.CanSeek)
        {
            return false;
        }

        var remainingBytes = stream.Length - stream.Position;
        if (remainingBytes <= 0)
        {
            return true;
        }

        if (remainingBytes > int.MaxValue)
        {
            remainingByteCount = int.MaxValue;
            return true;
        }

        remainingByteCount = (int)remainingBytes;
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

    private async Task TryLoadPickedAsync(FilePickerOpenResult picked, CancellationToken cancellationToken)
    {
        try
        {
            await using (picked.Stream.ConfigureAwait(true))
            {
                var sourcePath = string.IsNullOrEmpty(picked.DisplayName) ? "descriptor.pb" : picked.DisplayName;
                var payload = await TryReadPayloadAsync(picked.Stream, cancellationToken).ConfigureAwait(true);
                if (!payload.HasValue)
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

    private async Task<ReadOnlyMemory<byte>?> TryReadPayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (HasRemainingByteCount(stream, out var remainingByteCount))
        {
            if (remainingByteCount > MaxDescriptorFileSizeInBytes)
            {
                return null;
            }

            var exactPayload = new byte[remainingByteCount];
            var exactBuffer = new Memory<byte>(exactPayload);
            await stream.ReadExactlyAsync(exactBuffer, cancellationToken).ConfigureAwait(true);
            return exactPayload;
        }

        var payload = new byte[ReadBufferSizeInBytes];
        var totalBytesRead = 0;
        while (true)
        {
            if (totalBytesRead == payload.Length)
            {
                if (payload.Length == MaxDescriptorFileSizeInBytes)
                {
                    var probeBuffer = new byte[1];
                    var probeReadBuffer = new Memory<byte>(probeBuffer);
                    var probeBytesRead = await stream.ReadAsync(probeReadBuffer, cancellationToken).ConfigureAwait(true);
                    if (probeBytesRead == 0)
                    {
                        var exactPayload = new ReadOnlyMemory<byte>(payload, 0, totalBytesRead);
                        return exactPayload;
                    }

                    return null;
                }

                var nextBufferLength = Math.Min(payload.Length * 2, MaxDescriptorFileSizeInBytes);
                Array.Resize(ref payload, nextBufferLength);
            }

            var readBuffer = new Memory<byte>(payload, totalBytesRead, payload.Length - totalBytesRead);
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(true);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
        }

        var bufferedPayload = new ReadOnlyMemory<byte>(payload, 0, totalBytesRead);
        return bufferedPayload;
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
