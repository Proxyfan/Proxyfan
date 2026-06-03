using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="BreakpointDecision" />.
/// </summary>
public sealed class BreakpointDecisionTests
{
    /// <summary>
    ///     Verifies that <see cref="BreakpointDecisions.Abort" /> returns a decision flagged as aborting.
    /// </summary>
    [Test]
    public async Task Abort_WhenCreated_ReturnsAbortingDecision()
    {
        var decision = BreakpointDecisions.Abort();

        await Assert.That(decision.IsAborting).IsTrue();
        await Assert.That(decision.ModifiedRequest).IsNull();
        await Assert.That(decision.ModifiedResponse).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="BreakpointDecisions.ResumeRequest" /> stores the modified request.
    /// </summary>
    [Test]
    public async Task ResumeRequest_WithRequest_StoresRequest()
    {
        var request = CreateRequest();

        var decision = BreakpointDecisions.ResumeRequest(request);

        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedRequest).IsSameReferenceAs(request);
        await Assert.That(decision.ModifiedResponse).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="BreakpointDecisions.ResumeResponse" /> stores the modified response.
    /// </summary>
    [Test]
    public async Task ResumeResponse_WithResponse_StoresResponse()
    {
        var response = CreateResponse();

        var decision = BreakpointDecisions.ResumeResponse(response);

        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedRequest).IsNull();
        await Assert.That(decision.ModifiedResponse).IsSameReferenceAs(response);
    }

    /// <summary>
    ///     Verifies that constructing an aborting decision with a request payload throws.
    /// </summary>
    [Test]
    public async Task Constructor_AbortingWithRequest_Throws()
    {
        var request = CreateRequest();

        await Assert.That(() => new BreakpointDecision(isAborting: true, modifiedRequest: request, modifiedResponse: null))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that constructing an aborting decision with a response payload throws.
    /// </summary>
    [Test]
    public async Task Constructor_AbortingWithResponse_Throws()
    {
        var response = CreateResponse();

        await Assert.That(() => new BreakpointDecision(isAborting: true, modifiedRequest: null, modifiedResponse: response))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that constructing a non-aborting decision without any payload throws.
    /// </summary>
    [Test]
    public async Task Constructor_NonAbortingWithoutPayload_Throws()
    {
        await Assert.That(() => new BreakpointDecision(isAborting: false, modifiedRequest: null, modifiedResponse: null))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that constructing a non-aborting decision carrying both payloads throws.
    /// </summary>
    [Test]
    public async Task Constructor_NonAbortingWithBothPayloads_Throws()
    {
        var request = CreateRequest();
        var response = CreateResponse();

        await Assert.That(() => new BreakpointDecision(isAborting: false, modifiedRequest: request, modifiedResponse: response))
            .Throws<ArgumentException>();
    }

    private static HypertextTransferProtocolRequestData CreateRequest()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse()
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
