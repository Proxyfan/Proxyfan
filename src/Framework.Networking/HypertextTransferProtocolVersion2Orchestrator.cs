using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bidirectional HTTP/2 traffic relay used by the TLS interceptor when both endpoints
///     negotiate the <c>h2</c> ALPN protocol. The orchestrator forwards every frame between
///     the decrypted client SSL stream and the decrypted upstream SSL stream verbatim while
///     parsing HEADERS, CONTINUATION, DATA, and RST_STREAM frames in parallel so each
///     logical request/response pair lands in the inspector as a regular
///     <see cref="TrafficFlow" />.
///     <para>
///         The HPACK encoding on the wire is preserved end-to-end (frames are not re-encoded)
///         which means flow-control, SETTINGS, PING, PRIORITY, and other connection-management
///         frames pass through untouched. Two shadow
///         <see cref="HypertextTransferProtocolVersion2HpackDecoder" /> instances — one per
///         direction — track the dynamic-table state so the captured header lists remain
///         correct.
///     </para>
///     <para>
///         Rules and scripting do NOT run on HTTP/2 traffic in this release: rewriting frames
///         in-flight would require re-encoding HPACK against the peer's dynamic-table state,
///         which has the potential to break the stream. Inspection, capture, and HAR export
///         all work end-to-end. This matches the design tradeoff used by every released
///         version of Charles, Fiddler Classic, and mitmproxy where HTTP/2 capture is a
///         first-class feature but mid-flight modification is not.
///     </para>
/// </summary>
public sealed class HypertextTransferProtocolVersion2Orchestrator
{
    private readonly ConcurrentDictionary<uint, HypertextTransferProtocolVersion2CaptureState> _captures;
    private readonly HypertextTransferProtocolVersion2HeaderBlockAssembler _clientHeaderAssembler;
    private readonly HypertextTransferProtocolVersion2HpackDecoder _clientHeaderDecoder;
    private readonly ConcurrentDictionary<uint, bool> _clientPendingEndStream;
    private readonly HypertextTransferProtocolVersion2OrchestratorDependencies _dependencies;
    private readonly ConcurrentDictionary<uint, TrafficFlow> _flows;
    private readonly ConcurrentDictionary<uint, HypertextTransferProtocolVersion2RemoteProcedureCallCapture> _remoteProcedureCalls;
    private readonly TimeProvider _timeProvider;
    private readonly HypertextTransferProtocolVersion2HeaderBlockAssembler _upstreamHeaderAssembler;
    private readonly HypertextTransferProtocolVersion2HpackDecoder _upstreamHeaderDecoder;
    private readonly ConcurrentDictionary<uint, bool> _upstreamPendingEndStream;

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolVersion2Orchestrator" />.
    /// </summary>
    /// <param name="dependencies">The bundled orchestrator dependencies.</param>
    public HypertextTransferProtocolVersion2Orchestrator(HypertextTransferProtocolVersion2OrchestratorDependencies dependencies)
    {
        _dependencies = dependencies;
        _timeProvider = dependencies.TimeProvider ?? TimeProvider.System;
        var captures = new ConcurrentDictionary<uint, HypertextTransferProtocolVersion2CaptureState>();
        _captures = captures;
        var flows = new ConcurrentDictionary<uint, TrafficFlow>();
        _flows = flows;
        var clientDecoder = new HypertextTransferProtocolVersion2HpackDecoder();
        _clientHeaderDecoder = clientDecoder;
        var upstreamDecoder = new HypertextTransferProtocolVersion2HpackDecoder();
        _upstreamHeaderDecoder = upstreamDecoder;
        var clientAssembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();
        _clientHeaderAssembler = clientAssembler;
        var upstreamAssembler = new HypertextTransferProtocolVersion2HeaderBlockAssembler();
        _upstreamHeaderAssembler = upstreamAssembler;
        var clientPending = new ConcurrentDictionary<uint, bool>();
        _clientPendingEndStream = clientPending;
        var upstreamPending = new ConcurrentDictionary<uint, bool>();
        _upstreamPendingEndStream = upstreamPending;
        var remoteProcedureCalls = new ConcurrentDictionary<uint, HypertextTransferProtocolVersion2RemoteProcedureCallCapture>();
        _remoteProcedureCalls = remoteProcedureCalls;
    }

