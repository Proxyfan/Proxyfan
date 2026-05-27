using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="TrafficFlow" />.
/// </summary>
public sealed class TrafficFlowTests
{
    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Abort" /> transitions a pending flow to aborted.
    /// </summary>
    [Test]
    public async Task Abort_WhenPending_SetsStatusToAborted()
    {
        var flow = CreateFlow();

        flow.Abort();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Aborted);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Complete" /> records the response completion time.
    /// </summary>
    [Test]
    public async Task Complete_WhenActive_RecordsResponseCompletedAt()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        var response = CreateResponse();
        flow.SetRequest(request);
        flow.SetResponse(response);

        flow.Complete();

        await Assert.That(flow.Timings.ResponseCompletedAt).IsNotNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Complete" /> transitions an active flow to complete.
    /// </summary>
    [Test]
    public async Task Complete_WhenActive_SetsStatusToComplete()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        var response = CreateResponse();
        flow.SetRequest(request);
        flow.SetResponse(response);

        flow.Complete();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Complete);
    }

    /// <summary>
    ///     Verifies that calling <see cref="TrafficFlow.Complete" /> more than once is a no-op.
    /// </summary>
    [Test]
    public async Task Complete_WhenRepeated_RemainsComplete()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        var response = CreateResponse();
        flow.SetRequest(request);
        flow.SetResponse(response);
        flow.Complete();
        var completedAt = flow.Timings.ResponseCompletedAt;

        flow.Complete();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(flow.Timings.ResponseCompletedAt).IsEqualTo(completedAt);
    }

    /// <summary>
    ///     Verifies that a newly created flow has no failure timestamp.
    /// </summary>
    [Test]
    public async Task Constructor_NewFlow_FailedAtIsNull()
    {
        var flow = CreateFlow();
        await Assert.That(flow.FailedAt).IsNull();
    }

    /// <summary>
    ///     Verifies that a newly created flow starts pending.
    /// </summary>
    [Test]
    public async Task Constructor_NewFlow_StatusIsPending()
    {
        var flow = CreateFlow();
        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Pending);
    }

    /// <summary>
    ///     Verifies that a newly created flow starts with empty timings.
    /// </summary>
    [Test]
    public async Task Constructor_NewFlow_TimingsAreEmpty()
    {
        var flow = CreateFlow();

        await Assert.That(flow.Timings.RequestStartedAt).IsNull();
        await Assert.That(flow.Timings.RequestCompletedAt).IsNull();
        await Assert.That(flow.Timings.ResponseStartedAt).IsNull();
        await Assert.That(flow.Timings.ResponseCompletedAt).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Fail" /> transitions an active flow to failed.
    /// </summary>
    [Test]
    public async Task Fail_WhenActive_SetsStatusToFailed()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        flow.SetRequest(request);

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     Verifies that calling <see cref="TrafficFlow.Fail" /> twice preserves the first timestamp.
    /// </summary>
    [Test]
    public async Task Fail_WhenRepeated_PreservesFailedAt()
    {
        var flow = CreateFlow();
        flow.Fail();
        var failedAt = flow.FailedAt;

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
        await Assert.That(flow.FailedAt).IsEqualTo(failedAt);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.Fail" /> transitions a pending flow to failed.
    /// </summary>
    [Test]
    public async Task Fail_WhenPending_SetsStatusToFailed()
    {
        var flow = CreateFlow();

        flow.Fail();

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetColorTag" /> stores the assigned colour.
    /// </summary>
    [Test]
    public async Task SetColorTag_NewValue_UpdatesColorTag()
    {
        var flow = CreateFlow();

        flow.SetColorTag(TrafficFlowColorTag.Red);

        await Assert.That(flow.ColorTag).IsEqualTo(TrafficFlowColorTag.Red);
    }

    /// <summary>
    ///     Verifies that assigning <see cref="TrafficFlowColorTag.None" /> clears any previous colour.
    /// </summary>
    [Test]
    public async Task SetColorTag_ResetToNone_ClearsTag()
    {
        var flow = CreateFlow();
        flow.SetColorTag(TrafficFlowColorTag.Blue);

        flow.SetColorTag(TrafficFlowColorTag.None);

        await Assert.That(flow.ColorTag).IsEqualTo(TrafficFlowColorTag.None);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetComment" /> stores the comment text.
    /// </summary>
    [Test]
    public async Task SetComment_NewValue_UpdatesComment()
    {
        var flow = CreateFlow();

        flow.SetComment("Investigate this request");

        await Assert.That(flow.Comment).IsEqualTo("Investigate this request");
    }

    /// <summary>
    ///     Verifies that whitespace-only comments are treated as null/clear.
    /// </summary>
    [Test]
    public async Task SetComment_WhitespaceOnly_ClearsComment()
    {
        var flow = CreateFlow();
        flow.SetComment("Something");

        flow.SetComment("   ");

        await Assert.That(flow.Comment).IsNull();
    }

    /// <summary>
    ///     Verifies that null comment input clears the comment.
    /// </summary>
    [Test]
    public async Task SetComment_Null_ClearsComment()
    {
        var flow = CreateFlow();
        flow.SetComment("Something");

        flow.SetComment(null);

        await Assert.That(flow.Comment).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetRequest" /> stores the request and timestamps it.
    /// </summary>
    [Test]
    public async Task SetRequest_WhenPending_SetsRequestData()
    {
        var flow = CreateFlow();
        var request = CreateRequest();

        flow.SetRequest(request);

        await Assert.That(flow.Request).IsEqualTo(request);
        await Assert.That(flow.Timings.RequestStartedAt).IsNotNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetRequest" /> transitions a pending flow to active.
    /// </summary>
    [Test]
    public async Task SetRequest_WhenPending_SetsStatusToActive()
    {
        var flow = CreateFlow();
        var request = CreateRequest();

        flow.SetRequest(request);

        await Assert.That(flow.Status).IsEqualTo(TrafficFlowStatus.Active);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetResponse" /> records request and response timings.
    /// </summary>
    [Test]
    public async Task SetResponse_WhenActive_RecordsTimings()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        var response = CreateResponse();
        flow.SetRequest(request);

        flow.SetResponse(response);

        await Assert.That(flow.Timings.RequestCompletedAt).IsNotNull();
        await Assert.That(flow.Timings.ResponseStartedAt).IsNotNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlow.SetResponse" /> stores the response.
    /// </summary>
    [Test]
    public async Task SetResponse_WhenActive_SetsResponseData()
    {
        var flow = CreateFlow();
        var request = CreateRequest();
        var response = CreateResponse();
        flow.SetRequest(request);

        flow.SetResponse(response);

        await Assert.That(flow.Response).IsEqualTo(response);
    }

    private TrafficFlow CreateFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:12345", DateTimeOffset.UtcNow);
        return flow;
    }

    private HypertextTransferProtocolRequestData CreateRequest()
    {
        byte[] body =
        [
            1,
            2,
            3,
        ];
        var requestUri = new Uri("https://example.com/");
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = headers,
            Method = "GET",
            RequestUri = requestUri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return request;
    }

    private HypertextTransferProtocolResponseData CreateResponse()
    {
        byte[] body =
        [
            4,
            5,
            6,
        ];
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/plain");
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        return response;
    }
}