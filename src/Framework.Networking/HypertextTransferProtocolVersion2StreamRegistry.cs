using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Concurrent registry of all live HTTP/2 streams owned by a single connection. Also owns
///     the connection-level send and receive flow-control windows and tracks the highest
///     stream identifier seen so far (used for GOAWAY and the "stream id must monotonically
///     increase" rule from RFC 7540 § 5.1.1).
/// </summary>
public sealed class HypertextTransferProtocolVersion2StreamRegistry
{
    private readonly ConcurrentDictionary<uint, HypertextTransferProtocolVersion2Stream> _streams;
    private int _initialReceiveWindowSize;
    private int _initialSendWindowSize;

    /// <summary>
    ///     Gets the connection-level receive flow-control window — DATA bytes on any stream
    ///     consume it; the proxy emits stream-id-0 WINDOW_UPDATE frames to replenish it.
    /// </summary>
    public HypertextTransferProtocolVersion2FlowControlWindow ConnectionReceiveWindow { get; }

    /// <summary>
    ///     Gets the connection-level send flow-control window — DATA bytes the proxy transmits
    ///     consume it; received stream-id-0 WINDOW_UPDATE frames replenish it.
    /// </summary>
    public HypertextTransferProtocolVersion2FlowControlWindow ConnectionSendWindow { get; }

    /// <summary>
    ///     Gets the number of streams currently registered (in any state).
    /// </summary>
    public int Count => _streams.Count;

    /// <summary>
    ///     Gets the highest stream identifier that has ever been opened on this connection.
    ///     Stream identifiers must monotonically increase per RFC 7540 § 5.1.1.
    /// </summary>
    public uint HighestStreamIdentifier { get; private set; }

    /// <summary>
    ///     Initializes a new registry with default-sized connection-level windows.
    /// </summary>
    public HypertextTransferProtocolVersion2StreamRegistry()
    {
        var streams = new ConcurrentDictionary<uint, HypertextTransferProtocolVersion2Stream>();
        _streams = streams;
        var receiveWindow = new HypertextTransferProtocolVersion2FlowControlWindow();
        ConnectionReceiveWindow = receiveWindow;
        var sendWindow = new HypertextTransferProtocolVersion2FlowControlWindow();
        ConnectionSendWindow = sendWindow;
        _initialReceiveWindowSize = HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize;
        _initialSendWindowSize = HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize;
    }

    /// <summary>
    ///     Updates the local SETTINGS_INITIAL_WINDOW_SIZE value used for new streams' receive
    ///     windows. Existing streams have their receive window shifted by the delta.
    /// </summary>
    /// <param name="newInitialSize">The new local initial receive window size.</param>
    public void ApplyLocalInitialReceiveWindowSize(int newInitialSize)
    {
        var delta = newInitialSize - _initialReceiveWindowSize;
        _initialReceiveWindowSize = newInitialSize;
        if (delta == 0)
        {
            return;
        }
        foreach (var pair in _streams)
        {
            pair.Value.ReceiveWindow.ApplyInitialSizeDelta(delta);
        }
    }

    /// <summary>
    ///     Applies a peer SETTINGS_INITIAL_WINDOW_SIZE update. The new value is recorded so
    ///     future streams use it for their send window; existing streams have their
    ///     <see cref="HypertextTransferProtocolVersion2Stream.SendWindow" /> shifted by the
    ///     delta per RFC 7540 § 6.9.2.
    /// </summary>
    /// <param name="newInitialSize">The new initial window size from the peer.</param>
    public void ApplyPeerInitialSendWindowSize(int newInitialSize)
    {
        var delta = newInitialSize - _initialSendWindowSize;
        _initialSendWindowSize = newInitialSize;
        if (delta == 0)
        {
            return;
        }
        foreach (var pair in _streams)
        {
            pair.Value.SendWindow.ApplyInitialSizeDelta(delta);
        }
    }

    /// <summary>
    ///     Tries to look up an existing stream without creating one.
    /// </summary>
    /// <param name="streamIdentifier">The stream identifier.</param>
    /// <returns>The stream if it exists, otherwise <c>null</c>.</returns>
    public HypertextTransferProtocolVersion2Stream? Find(uint streamIdentifier)
    {
        if (_streams.TryGetValue(streamIdentifier, out var stream))
        {
            return stream;
        }
        return null;
    }

    /// <summary>
    ///     Returns the stream with the given identifier or creates it lazily if absent. When
    ///     creating, the new stream is initialized with the current
    ///     <c>SETTINGS_INITIAL_WINDOW_SIZE</c>-derived window sizes for receive and send sides.
    /// </summary>
    /// <param name="streamIdentifier">The 31-bit stream identifier.</param>
    /// <returns>The existing or newly created stream.</returns>
    public HypertextTransferProtocolVersion2Stream GetOrCreate(uint streamIdentifier)
    {
        var stream = _streams.GetOrAdd(streamIdentifier, identifier =>
        {
            var freshStream = new HypertextTransferProtocolVersion2Stream(identifier, _initialReceiveWindowSize, _initialSendWindowSize);
            return freshStream;
        });
        if (streamIdentifier > HighestStreamIdentifier)
        {
            HighestStreamIdentifier = streamIdentifier;
        }
        return stream;
    }

    /// <summary>
    ///     Removes the stream with the given identifier. This is typically done after the
    ///     stream has entered <c>Closed</c> and all bookkeeping (e.g. publishing the TrafficFlow)
    ///     has completed.
    /// </summary>
    /// <param name="streamIdentifier">The stream identifier.</param>
    /// <returns><c>true</c> when a stream was removed; <c>false</c> when none existed.</returns>
    public bool HasRemoved(uint streamIdentifier)
    {
        return _streams.TryRemove(streamIdentifier, out _);
    }

    /// <summary>
    ///     Returns a snapshot of all currently-registered streams. The snapshot reflects the
    ///     point-in-time view at the start of iteration and is safe to enumerate concurrently
    ///     with further mutations.
    /// </summary>
    /// <returns>A list of all currently-registered streams.</returns>
    public IReadOnlyList<HypertextTransferProtocolVersion2Stream> Snapshot()
    {
        var snapshot = new List<HypertextTransferProtocolVersion2Stream>(_streams.Count);
        foreach (var pair in _streams)
        {
            snapshot.Add(pair.Value);
        }
        return snapshot;
    }
}
