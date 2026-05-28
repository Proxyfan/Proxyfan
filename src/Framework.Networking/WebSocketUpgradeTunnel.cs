using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bidirectional WebSocket frame relay. Tees both directions through a
///     <see cref="WebSocketRelay" /> so every fully-assembled <see cref="WebSocketMessage" />
///     is recorded against a <see cref="WebSocketFlow" /> while the raw bytes pass through
///     unmodified.
/// </summary>
public sealed class WebSocketUpgradeTunnel
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new <see cref="WebSocketUpgradeTunnel" />.
    /// </summary>
    /// <param name="timeProvider">Time source used for message and close timestamps.</param>
    public WebSocketUpgradeTunnel(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Pumps WebSocket frames between client and upstream concurrently in both directions
    ///     until either side closes the connection or a close frame is observed. Captured
    ///     messages are appended to <paramref name="webSocketFlow" />.
    /// </summary>
    /// <param name="clientStream">The client-side transport.</param>
    /// <param name="upstreamStream">The upstream server transport.</param>
    /// <param name="webSocketFlow">The flow that receives captured messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when both directions have terminated.</returns>
    public async Task TunnelAsync(
        Stream clientStream,
        Stream upstreamStream,
        WebSocketFlow webSocketFlow,
        CancellationToken cancellationToken)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = linkedSource.Token;

        var clientToServer = new WebSocketRelay(
            WebSocketDirection.Outbound,
            webSocketFlow.RecordMessage,
            _timeProvider);

        var serverToClient = new WebSocketRelay(
            WebSocketDirection.Inbound,
            webSocketFlow.RecordMessage,
            _timeProvider);

        var clientToServerRequest = new WebSocketRelayDirectionRequest
        {
            Destination = upstreamStream,
            LinkedSource = linkedSource,
            Relay = clientToServer,
            Source = clientStream,
        };
        var serverToClientRequest = new WebSocketRelayDirectionRequest
        {
            Destination = clientStream,
            LinkedSource = linkedSource,
            Relay = serverToClient,
            Source = upstreamStream,
        };

        var forwardTask = WebSocketRelayDirection.RelayAsync(clientToServerRequest, linkedToken);
        var backwardTask = WebSocketRelayDirection.RelayAsync(serverToClientRequest, linkedToken);

        try
        {
            await Task.WhenAll(forwardTask, backwardTask).ConfigureAwait(false);
        }
        finally
        {
            webSocketFlow.MarkClosed(_timeProvider.GetUtcNow());
        }
    }
}
