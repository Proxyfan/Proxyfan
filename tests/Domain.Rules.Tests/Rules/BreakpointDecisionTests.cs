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
