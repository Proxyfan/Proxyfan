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
    public async Task HasAppliedPeerInitialSendWindowSize_PositiveDelta_ShiftsExistingStreams()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var stream = registry.GetOrCreate(1);

        var result = registry.HasAppliedPeerInitialSendWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize + 1000);

        await Assert.That(result).IsTrue();
        await Assert.That(stream.SendWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize + 1000);
    }

    /// <summary>
    ///     A local SETTINGS_INITIAL_WINDOW_SIZE update shifts every existing stream's receive window.
    /// </summary>
    [Test]
    public async Task HasAppliedLocalInitialReceiveWindowSize_NegativeDelta_ShiftsExistingStreams()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var stream = registry.GetOrCreate(1);

        var result = registry.HasAppliedLocalInitialReceiveWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize - 5000);

        await Assert.That(result).IsTrue();
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

    /// <summary>
    ///     Looking up a previously-created stream returns the existing instance, exercising the
    ///     TryGetValue success branch in <see cref="HypertextTransferProtocolVersion2StreamRegistry.Find" />.
    /// </summary>
    [Test]
    public async Task Find_ExistingStream_ReturnsSameInstance()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var created = registry.GetOrCreate(11);

        var found = registry.Find(11);

        await Assert.That(found).IsSameReferenceAs(created);
    }

    /// <summary>
    ///     Applying the same local initial receive window size is a no-op (delta == 0); existing
    ///     streams are not shifted. Exercises the early-return branch in
    ///     <see cref="HypertextTransferProtocolVersion2StreamRegistry.HasAppliedLocalInitialReceiveWindowSize" />.
    /// </summary>
    [Test]
    public async Task HasAppliedLocalInitialReceiveWindowSize_ZeroDelta_DoesNotShiftStreams()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var stream = registry.GetOrCreate(1);
        var originalAvailable = stream.ReceiveWindow.Available;

        var result = registry.HasAppliedLocalInitialReceiveWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize);

        await Assert.That(result).IsTrue();
        await Assert.That(stream.ReceiveWindow.Available).IsEqualTo(originalAvailable);
    }

    /// <summary>
    ///     Applying the same peer initial send window size is a no-op (delta == 0); existing
    ///     streams are not shifted. Exercises the early-return branch in
    ///     <see cref="HypertextTransferProtocolVersion2StreamRegistry.HasAppliedPeerInitialSendWindowSize" />.
    /// </summary>
    [Test]
    public async Task HasAppliedPeerInitialSendWindowSize_ZeroDelta_DoesNotShiftStreams()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        var stream = registry.GetOrCreate(1);
        var originalAvailable = stream.SendWindow.Available;

        var result = registry.HasAppliedPeerInitialSendWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize);

        await Assert.That(result).IsTrue();
        await Assert.That(stream.SendWindow.Available).IsEqualTo(originalAvailable);
    }

    /// <summary>
    ///     A peer SETTINGS_INITIAL_WINDOW_SIZE update that would push an existing stream's send
    ///     window above the maximum returns false so the caller can raise a FLOW_CONTROL_ERROR
    ///     connection error per RFC 7540 § 6.9.2.
    /// </summary>
    [Test]
    public async Task HasAppliedPeerInitialSendWindowSize_OverflowsExistingStream_ReturnsFalse()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        _ = registry.HasAppliedPeerInitialSendWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize - 1);
        var stream = registry.GetOrCreate(1);
        stream.SendWindow.HasIncremented(1);

        var result = registry.HasAppliedPeerInitialSendWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize);

        await Assert.That(result).IsFalse();
        await Assert.That(stream.SendWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize);
    }

    /// <summary>
    ///     A local SETTINGS_INITIAL_WINDOW_SIZE update that would push an existing stream's
    ///     receive window above the maximum returns false so the caller can raise a
    ///     FLOW_CONTROL_ERROR connection error per RFC 7540 § 6.9.2.
    /// </summary>
    [Test]
    public async Task HasAppliedLocalInitialReceiveWindowSize_OverflowsExistingStream_ReturnsFalse()
    {
        var registry = new HypertextTransferProtocolVersion2StreamRegistry();
        _ = registry.HasAppliedLocalInitialReceiveWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize - 1);
        var stream = registry.GetOrCreate(1);
        stream.ReceiveWindow.HasIncremented(1);

        var result = registry.HasAppliedLocalInitialReceiveWindowSize(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize);

        await Assert.That(result).IsFalse();
        await Assert.That(stream.ReceiveWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.MaximumSize);
    }
}
