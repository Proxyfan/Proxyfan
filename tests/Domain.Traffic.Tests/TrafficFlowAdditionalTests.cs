using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Additional state-transition tests for <see cref="TrafficFlow" /> covering invalid transitions.
/// </summary>
public sealed class TrafficFlowAdditionalTests
{
    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Complete" /> throws when invoked on a pending flow.
    /// </summary>
    [Test]
    public async Task Complete_WhenPending_ThrowsInvalidOperationException()
    {
        var flow = CreateFlow();

        await Assert.That(flow.Complete).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetRequest" /> throws when invoked twice.
    /// </summary>
    [Test]
    public async Task SetRequest_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        flow.SetRequest(request);

        await Assert.That(() => flow.SetRequest(request)).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetResponse" /> throws when invoked on a pending flow.
    /// </summary>
    [Test]
    public async Task SetResponse_WhenPending_ThrowsInvalidOperationException()
    {
        var flow = CreateFlow();
        var response = CreateResponse();

        await Assert.That(() => flow.SetResponse(response)).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Abort" /> is a no-op when the flow has completed.
    /// </summary>
    [Test]
    public async Task Abort_WhenComplete_StaysComplete()
    {
        var flow = CreateFlow();
        flow.SetRequest(CreateRequest());
        flow.SetResponse(CreateResponse());
        flow.Complete();

        flow.Abort();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Abort" /> is a no-op when the flow has failed.
    /// </summary>
    [Test]
    public async Task Abort_WhenFailed_StaysFailed()
    {
        var flow = CreateFlow();
        flow.Fail();

        flow.Abort();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Fail" /> is a no-op when the flow has completed.
    /// </summary>
    [Test]
    public async Task Fail_WhenComplete_StaysComplete()
    {
        var flow = CreateFlow();
        flow.SetRequest(CreateRequest());
        flow.SetResponse(CreateResponse());
        flow.Complete();

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Fail" /> is a no-op when the flow has been aborted.
    /// </summary>
    [Test]
    public async Task Fail_WhenAborted_StaysAborted()
    {
        var flow = CreateFlow();
        flow.Abort();

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Aborted);
    }

    private static TrafficFlow CreateFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:12345", DateTimeOffset.UtcNow);
        return flow;
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