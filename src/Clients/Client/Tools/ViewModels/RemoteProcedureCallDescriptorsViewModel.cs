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
    private const int MaxDescriptorPayloadBytes = MaxDescriptorPayloadMegabytes * 1024 * 1024;
    private const int MaxDescriptorPayloadMegabytes = 8;
    private const int StreamReadBufferSize = 81920;
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

    private byte[] CopySegment(ArraySegment<byte> segment)
    {
        if (segment.Offset == 0 && segment.Array is not null && segment.Count == segment.Array.Length)
        {
            return segment.Array;
        }

        var payload = new byte[segment.Count];
        if (segment.Array is not null)
        {
            Buffer.BlockCopy(segment.Array, segment.Offset, payload, 0, segment.Count);
        }

        return payload;
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

    private async Task<byte[]?> ReadDescriptorPayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            return await ReadSeekableDescriptorPayloadAsync(stream, cancellationToken).ConfigureAwait(true);
        }

        return await ReadUnseekableDescriptorPayloadAsync(stream, cancellationToken).ConfigureAwait(true);
    }

    private async Task<byte[]?> ReadSeekableDescriptorPayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var remainingLength = stream.Length - stream.Position;
        if (remainingLength < 0)
        {
            throw new IOException("Descriptor stream position is invalid.");
        }

        if (remainingLength > MaxDescriptorPayloadBytes)
        {
            return null;
        }

        var payload = new byte[(int)remainingLength];
        var readOffset = 0;
        while (readOffset < payload.Length)
        {
            var readCount = await stream.ReadAsync(payload.AsMemory(readOffset), cancellationToken).ConfigureAwait(true);
            if (readCount == 0)
            {
                throw new IOException("Descriptor stream ended unexpectedly.");
            }

            readOffset += readCount;
        }

        return payload;
    }

    private async Task<byte[]?> ReadUnseekableDescriptorPayloadAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[StreamReadBufferSize];
        var totalBytesRead = 0;
        while (true)
        {
            var readCount = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(true);
            if (readCount == 0)
            {
                break;
            }

            totalBytesRead += readCount;
            if (totalBytesRead > MaxDescriptorPayloadBytes)
            {
                return null;
            }

            await memory.WriteAsync(buffer.AsMemory(0, readCount), cancellationToken).ConfigureAwait(true);
        }

        if (!memory.TryGetBuffer(out var segment))
        {
            return memory.ToArray();
        }

        return CopySegment(segment);
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
                var payload = await ReadDescriptorPayloadAsync(picked.Stream, cancellationToken).ConfigureAwait(true);
                if (payload is null)
                {
                    _userInterfaceScheduler.Post(() =>
                        StatusText = "Failed to load descriptor set: file exceeds " + MaxDescriptorPayloadMegabytes + " MB limit.");
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