    /// <summary>
    ///     Runs the orchestrator until both directions close or
    ///     <paramref name="cancellationToken" /> fires. The two streams must be
    ///     already-authenticated decrypted bidirectional streams (typically
    ///     <see cref="System.Net.Security.SslStream" /> instances produced by the TLS
    ///     interceptor).
    /// </summary>
    /// <param name="clientStream">The decrypted client-facing stream.</param>
    /// <param name="upstreamStream">The decrypted upstream-facing stream.</param>
    /// <param name="clientEndPointDescription">
    ///     Description of the client endpoint used for created traffic flows.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the orchestration.</param>
    /// <returns>A task that completes when both directions finish.</returns>
    public async Task RunAsync(
        Stream clientStream,
        Stream upstreamStream,
        string clientEndPointDescription,
        CancellationToken cancellationToken)
    {
        var pipeOptions = new StreamPipeReaderOptions(leaveOpen: true);
        var clientReader = PipeReader.Create(clientStream, pipeOptions);
        var upstreamReader = PipeReader.Create(upstreamStream, pipeOptions);

        using var pumpCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var clientPumpContext = new HypertextTransferProtocolVersion2OrchestratorPumpContext
        {
            ClientEndPointDescription = clientEndPointDescription,
            Direction = HypertextTransferProtocolVersion2RelayDirection.ClientToUpstream,
            Reader = clientReader,
            WriteStream = upstreamStream,
        };
        var upstreamPumpContext = new HypertextTransferProtocolVersion2OrchestratorPumpContext
        {
            ClientEndPointDescription = clientEndPointDescription,
            Direction = HypertextTransferProtocolVersion2RelayDirection.UpstreamToClient,
            Reader = upstreamReader,
            WriteStream = clientStream,
        };

        var clientToUpstream = PumpAsync(clientPumpContext, pumpCancellation, cancellationToken);
        var upstreamToClient = PumpAsync(upstreamPumpContext, pumpCancellation, cancellationToken);

        await Task.WhenAll(clientToUpstream, upstreamToClient).ConfigureAwait(false);
        FailUnfinishedFlows();
    }

    private void ApplyAssembledHeaders(
        uint streamIdentifier,
        byte[] assembled,
        bool isEndStream,
        HypertextTransferProtocolVersion2OrchestratorPumpContext context)
    {
        var decoder = SelectDecoder(context.Direction);
        try
        {
            var headers = decoder.Decode(assembled);
            var capture = GetOrCreateCaptureState(streamIdentifier);
            if (context.Direction == HypertextTransferProtocolVersion2RelayDirection.ClientToUpstream)
            {
                capture.AppendRequestHeaders(headers, isEndStream);
                MaybePublishRequest(streamIdentifier, capture, context.ClientEndPointDescription);
            }
            else
            {
                capture.AppendResponseHeaders(headers, isEndStream);
                MaybePublishResponse(streamIdentifier, capture);
            }

            MaybeCompleteFlow(streamIdentifier, capture);
        }
        catch (FormatException ex)
        {
            _ = ex;
        }
    }

    private void FailUnfinishedFlows()
    {
        foreach (var pair in _flows)
        {
            var flow = pair.Value;
            flow.Fail();
            _dependencies.FlowEventPublisher.PublishFlowCompleted(flow);
        }

        var remoteProcedureCallCloseTimestamp = _timeProvider.GetUtcNow();
        foreach (var pair in _remoteProcedureCalls)
        {
            pair.Value.Flow.MarkClosed(remoteProcedureCallCloseTimestamp);
        }

        _flows.Clear();
        _captures.Clear();
        _clientPendingEndStream.Clear();
        _upstreamPendingEndStream.Clear();
        _remoteProcedureCalls.Clear();
    }

    private HypertextTransferProtocolVersion2CaptureState GetOrCreateCaptureState(uint streamIdentifier)
    {
        var capture = _captures.GetOrAdd(streamIdentifier, HypertextTransferProtocolVersion2OrchestratorHelpers.CreateCaptureState);
        return capture;
    }

