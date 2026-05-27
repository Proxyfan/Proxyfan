using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2StreamRegistry" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2StreamRegistryTests
{
    /// <summary>
    ///     A new registry exposes empty streams and default-sized connection-level windows.
    /// </summary>
    [Test]
    public async Task Constructor_NewRegistry_HasEmptyState()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();

        await Assert.That(registry.Count).IsEqualTo(0);
        await Assert.That(registry.HighestStreamIdentifier).IsEqualTo((uint)0);
        await Assert.That(registry.ConnectionReceiveWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize);
        await Assert.That(registry.ConnectionSendWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2StreamRegistry.GetOrCreate" /> returns the
    ///     same instance on repeated lookups.
    /// </summary>
    [Test]
    public async Task GetOrCreate_SameIdentifierTwice_ReturnsSameInstance()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();

        var first = registry.GetOrCreate(3);
        var second = registry.GetOrCreate(3);

        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(registry.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     The highest seen stream identifier monotonically tracks the largest id ever requested.
    /// </summary>
    [Test]
    public async Task GetOrCreate_ManyIdentifiers_TracksHighest()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();

        registry.GetOrCreate(1);
        registry.GetOrCreate(5);
        registry.GetOrCreate(3);

        await Assert.That(registry.HighestStreamIdentifier).IsEqualTo((uint)5);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2StreamRegistry.Find" /> returns null when
    ///     the stream does not exist.
    /// </summary>
    [Test]
    public async Task Find_UnknownIdentifier_ReturnsNull()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();

        var stream = registry.Find(7);

        await Assert.That(stream).IsNull();
    }

    /// <summary>
    ///     Remove deletes streams from the registry.
    /// </summary>
    [Test]
    public async Task HasRemoved_ExistingStream_ReturnsTrueAndRemoves()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        registry.GetOrCreate(1);

        var removed = registry.HasRemoved(1);

        await Assert.That(removed).IsTrue();
        await Assert.That(registry.Find(1)).IsNull();
    }

    /// <summary>
    ///     A peer SETTINGS_INITIAL_WINDOW_SIZE update shifts every existing stream's send window
    ///     by the delta.
    /// </summary>
    [Test]
    public async Task ApplyPeerInitialSendWindowSize_PositiveDelta_ShiftsExistingStreams()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var stream = registry.GetOrCreate(1);

        registry.ApplyPeerInitialSendWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize + 1000);

        await Assert.That(stream.SendWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize + 1000);
    }

    /// <summary>
    ///     A local SETTINGS_INITIAL_WINDOW_SIZE update shifts every existing stream's receive window.
    /// </summary>
    [Test]
    public async Task ApplyLocalInitialReceiveWindowSize_NegativeDelta_ShiftsExistingStreams()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var stream = registry.GetOrCreate(1);

        registry.ApplyLocalInitialReceiveWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize - 5000);

        await Assert.That(stream.ReceiveWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize - 5000);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2StreamRegistry.Snapshot" /> returns the
    ///     current set of streams.
    /// </summary>
    [Test]
    public async Task Snapshot_TwoStreams_ContainsBoth()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        registry.GetOrCreate(1);
        registry.GetOrCreate(3);

        var snapshot = registry.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(2);
    }
}
