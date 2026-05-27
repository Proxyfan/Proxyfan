using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="BreakpointPause" />.
/// </summary>
public sealed class BreakpointPauseTests
{
    /// <summary>
    ///     A request-phase pause exposes a null response and a Request phase value.
    /// </summary>
    [Test]
    public async Task Constructor_RequestPhase_HasNullResponse()
    {
        var pause = new BreakpointPause(Guid.NewGuid(), CreateRequest("https://example.com/"));

        await Assert.That(pause.Response).IsNull();
        await Assert.That(pause.Phase).IsEqualTo(BreakpointPhase.Request);
    }

    /// <summary>
    ///     A response-phase pause exposes the response and a Response phase value.
    /// </summary>
    [Test]
    public async Task Constructor_ResponsePhase_HasResponse()
    {
        var request = CreateRequest("https://example.com/");
        var response = CreateResponse();

        var pause = new BreakpointPause(Guid.NewGuid(), request, response);

        await Assert.That(pause.Response).IsSameReferenceAs(response);
        await Assert.That(pause.Phase).IsEqualTo(BreakpointPhase.Response);
    }

    /// <summary>
    ///     ResumeWith completes the decision task with the supplied decision.
    /// </summary>
    [Test]
    public async Task ResumeWith_RequestPhase_CompletesDecisionTask()
    {
        var pause = new BreakpointPause(Guid.NewGuid(), CreateRequest("https://example.com/"));
        var modified = CreateRequest("https://example.com/modified");

        pause.ResumeWith(BreakpointDecisions.ResumeRequest(modified));
        var decision = await pause.WaitForDecisionAsync(CancellationToken.None);

        await Assert.That(decision.IsAborting).IsFalse();
        await Assert.That(decision.ModifiedRequest).IsSameReferenceAs(modified);
        await Assert.That(pause.IsResolved).IsTrue();
    }

    /// <summary>
    ///     Abort completes the decision task with an aborting decision.
    /// </summary>
    [Test]
    public async Task Abort_RequestPhase_CompletesWithAbortDecision()
    {
        var pause = new BreakpointPause(Guid.NewGuid(), CreateRequest("https://example.com/"));

        pause.Abort();
        var decision = await pause.WaitForDecisionAsync(CancellationToken.None);

        await Assert.That(decision.IsAborting).IsTrue();
    }

    /// <summary>
    ///     Cancel surfaces an OperationCanceledException to WaitForDecisionAsync.
    /// </summary>
    [Test]
    public async Task Cancel_PendingPause_ThrowsOperationCanceledException()
    {
        var pause = new BreakpointPause(Guid.NewGuid(), CreateRequest("https://example.com/"));
        using var cts = new CancellationTokenSource();

        pause.Cancel(cts.Token);

        await Assert.That(async () => await pause.WaitForDecisionAsync(CancellationToken.None))
            .Throws<TaskCanceledException>();
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
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
