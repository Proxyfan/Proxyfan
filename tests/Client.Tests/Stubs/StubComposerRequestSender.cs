using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Stub <see cref="IComposerRequestSender" /> that captures each sent request and returns a
///     pre-canned response. Allows tests to drive the composer view model without any real
///     network traffic.
/// </summary>
public sealed class StubComposerRequestSender : IComposerRequestSender
{
    /// <summary>
    ///     Gets the list of requests captured by <see cref="SendAsync" /> in call order.
    /// </summary>
    public List<HypertextTransferProtocolRequestData> CapturedRequests { get; } = [];

    /// <summary>
    ///     Gets or sets the exception that <see cref="SendAsync" /> should throw instead of
    ///     returning a response.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    ///     Gets or sets the response that <see cref="SendAsync" /> should return.
    /// </summary>
    public HypertextTransferProtocolResponseData ResponseToReturn { get; set; } =
        new(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });

    /// <inheritdoc />
    public Task<HypertextTransferProtocolResponseData> SendAsync(
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        CapturedRequests.Add(request);

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(ResponseToReturn);
    }
}
