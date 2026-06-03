using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Files;
using Proxyfan.Presentation.RemoteProcedureCall;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallDescriptorsViewModel" /> covering load, unload,
///     clear, error reporting, and cancelled-picker behaviour.
/// </summary>
public sealed class RemoteProcedureCallDescriptorsViewModelTests
{
    /// <summary>
    ///     A freshly-constructed view model exposes an empty file list.
    /// </summary>
    [Test]
    public async Task Construct_Empty_HasNoLoadedFiles()
    {
        var library = new StubDescriptorLibrary();
        var picker = new StubPickerService();
        var viewModel = CreateViewModel(library, picker);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Loading a descriptor file populates the file list and updates the status text.
    /// </summary>
    [Test]
    public async Task LoadFromFile_ValidDescriptorSet_AppearsInFileList()
    {
        var library = new StubDescriptorLibrary();
        var picker = new StubPickerService
        {
            Stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }),
            DisplayName = "test.pb",
        };
        var viewModel = CreateViewModel(library, picker);

        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(1);
        await Assert.That(viewModel.LoadedFilePaths[0]).IsEqualTo("test.pb");
        await Assert.That(viewModel.StatusText).Contains("Loaded");
    }

    /// <summary>
    ///     A cancelled picker (null result) is a silent no-op.
    /// </summary>
    [Test]
    public async Task LoadFromFile_CancelledPicker_NoChangeToLibrary()
    {
        var library = new StubDescriptorLibrary();
        var picker = new StubPickerService { Stream = null };
        var viewModel = CreateViewModel(library, picker);

        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     A malformed descriptor payload is reported as a parse failure in the status text.
    /// </summary>
    [Test]
    public async Task LoadFromFile_MalformedPayload_ReportsParseFailure()
    {
        var library = new StubDescriptorLibrary
        {
            LoadException = new InvalidDataException("Malformed descriptor set."),
        };
        var picker = new StubPickerService
        {
            Stream = new MemoryStream(new byte[] { 0x80 }),
            DisplayName = "broken.pb",
        };
        var viewModel = CreateViewModel(library, picker);

        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).Contains("Failed to parse");
    }

    /// <summary>
    ///     UnloadSelected removes the selected entry and updates the status text.
    /// </summary>
    [Test]
    public async Task UnloadSelected_AfterLoad_RemovesEntry()
    {
        var library = new StubDescriptorLibrary();
        var picker = new StubPickerService { Stream = new MemoryStream(new byte[] { 0x01 }), DisplayName = "a.pb" };
        var viewModel = CreateViewModel(library, picker);
        await viewModel.LoadFromFileCommand.ExecuteAsync(null);
        viewModel.SelectedFilePath = "a.pb";

        viewModel.UnloadSelectedCommand.Execute(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).Contains("Unloaded");
    }

    /// <summary>
    ///     UnloadSelected with no selection is a silent no-op.
    /// </summary>
    [Test]
    public async Task UnloadSelected_NoSelection_NoOp()
    {
        var library = new StubDescriptorLibrary();
        var picker = new StubPickerService();
        var viewModel = CreateViewModel(library, picker);

        viewModel.UnloadSelectedCommand.Execute(null);

        await Assert.That(viewModel.StatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Clear empties the library.
    /// </summary>
    [Test]
    public async Task Clear_AfterLoad_EmptiesLibrary()
    {
        var library = new StubDescriptorLibrary();
        var picker = new StubPickerService { Stream = new MemoryStream(new byte[] { 0x01 }), DisplayName = "a.pb" };
        var viewModel = CreateViewModel(library, picker);
        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        viewModel.ClearCommand.Execute(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).Contains("unloaded");
    }

    private static RemoteProcedureCallDescriptorsViewModel CreateViewModel(
        StubDescriptorLibrary library,
        StubPickerService picker)
    {
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);
        return viewModel;
    }

    private sealed class StubDescriptorLibrary : IRemoteProcedureCallDescriptorFileLibrary
    {
        private readonly List<string> _loadedFilePaths;

        public InvalidDataException? LoadException { get; set; }

        public StubDescriptorLibrary()
        {
            _loadedFilePaths = new List<string>();
        }

        public void Clear()
        {
            _loadedFilePaths.Clear();
        }

        public void Load(string sourcePath, byte[] payload)
        {
            _ = payload;
            if (LoadException is not null)
            {
                throw LoadException;
            }

            _loadedFilePaths.Add(sourcePath);
        }

        public IReadOnlyList<string> LoadedFilePaths => _loadedFilePaths;

        public void Unload(string sourcePath)
        {
            _loadedFilePaths.Remove(sourcePath);
        }
    }

    private sealed class StubPickerService : IFilePickerService
    {
        public string DisplayName { get; set; } = string.Empty;

        public Stream? Stream { get; set; }

        public Task<Stream?> OpenForReadAsync(FilePickerOpenRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Stream);
        }

        public Task<FilePickerOpenResult?> OpenForReadWithMetadataAsync(FilePickerOpenRequest request, CancellationToken cancellationToken)
        {
            if (Stream is null)
            {
                return Task.FromResult<FilePickerOpenResult?>(null);
            }

            var result = new FilePickerOpenResult
            {
                DisplayName = DisplayName,
                Stream = Stream,
            };
            return Task.FromResult<FilePickerOpenResult?>(result);
        }

        public Task<Stream?> OpenForWriteAsync(FilePickerSaveRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream?>(null);
        }
    }
}
