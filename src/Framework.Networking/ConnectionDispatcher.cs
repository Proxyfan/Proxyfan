using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Proxy.Events;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads the first bytes of an accepted connection to detect the protocol and dispatches
///     to the first registered <see cref="IConnectionHandler" /> that accepts those bytes.
/// </summary>
/// <remarks>
///     Use <see cref="DispatchAsync" /> as the <c>onConnectionAccepted</c> callback passed to
///     <see cref="IProxyListener.StartAsync" />. The dispatcher peeks at up to
///     <see cref="PeekByteCount" /> bytes without consuming them, so the handler receives the
///     full original byte stream from the start.
///     <para>
///         Handler exceptions (excluding <see cref="OperationCanceledException" />) are caught,
///         logged, and reported via a <see cref="ConnectionErrorOccurred" /> domain event.
///         The exception is then swallowed so that one failing connection cannot affect others.
///     </para>
/// </remarks>
public sealed partial class ConnectionDispatcher(
    IEnumerable<IConnectionHandler> handlers,
    IOptionsMonitor<ProxyOptions> optionsMonitor,
    IDomainEventBus eventBus,
    ILogger<ConnectionDispatcher> logger) : IConnectionDispatcher
{
    /// <summary>
    ///     The number of bytes read from the connection for protocol detection.
    ///     Eight bytes is sufficient to identify all supported protocol signatures.
    /// </summary>
    public const int PeekByteCount = 8;

    private readonly IEnumerable<IConnectionHandler> _handlers = handlers;
    private readonly IDomainEventBus _eventBus = eventBus;
    private readonly ILogger<ConnectionDispatcher> _logger = logger;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor = optionsMonitor;

    /// <summary>
    ///     Detects the protocol of <paramref name="connection" /> from its first bytes and
    ///     dispatches to the appropriate registered handler.
    /// </summary>
    /// <param name="connection">The accepted connection to inspect and dispatch.</param>
    /// <param name="cancellationToken">A token that, when cancelled, stops the dispatch.</param>
    /// <returns>A <see cref="Task" /> that completes when the connection has been fully handled.</returns>
    public async Task DispatchAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        var timeout = _optionsMonitor.CurrentValue.ProtocolDetectionTimeout;

        using var detectionCts = timeout > TimeSpan.Zero
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        detectionCts?.CancelAfter(timeout);

        var detectionToken = detectionCts?.Token ?? cancellationToken;
        var reader = connection.Transport.Input;
        ReadResult result;

        try
        {
            result = await ReadUntilAsync(reader, PeekByteCount, detectionToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogDetectionTimeout(connection.RemoteEndPoint);
            return;
        }

        var buffer = result.Buffer;

        if (buffer.IsEmpty)
        {
            // Connection closed before any data arrived.
            LogConnectionClosedEarly(connection.RemoteEndPoint);
            reader.AdvanceTo(buffer.End);
            return;
        }

        ReadOnlySequence<byte> peekSlice;

        if (buffer.Length > PeekByteCount)
        {
            peekSlice = buffer.Slice(0, PeekByteCount);
        }
        else
        {
            peekSlice = buffer;
        }

        foreach (var handler in _handlers)
        {
            if (!handler.CanHandle(peekSlice))
            {
                continue;
            }

            // Restore all peeked bytes to the pipe so the handler reads from the very beginning.
            // AdvanceTo(Start, Start) signals that no bytes were consumed AND no bytes were examined,
            // causing the next ReadAsync on this reader (by the handler) to return immediately with
            // the full buffered content.
            reader.AdvanceTo(buffer.Start, buffer.Start);

            try
            {
                await handler.HandleAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogHandlerError(ex, connection.RemoteEndPoint);
                _eventBus.Publish(new ConnectionErrorOccurred(connection.RemoteEndPoint, ex, DateTimeOffset.UtcNow));
            }

            return;
        }

        // No handler accepted the protocol — consume all peeked bytes and close.
        reader.AdvanceTo(buffer.End);
        LogUnknownProtocol(connection.RemoteEndPoint);
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Protocol detection timed out for {RemoteEndPoint}; closing connection")]
    private partial void LogDetectionTimeout(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Unknown protocol from {RemoteEndPoint}; closing connection")]
    private partial void LogUnknownProtocol(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Connection from {RemoteEndPoint} closed before protocol could be detected")]
    private partial void LogConnectionClosedEarly(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unhandled error in connection handler for {RemoteEndPoint}")]
    private partial void LogHandlerError(Exception ex, EndPoint remoteEndPoint);

    private static async Task<ReadResult> ReadUntilAsync(PipeReader reader, int minimumBytes, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (result.Buffer.Length >= minimumBytes || result.IsCompleted || result.IsCanceled)
            {
                return result;
            }

            // Not enough bytes yet — mark all available bytes as examined (but none as consumed)
            // so the pipe waits for more data before returning on the next iteration.
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }
}