    private void MaybeAttachRemoteProcedureCallCapture(
        uint streamIdentifier,
        HypertextTransferProtocolVersion2CaptureState capture,
        TrafficFlow flow,
        HypertextTransferProtocolResponseData response)
    {
        var store = _dependencies.RemoteProcedureCallStore;
        if (store is null)
        {
            return;
        }

        if (!RemoteProcedureCallResponseDetector.HasRemoteProcedureCallResponse(response.Headers))
        {
            return;
        }

        if (_remoteProcedureCalls.ContainsKey(streamIdentifier))
        {
            return;
        }

        var remoteProcedureCallFlow = new RemoteProcedureCallFlow(flow);
        store.Add(remoteProcedureCallFlow);
        var remoteProcedureCallCapture = new HypertextTransferProtocolVersion2RemoteProcedureCallCapture(remoteProcedureCallFlow, _timeProvider);
        _remoteProcedureCalls[streamIdentifier] = remoteProcedureCallCapture;

        if (capture.RequestBody.Length > 0)
        {
            remoteProcedureCallCapture.AppendClientBytes(capture.RequestBody.Span);
        }
    }

    private void MaybeCompleteFlow(uint streamIdentifier, HypertextTransferProtocolVersion2CaptureState capture)
    {
        if (!capture.IsRequestEnded || !capture.IsResponseEnded)
        {
            return;
        }

        if (!_flows.TryRemove(streamIdentifier, out var flow))
        {
            _captures.TryRemove(streamIdentifier, out _);
            return;
        }

        if (flow.Response is null)
        {
            var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(capture.ResponseHeaders, capture.ResponseBody);
            if (response is not null)
            {
                flow.SetResponse(response);
                _dependencies.FlowEventPublisher.PublishResponseReceived(flow, response);
            }
        }
        else
        {
            var responseWithBody = HypertextTransferProtocolVersion2OrchestratorHelpers.ReplaceResponseBody(flow.Response, capture.ResponseBody);
            flow.SetResponse(responseWithBody);
        }

        if (flow.Response is null)
        {
            flow.Fail();
        }
        else
        {
            flow.Complete();
        }

        _dependencies.TrafficStore.Add(flow);
        _dependencies.FlowEventPublisher.PublishFlowCompleted(flow);
        _captures.TryRemove(streamIdentifier, out _);
        if (_remoteProcedureCalls.TryRemove(streamIdentifier, out var completedRemoteProcedureCallCapture))
        {
            completedRemoteProcedureCallCapture.Flow.MarkClosed(_timeProvider.GetUtcNow());
        }
    }

    private void MaybePublishRequest(
        uint streamIdentifier,
        HypertextTransferProtocolVersion2CaptureState capture,
        string clientEndPointDescription)
    {
        if (capture.RequestHeaders.Count == 0)
        {
            return;
        }

        var flow = _flows.GetOrAdd(streamIdentifier, identifier =>
        {
            _ = identifier;
            var fresh = new TrafficFlow(Guid.NewGuid(), clientEndPointDescription, DateTimeOffset.UtcNow);
            _dependencies.FlowEventPublisher.PublishFlowCreated(fresh);
            return fresh;
        });

        if (flow.Request is not null)
        {
            return;
        }

        if (!capture.IsRequestEnded)
        {
            return;
        }

        HypertextTransferProtocolRequestData request;
        try
        {
            request = HypertextTransferProtocolVersion2RequestTranslation.Translate(capture.RequestHeaders, capture.RequestBody);
        }
        catch (FormatException ex)
        {
            _ = ex;
            return;
        }

        flow.SetRequest(request);
        _dependencies.FlowEventPublisher.PublishRequestReceived(flow, request);
    }

    private void MaybePublishResponse(uint streamIdentifier, HypertextTransferProtocolVersion2CaptureState capture)
    {
        if (!_flows.TryGetValue(streamIdentifier, out var flow))
        {
            return;
        }

        if (flow.Response is not null)
        {
            return;
        }

        if (capture.ResponseHeaders.Count == 0)
        {
            return;
        }

        var response = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildResponseFromHeaders(capture.ResponseHeaders, capture.ResponseBody);
        if (response is null)
        {
            return;
        }

        flow.SetResponse(response);
        _dependencies.FlowEventPublisher.PublishResponseReceived(flow, response);
        MaybeAttachRemoteProcedureCallCapture(streamIdentifier, capture, flow, response);
    }

