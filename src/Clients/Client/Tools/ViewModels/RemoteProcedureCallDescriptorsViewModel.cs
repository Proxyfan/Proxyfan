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
    private const int DescriptorReadBufferSizeInBytes = 8192;
    private const int MaximumDescriptorPayloadSizeInBytes = 8 * 1024 * 1024;
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
            var remainingLength = stream.Length - stream.Position;
            if (remainingLength > MaximumDescriptorPayloadSizeInBytes)
            {
                return null;
            }
        }

        using var memory = new MemoryStream();
        var buffer = new byte[DescriptorReadBufferSizeInBytes];
        var totalBytesRead = 0;
        while (true)
        {
            var readBuffer = new Memory<byte>(buffer);
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
            if (totalBytesRead > MaximumDescriptorPayloadSizeInBytes)
            {
                return null;
            }

            var writeBuffer = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            await memory.WriteAsync(writeBuffer, cancellationToken).ConfigureAwait(false);
        }

        if (memory.Length > MaximumDescriptorPayloadSizeInBytes)
        {
            return null;
        }

        var payloadLength = (int)memory.Length;
        var payloadBuffer = memory.GetBuffer();
        if (payloadBuffer.Length == payloadLength)
        {
            return payloadBuffer;
        }

        var payload = new byte[payloadLength];
        Buffer.BlockCopy(payloadBuffer, 0, payload, 0, payloadLength);
        return payload;
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
        var sourcePath = string.IsNullOrEmpty(picked.DisplayName) ? "descriptor.pb" : picked.DisplayName;
        try
        {
            await using (picked.Stream.ConfigureAwait(true))
            {
                var payload = await ReadDescriptorPayloadAsync(picked.Stream, cancellationToken).ConfigureAwait(true);
                if (payload is null)
                {
                    _userInterfaceScheduler.Post(() => StatusText = "Failed to read descriptor file: descriptor set exceeds maximum allowed size.");
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
