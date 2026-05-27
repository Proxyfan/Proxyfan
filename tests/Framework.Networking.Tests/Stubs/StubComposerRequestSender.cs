using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="IComposerRequestSender" /> that records each
///     dispatched request and returns a configurable response. Used by the
///     <see cref="RequestRepeater" /> tests to drive the pipeline without any real
///     network traffic.
/// </summary>
public sealed class StubComposerRequestSender : IComposerRequestSender
{
    /// <summary>
    ///     Gets the list of requests captured by <see cref="SendAsync" /> in call order.
    /// </summary>
    public List<HypertextTransferProtocolRequestData> CapturedRequests { get; } = [];

    /// <summary>
    ///     Gets or sets an exception that <see cref="SendAsync" /> throws instead of returning.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    ///     Gets or sets the response that <see cref="SendAsync" /> returns.
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
