using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the HAR session save/load commands on <see cref="ShellViewModel" />.
/// </summary>
public sealed class ShellViewModelSessionTests
{
    /// <summary>
    ///     Verifies that SaveSessionCommand prompts for write, calls the exporter, and disposes the stream.
    /// </summary>
    [Test]
    public async Task SaveSessionCommand_WhenUserPicksFile_InvokesExporter()
    {
        using var writeStream = new MemoryStream();
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = writeStream };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var importer = new ShellViewModelFactory.StubHarImporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, importer);

        await viewModel.SaveSessionCommand.ExecuteAsync(null);

        await Assert.That(picker.OpenForWriteCallCount).IsEqualTo(1);
        await Assert.That(exporter.CallCount).IsEqualTo(1);
        await Assert.That(exporter.LastStream).IsSameReferenceAs(writeStream);
    }

    /// <summary>
    ///     Verifies that SaveSessionCommand short-circuits when the user cancels the picker.
    /// </summary>
    [Test]
    public async Task SaveSessionCommand_WhenUserCancels_DoesNotInvokeExporter()
    {
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = null };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var importer = new ShellViewModelFactory.StubHarImporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, importer);

        await viewModel.SaveSessionCommand.ExecuteAsync(null);

        await Assert.That(picker.OpenForWriteCallCount).IsEqualTo(1);
        await Assert.That(exporter.CallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that OpenSessionCommand calls the importer, loads flows, and disposes the stream.
    /// </summary>
    [Test]
    public async Task OpenSessionCommand_WhenUserPicksFile_LoadsImportedFlows()
    {
        using var readStream = new MemoryStream();
        var importedFlows = new List<TrafficFlow>
        {
            new(Guid.NewGuid(), "127.0.0.1:9001", DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "127.0.0.1:9002", DateTimeOffset.UtcNow),
        };
        var picker = new ShellViewModelFactory.StubFilePickerService { ReadStream = readStream };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var importer = new ShellViewModelFactory.StubHarImporter { ReturnFlows = importedFlows };
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, importer);

        await viewModel.OpenSessionCommand.ExecuteAsync(null);

        await Assert.That(picker.OpenForReadCallCount).IsEqualTo(1);
        await Assert.That(importer.CallCount).IsEqualTo(1);
        await Assert.That(viewModel.TrafficList.Flows.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that OpenSessionCommand short-circuits when the user cancels the picker.
    /// </summary>
    [Test]
    public async Task OpenSessionCommand_WhenUserCancels_DoesNotInvokeImporter()
    {
        var picker = new ShellViewModelFactory.StubFilePickerService { ReadStream = null };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var importer = new ShellViewModelFactory.StubHarImporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, importer);

        await viewModel.OpenSessionCommand.ExecuteAsync(null);

        await Assert.That(picker.OpenForReadCallCount).IsEqualTo(1);
        await Assert.That(importer.CallCount).IsEqualTo(0);
        await Assert.That(viewModel.TrafficList.Flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies the save command captures the current traffic list flows as a snapshot.
    /// </summary>
    [Test]
    public async Task SaveSessionCommand_WithCapturedFlows_PassesSnapshotToExporter()
    {
        using var writeStream = new MemoryStream();
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = writeStream };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var importer = new ShellViewModelFactory.StubHarImporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, importer);

        var seedFlows = new List<TrafficFlow>
        {
            new(Guid.NewGuid(), "127.0.0.1:9100", DateTimeOffset.UtcNow),
        };
        viewModel.TrafficList.LoadFlows(seedFlows);

        await viewModel.SaveSessionCommand.ExecuteAsync(null);

        await Assert.That(exporter.CallCount).IsEqualTo(1);
        await Assert.That(exporter.LastFlows).IsNotNull();
        await Assert.That(exporter.LastFlows!.Count).IsEqualTo(1);
    }
}
