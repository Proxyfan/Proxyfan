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
        var library = new StubDescriptorFileLibrary();
        var picker = new StubPickerService();
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Loading a descriptor file populates the file list and updates the status text.
    /// </summary>
    [Test]
    public async Task LoadFromFile_ValidDescriptorSet_AppearsInFileList()
    {
        var library = new StubDescriptorFileLibrary();
        var setBytes = BuildEmptyDescriptorSet("test.proto", "demo");
        var picker = new StubPickerService
        {
            Stream = new MemoryStream(setBytes),
            DisplayName = "test.pb",
        };
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);

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
        var library = new StubDescriptorFileLibrary();
        var picker = new StubPickerService { Stream = null };
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);

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
        var library = new StubDescriptorFileLibrary
        {
            LoadException = new InvalidDataException("Malformed descriptor set."),
        };
        var picker = new StubPickerService
        {
            Stream = new MemoryStream(new byte[] { 0x80 }),
            DisplayName = "broken.pb",
        };
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);

        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).Contains("Failed to parse");
    }

    /// <summary>
    ///     A file that exceeds the maximum size limit is rejected with an appropriate status message.
    /// </summary>
    [Test]
    public async Task LoadFromFile_OversizeFile_ReportsSizeLimitExceeded()
    {
        var library = new StubDescriptorFileLibrary();
        var picker = new StubPickerService
        {
            Stream = new OversizeStream(11 * 1024 * 1024),
            DisplayName = "huge.pb",
        };
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);

        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).Contains("size limit");
    }

    /// <summary>
    ///     UnloadSelected removes the selected entry and updates the status text.
    /// </summary>
    [Test]
    public async Task UnloadSelected_AfterLoad_RemovesEntry()
    {
        var library = new StubDescriptorFileLibrary();
        var setBytes = BuildEmptyDescriptorSet("a.proto", "a");
        var picker = new StubPickerService { Stream = new MemoryStream(setBytes), DisplayName = "a.pb" };
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);
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
        var library = new StubDescriptorFileLibrary();
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, new StubPickerService(), Stubs.InlineUserInterfaceScheduler.Instance);

        viewModel.UnloadSelectedCommand.Execute(null);

        await Assert.That(viewModel.StatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Clear empties the library.
    /// </summary>
    [Test]
    public async Task Clear_AfterLoad_EmptiesLibrary()
    {
        var library = new StubDescriptorFileLibrary();
        var setBytes = BuildEmptyDescriptorSet("a.proto", "a");
        var picker = new StubPickerService { Stream = new MemoryStream(setBytes), DisplayName = "a.pb" };
        var viewModel = new RemoteProcedureCallDescriptorsViewModel(library, picker, Stubs.InlineUserInterfaceScheduler.Instance);
        await viewModel.LoadFromFileCommand.ExecuteAsync(null);

        viewModel.ClearCommand.Execute(null);

        await Assert.That(viewModel.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(viewModel.StatusText).Contains("unloaded");
    }

    private static byte[] BuildEmptyDescriptorSet(string fileName, string package)
    {
        // FileDescriptorSet { repeated FileDescriptorProto file = 1; }
        // FileDescriptorProto { string name = 1; string package = 2; }
        using var memory = new MemoryStream();
        memory.Write(BuildFieldString(1, fileName));
        memory.Write(BuildFieldString(2, package));
        var fileBytes = memory.ToArray();
        return BuildLengthDelimitedField(1, fileBytes);
    }

    private static byte[] BuildLengthDelimitedField(int fieldNumber, byte[] payload)
    {
        var tag = (uint)((fieldNumber << 3) | 2);
        using var memory = new MemoryStream();
        WriteVarint(memory, tag);
        WriteVarint(memory, (uint)payload.Length);
        memory.Write(payload, 0, payload.Length);
        return memory.ToArray();
    }

    private static byte[] BuildFieldString(int fieldNumber, string value)
    {
        var encoded = System.Text.Encoding.UTF8.GetBytes(value);
        return BuildLengthDelimitedField(fieldNumber, encoded);
    }

    private static void WriteVarint(Stream stream, uint value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        stream.WriteByte((byte)value);
    }

    private sealed class OversizeStream : Stream
    {
        private long _remaining;

        public OversizeStream(long totalBytes)
        {
            _remaining = totalBytes;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining <= 0)
            {
                return 0;
            }

            var toReturn = (int)Math.Min(_remaining, count);
            _remaining -= toReturn;
            return toReturn;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_remaining <= 0)
            {
                return ValueTask.FromResult(0);
            }

            var toReturn = (int)Math.Min(_remaining, buffer.Length);
            _remaining -= toReturn;
            return ValueTask.FromResult(toReturn);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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

    private sealed class StubDescriptorFileLibrary : IRemoteProcedureCallDescriptorFileLibrary
    {
        private readonly List<string> _loadedFilePaths;

        public StubDescriptorFileLibrary()
        {
            _loadedFilePaths = new List<string>();
        }

        public InvalidDataException? LoadException { get; set; }

        public IReadOnlyList<string> LoadedFilePaths => _loadedFilePaths;

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

            _loadedFilePaths.Remove(sourcePath);
            _loadedFilePaths.Add(sourcePath);
        }

        public void Unload(string sourcePath)
        {
            _loadedFilePaths.Remove(sourcePath);
        }
    }
}
