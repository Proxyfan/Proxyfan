using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Proxy.Events;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads the first bytes of an accepted connection to detect the protocol and dispatches
///     to the first registered <see cref="IConnectionHandler" /> that accepts those bytes.
///     Use <see cref="DispatchAsync" /> as the <c>onConnectionAccepted</c> callback passed to
///     <see cref="IProxyListener.StartAsync" />. The dispatcher peeks at up to
///     <see cref="PeekByteCount" /> bytes without consuming them, so the handler receives the
///     full original byte stream from the start.
///     Handler exceptions (excluding <see cref="OperationCanceledException" />) are caught,
///     logged, and reported via a <see cref="ConnectionErrorOccurred" /> domain event.
///     The exception is then swallowed so that one failing connection cannot affect others.
/// </summary>
public sealed partial class ConnectionDispatcher : IConnectionDispatcher
{
    /// <summary>
    ///     The number of bytes read from the connection for protocol detection.
    ///     Eight bytes is sufficient to identify all supported protocol signatures.
    /// </summary>
    public const int PeekByteCount = 8;
    private readonly IDomainEventBus _eventBus;
    private readonly IEnumerable<IConnectionHandler> _handlers;
    private readonly ILogger<ConnectionDispatcher> _logger;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;

    /// <summary>
    ///     Initializes a new instance of <see cref="ConnectionDispatcher" />.
    /// </summary>
    /// <param name="handlers">
    ///     The ordered collection of connection handlers to try for each accepted connection.
    /// </param>
    /// <param name="optionsMonitor">
    ///     Live monitor for <see cref="ProxyOptions" /> used to read protocol detection timeout.
    /// </param>
    /// <param name="eventBus">
    ///     Domain event bus for publishing <see cref="ConnectionErrorOccurred" /> events.
    /// </param>
    /// <param name="logger">
    ///     Logger for structured diagnostic output.
    /// </param>
    public ConnectionDispatcher(
        IEnumerable<IConnectionHandler> handlers,
        IOptionsMonitor<ProxyOptions> optionsMonitor,
        IDomainEventBus eventBus,
        ILogger<ConnectionDispatcher> logger)
    {
        _handlers = handlers;
        _optionsMonitor = optionsMonitor;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    ///     Detects the protocol of <paramref name="connection" /> from its first bytes and
    ///     dispatches to the appropriate registered handler.
    /// </summary>
    /// <param name="connection">
    ///     The accepted connection to inspect and dispatch.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token that, when cancelled, stops the dispatch.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> that completes when the connection has been fully handled.
    /// </returns>
    public async Task DispatchAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        var timeout = _optionsMonitor.CurrentValue.ProtocolDetectionTimeout;

        using var detectionCancellationSource = timeout > TimeSpan.Zero
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        detectionCancellationSource?.CancelAfter(timeout);

        var detectionToken = detectionCancellationSource?.Token ?? cancellationToken;
        var reader = connection.Transport.Input;
        ReadResult result;

        try
        {
            result = await PipeReaderHelper.ReadUntilAsync(reader, PeekByteCount, detectionToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogDetectionTimeout(connection.RemoteEndPoint);
            return;
        }

        var buffer = result.Buffer;

        if (buffer.IsEmpty)
        {
            LogConnectionClosedEarly(connection.RemoteEndPoint);
            reader.AdvanceTo(buffer.End);
            return;
        }

        await DispatchToPeekHandlerAsync(connection, reader, buffer, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchToPeekHandlerAsync(
        IProxyConnection connection,
        PipeReader reader,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
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

            reader.AdvanceTo(buffer.Start, buffer.Start);

            try
            {
                await handler.HandleAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogHandlerError(ex, connection.RemoteEndPoint);
                var errorEvent = new ConnectionErrorOccurred(connection.RemoteEndPoint, ex, DateTimeOffset.UtcNow);
                _eventBus.Publish(errorEvent);
            }

            return;
        }

        reader.AdvanceTo(buffer.End);
        LogUnknownProtocol(connection.RemoteEndPoint);
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Connection from {RemoteEndPoint} closed before protocol could be detected")]
    private partial void LogConnectionClosedEarly(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Protocol detection timed out for {RemoteEndPoint}; closing connection")]
    private partial void LogDetectionTimeout(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unhandled error in connection handler for {RemoteEndPoint}")]
    private partial void LogHandlerError(Exception ex, EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Unknown protocol from {RemoteEndPoint}; closing connection")]
    private partial void LogUnknownProtocol(EndPoint remoteEndPoint);
}