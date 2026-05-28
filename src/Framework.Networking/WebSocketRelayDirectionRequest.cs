using System.IO;
using System.Threading;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object for <see cref="WebSocketRelayDirection.RelayAsync" /> describing one
///     direction of a WebSocket tunnel (source stream, destination stream, the relay that
///     consumes the source bytes, and the linked cancellation source used to abort the peer
///     direction when one side terminates).
/// </summary>
public sealed class WebSocketRelayDirectionRequest
{
    /// <summary>
    ///     Gets the destination stream where decoded bytes are forwarded.
    /// </summary>
    public required Stream Destination { get; init; }

    /// <summary>
    ///     Gets the linked cancellation source that aborts the paired direction when this
    ///     direction terminates.
    /// </summary>
    public required CancellationTokenSource LinkedSource { get; init; }

    /// <summary>
    ///     Gets the relay that parses frames out of the source stream and writes them to the
    ///     destination stream.
    /// </summary>
    public required WebSocketRelay Relay { get; init; }

    /// <summary>
    ///     Gets the source stream from which WebSocket frames are read.
    /// </summary>
    public required Stream Source { get; init; }
}
