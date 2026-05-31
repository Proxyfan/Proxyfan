using Proxyfan.Client.EndToEndTests.Fixtures;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering session save/open commands described in
///     <c>docs/DESIGN.md § 6.16 Session Management</c> and § 6.17 Export and Import.
///     Verifies that the file-picker abstraction is correctly invoked, that the
///     HAR exporter receives the right flow snapshot, and that a cancelled
///     picker is a no-op.
/// </summary>
public sealed class ShellViewModelSessionEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task SaveSession_NoWriteStream_PickerCancelled_NoExport()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.FilePicker.WriteStream = null;

            await env.ShellViewModel.SaveSessionCommand.ExecuteAsync(null);

            await Assert.That(env.FilePicker.OpenForWriteCallCount).IsEqualTo(1);
            await Assert.That(env.HarExporter.CallCount).IsEqualTo(0);
        });
    }

    [Test]
    public async Task SaveSession_WithFlows_PassesSnapshotToExporter()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.ShellViewModel.TrafficList.LoadFlows([
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/a"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(2, "https://api.example.com/b"),
            ]);
            using var writeStream = new MemoryStream();
            env.FilePicker.WriteStream = writeStream;

            await env.ShellViewModel.SaveSessionCommand.ExecuteAsync(null);

            await Assert.That(env.HarExporter.CallCount).IsEqualTo(1);
            await Assert.That(env.HarExporter.LastFlows).IsNotNull();
            await Assert.That(env.HarExporter.LastFlows!.Count).IsEqualTo(2);
            await Assert.That(env.HarExporter.LastStream).IsSameReferenceAs(writeStream);
        });
    }

    [Test]
    public async Task SaveSession_EmptyTrafficList_PassesEmptySnapshot()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            using var writeStream = new MemoryStream();
            env.FilePicker.WriteStream = writeStream;

            await env.ShellViewModel.SaveSessionCommand.ExecuteAsync(null);

            await Assert.That(env.HarExporter.CallCount).IsEqualTo(1);
            await Assert.That(env.HarExporter.LastFlows).IsNotNull();
            await Assert.That(env.HarExporter.LastFlows!.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task OpenSession_NoReadStream_PickerCancelled_NoLoad()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.FilePicker.ReadStream = null;
            env.HarImporter.ReturnFlows = [
                EndToEndTrafficFlowFactory.CreateCompletedGet(1, "https://api.example.com/x"),
            ];

            await env.ShellViewModel.OpenSessionCommand.ExecuteAsync(null);

            await Assert.That(env.FilePicker.OpenForReadCallCount).IsEqualTo(1);
            await Assert.That(env.HarImporter.CallCount).IsEqualTo(0);
            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(0);
        });
    }

    [Test]
    public async Task OpenSession_WithImportedFlows_LoadsThemIntoTrafficList()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var imported = new TrafficFlow[]
            {
                EndToEndTrafficFlowFactory.CreateCompletedGet(11, "https://api.example.com/p"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(12, "https://api.example.com/q"),
                EndToEndTrafficFlowFactory.CreateCompletedGet(13, "https://api.example.com/r"),
            };
            env.HarImporter.ReturnFlows = imported;
            env.FilePicker.ReadStream = new MemoryStream(Array.Empty<byte>());

            await env.ShellViewModel.OpenSessionCommand.ExecuteAsync(null);

            await Assert.That(env.HarImporter.CallCount).IsEqualTo(1);
            await Assert.That(env.ShellViewModel.TrafficList.Flows.Count).IsEqualTo(3);
        });
    }
}
