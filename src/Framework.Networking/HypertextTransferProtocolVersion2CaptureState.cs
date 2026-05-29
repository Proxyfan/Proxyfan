using System;
using System.Buffers;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Per-stream MITM accumulator for HTTP/2 traffic capture. As frames flow through the
///     <see cref="HypertextTransferProtocolVersion2Orchestrator" />, this state object is
///     updated for the originating stream so a single
///     <see cref="Proxyfan.Domain.Traffic.TrafficFlow" /> can be produced when the stream
///     reaches the closed state in both directions.
/// </summary>
public sealed class HypertextTransferProtocolVersion2CaptureState
{
    private readonly ArrayBufferWriter<byte> _requestBody;
    private readonly List<HypertextTransferProtocolVersion2HpackHeaderField> _requestHeaders;
    private readonly ArrayBufferWriter<byte> _responseBody;
    private readonly List<HypertextTransferProtocolVersion2HpackHeaderField> _responseHeaders;

    /// <summary>
    ///     Gets a value indicating whether the client signalled END_STREAM on the request side.
    /// </summary>
    public bool IsRequestEnded { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the upstream signalled END_STREAM on the response
    ///     side.
    /// </summary>
    public bool IsResponseEnded { get; private set; }

    /// <summary>
    ///     Gets the accumulated request body bytes appended from DATA frames sent by the client.
    /// </summary>
    public ReadOnlyMemory<byte> RequestBody => _requestBody.WrittenMemory;

    /// <summary>
    ///     Gets the request header list accumulated from HEADERS + CONTINUATION frames sent by
    ///     the client.
    /// </summary>
    public IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> RequestHeaders => _requestHeaders;

    /// <summary>
    ///     Gets the accumulated response body bytes appended from DATA frames sent by the
    ///     upstream server.
    /// </summary>
    public ReadOnlyMemory<byte> ResponseBody => _responseBody.WrittenMemory;

    /// <summary>
    ///     Gets the response header list accumulated from HEADERS + CONTINUATION frames sent by
    ///     the upstream server.
    /// </summary>
    public IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> ResponseHeaders => _responseHeaders;

    /// <summary>
    ///     Gets the 31-bit stream identifier this state is tracking.
    /// </summary>
    public uint StreamIdentifier { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolVersion2CaptureState" /> for
    ///     the supplied stream identifier.
    /// </summary>
    /// <param name="streamIdentifier">The 31-bit stream identifier.</param>
    public HypertextTransferProtocolVersion2CaptureState(uint streamIdentifier)
    {
        StreamIdentifier = streamIdentifier;
        var requestBody = new ArrayBufferWriter<byte>();
        var responseBody = new ArrayBufferWriter<byte>();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>();
        var responseHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>();
        _requestBody = requestBody;
        _responseBody = responseBody;
        _requestHeaders = requestHeaders;
        _responseHeaders = responseHeaders;
    }

    /// <summary>
    ///     Appends DATA bytes received from the client and optionally marks END_STREAM.
    /// </summary>
    /// <param name="data">The DATA payload (after padding stripped).</param>
    /// <param name="isEndStream">Whether the originating DATA frame had END_STREAM set.</param>
    public void AppendRequestData(ReadOnlySpan<byte> data, bool isEndStream)
    {
        _requestBody.Write(data);

        if (isEndStream)
        {
            IsRequestEnded = true;
        }
    }

    /// <summary>
    ///     Appends request headers received from the client and optionally marks END_STREAM.
    /// </summary>
    /// <param name="headers">The decoded HPACK header fields.</param>
    /// <param name="isEndStream">Whether the originating HEADERS frame had END_STREAM set.</param>
    public void AppendRequestHeaders(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers,
        bool isEndStream)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            _requestHeaders.Add(headers[index]);
        }

        if (isEndStream)
        {
            IsRequestEnded = true;
        }
    }

    /// <summary>
    ///     Appends DATA bytes received from the upstream and optionally marks END_STREAM.
    /// </summary>
    /// <param name="data">The DATA payload (after padding stripped).</param>
    /// <param name="isEndStream">Whether the originating DATA frame had END_STREAM set.</param>
    public void AppendResponseData(ReadOnlySpan<byte> data, bool isEndStream)
    {
        _responseBody.Write(data);

        if (isEndStream)
        {
            IsResponseEnded = true;
        }
    }

    /// <summary>
    ///     Appends response headers received from the upstream and optionally marks END_STREAM.
    /// </summary>
    /// <param name="headers">The decoded HPACK header fields.</param>
    /// <param name="isEndStream">Whether the originating HEADERS frame had END_STREAM set.</param>
    public void AppendResponseHeaders(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers,
        bool isEndStream)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            _responseHeaders.Add(headers[index]);
        }

        if (isEndStream)
        {
            IsResponseEnded = true;
        }
    }
}
