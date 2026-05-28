using Proxyfan.Domain.Traffic;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Provides shared HTTP pipe read and write helpers used by networking handlers.
/// </summary>
public static class HypertextTransferProtocolPipeHelpers
{
    /// <summary>
    ///     Reads a complete HTTP request exchange from the specified pipe.
    /// </summary>
    /// <param name="reader">The pipe reader to consume request bytes from.</param>
    /// <param name="maxBytes">The maximum number of request header bytes to allow.</param>
    /// <param name="cancellationToken">A token that cancels the read operation.</param>
    /// <returns>The parsed request exchange, or <see langword="null" /> when parsing fails.</returns>
    public static async Task<HypertextTransferProtocolProxyRequestExchange?> ReadRequestAsync(
        PipeReader reader,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var headerBytes = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(reader, maxBytes, cancellationToken).ConfigureAwait(false);

        if (headerBytes is null)
        {
            return null;
        }

        var request = HypertextTransferProtocolRequestParser.ParseHeaders(headerBytes);

        if (request is null)
        {
            return null;
        }

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);
        byte[]? bodyBytes;

        if (framing == HypertextTransferProtocolBodyFraming.Chunked)
        {
            bodyBytes = await HypertextTransferProtocolChunkedBodyReader.ReadAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        else if (framing == HypertextTransferProtocolBodyFraming.ContentLength)
        {
            bodyBytes = await ReadBodyAsync(reader, HypertextTransferProtocolRequestParser.GetContentLength(request), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            bodyBytes = [];
        }

        if (bodyBytes is null)
        {
            return null;
        }

        var requestWithBody = CreateRequestWithBody(request, bodyBytes);
        var requestExchange = new HypertextTransferProtocolProxyRequestExchange(bodyBytes, headerBytes, requestWithBody);
        return requestExchange;
    }

    /// <summary>
    ///     Reads a complete HTTP response exchange from the specified pipe. Body framing is
    ///     determined per RFC 7230 § 3.3.3 using <paramref name="requestMethod" /> to honor the
    ///     no-body rule for HEAD responses.
    /// </summary>
    /// <param name="reader">The pipe reader to consume response bytes from.</param>
    /// <param name="maxBytes">The maximum number of response header bytes to allow.</param>
    /// <param name="requestMethod">The HTTP method of the request that produced this response.</param>
    /// <param name="cancellationToken">A token that cancels the read operation.</param>
    /// <returns>The parsed response exchange, or <see langword="null" /> when parsing fails.</returns>
    public static async Task<HypertextTransferProtocolProxyResponseExchange?> ReadResponseAsync(
        PipeReader reader,
        int maxBytes,
        string requestMethod,
        CancellationToken cancellationToken)
    {
        var headerBytes = await PipeReaderHelper.ReadUntilEndOfHeadersAsync(reader, maxBytes, cancellationToken).ConfigureAwait(false);

        if (headerBytes is null)
        {
            return null;
        }

        var response = HypertextTransferProtocolResponseParser.ParseHeaders(headerBytes);

        if (response is null)
        {
            return null;
        }

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, requestMethod);
        var bodyBytes = await ReadResponseBodyAsync(reader, response, framing, cancellationToken).ConfigureAwait(false);

        if (bodyBytes is null)
        {
            return null;
        }

        var responseWithBody = CreateResponseWithBody(response, bodyBytes);
        var responseExchange = new HypertextTransferProtocolProxyResponseExchange(bodyBytes, headerBytes, responseWithBody);
        return responseExchange;
    }

    /// <summary>
    ///     Writes a complete HTTP response exchange to the specified pipe.
    /// </summary>
    /// <param name="writer">The pipe writer to write response bytes to.</param>
    /// <param name="response">The response exchange to write.</param>
    /// <param name="cancellationToken">A token that cancels the write operation.</param>
    /// <returns>A task that completes when the response has been flushed.</returns>
    public static async Task WriteResponseAsync(
        PipeWriter writer,
        HypertextTransferProtocolProxyResponseExchange response,
        CancellationToken cancellationToken)
    {
        await writer.WriteAsync(response.HeaderBytes, cancellationToken).ConfigureAwait(false);
        await writer.WriteAsync(response.Body, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static byte[] CopySequenceToArray(ReadOnlySequence<byte> sequence, int length)
    {
        var buffer = new byte[length];
        var destination = buffer.AsSpan();
        var destinationIndex = 0;

        foreach (var memory in sequence)
        {
            memory.Span.CopyTo(destination[destinationIndex..]);
            destinationIndex += memory.Length;
        }

        return buffer;
    }

    private static HypertextTransferProtocolRequestData CreateRequestWithBody(
        HypertextTransferProtocolRequestData request,
        byte[] bodyBytes)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Method = request.Method,
            RequestUri = request.RequestUri,
            Version = request.Version,
            Headers = request.Headers,
            Body = bodyBytes,
        };
        var requestWithBody = new HypertextTransferProtocolRequestData(parameters);
        return requestWithBody;
    }

    private static HypertextTransferProtocolResponseData CreateResponseWithBody(
        HypertextTransferProtocolResponseData response,
        byte[] bodyBytes)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            StatusCode = response.StatusCode,
            ReasonPhrase = response.ReasonPhrase,
            Version = response.Version,
            Headers = response.Headers,
            Body = bodyBytes,
        };
        var responseWithBody = new HypertextTransferProtocolResponseData(parameters);
        return responseWithBody;
    }

    private static async Task<byte[]?> ReadBodyAsync(
        PipeReader reader,
        long contentLength,
        CancellationToken cancellationToken)
    {
        if (contentLength <= 0)
        {
            return [];
        }

        if (contentLength > int.MaxValue)
        {
            return null;
        }

        var minimumBytes = Convert.ToInt32(contentLength);
        var result = await reader.ReadAtLeastAsync(minimumBytes, cancellationToken).ConfigureAwait(false);
        var buffer = result.Buffer;

        if (buffer.Length < minimumBytes)
        {
            reader.AdvanceTo(buffer.End);
            return null;
        }

        var bodyBuffer = buffer.Slice(0, minimumBytes);
        var bodyBytes = CopySequenceToArray(bodyBuffer, minimumBytes);
        var consumedPosition = buffer.GetPosition(minimumBytes);
        reader.AdvanceTo(consumedPosition, buffer.End);
        return bodyBytes;
    }

    private static async Task<byte[]> ReadBodyUntilCloseAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            foreach (var memory in buffer)
            {
                stream.Write(memory.Span);
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                return stream.ToArray();
            }
        }
    }

    private static async Task<byte[]?> ReadResponseBodyAsync(
        PipeReader reader,
        HypertextTransferProtocolResponseData response,
        HypertextTransferProtocolBodyFraming framing,
        CancellationToken cancellationToken)
    {
        if (framing == HypertextTransferProtocolBodyFraming.None)
        {
            return [];
        }

        if (framing == HypertextTransferProtocolBodyFraming.Chunked)
        {
            return await HypertextTransferProtocolChunkedBodyReader.ReadAsync(reader, cancellationToken).ConfigureAwait(false);
        }

        if (framing == HypertextTransferProtocolBodyFraming.ContentLength)
        {
            var contentLength = HypertextTransferProtocolResponseParser.GetContentLength(response);
            return await ReadBodyAsync(reader, contentLength, cancellationToken).ConfigureAwait(false);
        }

        return await ReadBodyUntilCloseAsync(reader, cancellationToken).ConfigureAwait(false);
    }
}