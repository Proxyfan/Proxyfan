using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolForwardingOutcomes" /> and
///     <see cref="HypertextTransferProtocolForwardingOutcome" />.
/// </summary>
public sealed class HypertextTransferProtocolForwardingOutcomesTests
{
    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolForwardingOutcomes.Failure" />
    ///     returns an outcome with <see cref="HypertextTransferProtocolForwardingOutcome.IsFailure" />
    ///     set and no exchange.
    /// </summary>
    [Test]
    public async Task Failure_NoArguments_ReturnsFailureOutcome()
    {
        var outcome = HypertextTransferProtocolForwardingOutcomes.Failure();

        await Assert.That(outcome.IsFailure).IsTrue();
        await Assert.That(outcome.IsStreaming).IsFalse();
        await Assert.That(outcome.Exchange).IsNull();
    }

    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolForwardingOutcomes.Standard" />
    ///     returns an outcome carrying the supplied exchange.
    /// </summary>
    [Test]
    public async Task Standard_WithExchange_ReturnsStandardOutcomeCarryingExchange()
    {
        var exchange = BuildExchange();

        var outcome = HypertextTransferProtocolForwardingOutcomes.Standard(exchange);

        await Assert.That(outcome.IsFailure).IsFalse();
        await Assert.That(outcome.IsStreaming).IsFalse();
        await Assert.That(outcome.Exchange).IsSameReferenceAs(exchange);
    }

    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolForwardingOutcomes.Streamed" />
    ///     returns a streaming outcome with no exchange.
    /// </summary>
    [Test]
    public async Task Streamed_NoArguments_ReturnsStreamingOutcome()
    {
        var outcome = HypertextTransferProtocolForwardingOutcomes.Streamed();

        await Assert.That(outcome.IsFailure).IsFalse();
        await Assert.That(outcome.IsStreaming).IsTrue();
        await Assert.That(outcome.Exchange).IsNull();
    }

    private static HypertextTransferProtocolProxyResponseExchange BuildExchange()
    {
        var responseHeaders = HeaderCollection.Empty.Add("Content-Length", "0");
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        return new HypertextTransferProtocolProxyResponseExchange(
            ReadOnlyMemory<byte>.Empty,
            Array.Empty<byte>(),
            response);
    }
}