    private void ProcessContinuationFrame(
        HypertextTransferProtocolVersion2Frame frame,
        HypertextTransferProtocolVersion2OrchestratorPumpContext context)
    {
        var isEndHeaders = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.EndHeaders) != 0;
        var assembler = SelectAssembler(context.Direction);
        var assembled = assembler.AppendContinuation(frame.Header.StreamIdentifier, frame.Payload.Span, isEndHeaders);
        if (assembled is null)
        {
            return;
        }

        var pendingMap = SelectPendingEndStreamMap(context.Direction);
        var isEndStream = false;
        if (pendingMap.TryRemove(frame.Header.StreamIdentifier, out var pendingFlag))
        {
            isEndStream = pendingFlag;
        }

        ApplyAssembledHeaders(frame.Header.StreamIdentifier, assembled, isEndStream, context);
    }

    private void ProcessDataFrame(
        HypertextTransferProtocolVersion2Frame frame,
        HypertextTransferProtocolVersion2OrchestratorPumpContext context)
    {
        var isEndStream = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge) != 0;
        var isPadded = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.Padded) != 0;
        var data = HypertextTransferProtocolVersion2DataFramePayloadParser.Parse(frame.Payload, isPadded);
        if (data is null)
        {
            return;
        }

        var capture = GetOrCreateCaptureState(frame.Header.StreamIdentifier);
        if (context.Direction == HypertextTransferProtocolVersion2RelayDirection.ClientToUpstream)
        {
            capture.AppendRequestData(data.Value.Span, isEndStream);
            if (_remoteProcedureCalls.TryGetValue(frame.Header.StreamIdentifier, out var clientRemoteProcedureCallCapture))
            {
                clientRemoteProcedureCallCapture.AppendClientBytes(data.Value.Span);
            }

            MaybePublishRequest(frame.Header.StreamIdentifier, capture, context.ClientEndPointDescription);
        }
        else
        {
            capture.AppendResponseData(data.Value.Span, isEndStream);
            if (_remoteProcedureCalls.TryGetValue(frame.Header.StreamIdentifier, out var upstreamRemoteProcedureCallCapture))
            {
                upstreamRemoteProcedureCallCapture.AppendUpstreamBytes(data.Value.Span);
            }

            MaybePublishResponse(frame.Header.StreamIdentifier, capture);
        }

        MaybeCompleteFlow(frame.Header.StreamIdentifier, capture);
    }

    private void ProcessFrame(
        HypertextTransferProtocolVersion2Frame frame,
        HypertextTransferProtocolVersion2OrchestratorPumpContext context)
    {
        var type = frame.Header.Type;
        var assembler = SelectAssembler(context.Direction);
        if (assembler.IsInProgress)
        {
            var isContiguousContinuation = type == HypertextTransferProtocolVersion2FrameType.Continuation
                && frame.Header.StreamIdentifier == assembler.ActiveStreamIdentifier;
            if (!isContiguousContinuation)
            {
                RejectInterleavedFrame(context);
                return;
            }
        }

        if (type == HypertextTransferProtocolVersion2FrameType.Headers)
        {
            ProcessHeadersFrame(frame, context);
            return;
        }

        if (type == HypertextTransferProtocolVersion2FrameType.Continuation)
        {
            ProcessContinuationFrame(frame, context);
            return;
        }

        if (type == HypertextTransferProtocolVersion2FrameType.Data)
        {
            ProcessDataFrame(frame, context);
            return;
        }

        if (type == HypertextTransferProtocolVersion2FrameType.ResetStream)
        {
            ProcessResetStreamFrame(frame);
        }
    }

    private void ProcessHeadersFrame(
        HypertextTransferProtocolVersion2Frame frame,
        HypertextTransferProtocolVersion2OrchestratorPumpContext context)
    {
        var isEndStream = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge) != 0;
        var isEndHeaders = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.EndHeaders) != 0;
        var isPadded = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.Padded) != 0;
        var hasPriority = (frame.Header.Flags & HypertextTransferProtocolVersion2FrameFlag.Priority) != 0;
        var fragment = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(frame.Payload, isPadded, hasPriority);
        if (fragment is null)
        {
            return;
        }

        var assembler = SelectAssembler(context.Direction);
        var assembled = assembler.BeginBlock(frame.Header.StreamIdentifier, fragment.Value.Span, isEndHeaders);

        if (assembled is null)
        {
            if (isEndStream)
            {
                var pendingMap = SelectPendingEndStreamMap(context.Direction);
                pendingMap[frame.Header.StreamIdentifier] = true;
            }

            return;
        }

        ApplyAssembledHeaders(frame.Header.StreamIdentifier, assembled, isEndStream, context);
    }

    private void ProcessResetStreamFrame(HypertextTransferProtocolVersion2Frame frame)
    {
        if (_flows.TryRemove(frame.Header.StreamIdentifier, out var flow))
        {
            flow.Fail();
            _dependencies.FlowEventPublisher.PublishFlowCompleted(flow);
        }

        _captures.TryRemove(frame.Header.StreamIdentifier, out _);
        _clientPendingEndStream.TryRemove(frame.Header.StreamIdentifier, out _);
        _upstreamPendingEndStream.TryRemove(frame.Header.StreamIdentifier, out _);
        if (_remoteProcedureCalls.TryRemove(frame.Header.StreamIdentifier, out var resetRemoteProcedureCallCapture))
        {
            resetRemoteProcedureCallCapture.Flow.MarkClosed(_timeProvider.GetUtcNow());
        }
    }

    private async Task PumpAsync(
        HypertextTransferProtocolVersion2OrchestratorPumpContext context,
        CancellationTokenSource pumpCancellation,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var token = pumpCancellation.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var frame = await HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(context.Reader, token).ConfigureAwait(false);
                if (frame is null)
                {
                    break;
                }

                ProcessFrame(frame, context);
                var hasWriteSucceeded = await HypertextTransferProtocolVersion2OrchestratorWriter.TryForwardFrameAsync(context.WriteStream, frame, token).ConfigureAwait(false);
                if (!hasWriteSucceeded)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
        catch (IOException ex)
        {
            _ = ex;
        }
        finally
        {
            await context.Reader.CompleteAsync().ConfigureAwait(false);
            await pumpCancellation.CancelAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Discards the in-progress header block state (assembler buffer + pending END_STREAM
    ///     flag for the active stream) when the peer interleaves a non-CONTINUATION frame, or
    ///     a CONTINUATION for a different stream, between a HEADERS fragment and its terminating
    ///     END_HEADERS CONTINUATION. RFC 7540 § 6.10 forbids interleaving and treats this as a
    ///     connection-level PROTOCOL_ERROR. The orchestrator continues to forward the wire bytes
    ///     verbatim — the peers will surface the protocol error themselves — but it MUST stop
    ///     attempting to decode further HPACK fragments for the desynchronised block.
    /// </summary>
    private void RejectInterleavedFrame(HypertextTransferProtocolVersion2OrchestratorPumpContext context)
    {
        var assembler = SelectAssembler(context.Direction);
        var activeStreamIdentifier = assembler.ActiveStreamIdentifier;
        assembler.Reset();
        if (activeStreamIdentifier == 0)
        {
            return;
        }

        var pendingMap = SelectPendingEndStreamMap(context.Direction);
        pendingMap.TryRemove(activeStreamIdentifier, out _);
    }

    private HypertextTransferProtocolVersion2HeaderBlockAssembler SelectAssembler(HypertextTransferProtocolVersion2RelayDirection direction)
    {
        if (direction == HypertextTransferProtocolVersion2RelayDirection.ClientToUpstream)
        {
            return _clientHeaderAssembler;
        }

        return _upstreamHeaderAssembler;
    }

    private HypertextTransferProtocolVersion2HpackDecoder SelectDecoder(HypertextTransferProtocolVersion2RelayDirection direction)
    {
        if (direction == HypertextTransferProtocolVersion2RelayDirection.ClientToUpstream)
        {
            return _clientHeaderDecoder;
        }

        return _upstreamHeaderDecoder;
    }

    private ConcurrentDictionary<uint, bool> SelectPendingEndStreamMap(HypertextTransferProtocolVersion2RelayDirection direction)
    {
        if (direction == HypertextTransferProtocolVersion2RelayDirection.ClientToUpstream)
        {
            return _clientPendingEndStream;
        }

        return _upstreamPendingEndStream;
    }
}
