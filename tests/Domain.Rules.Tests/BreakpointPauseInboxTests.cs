using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="BreakpointPauseInbox" />.
/// </summary>
public sealed class BreakpointPauseInboxTests
{
    /// <summary>
    ///     Add enqueues the pause and raises PauseAdded.
    /// </summary>
    [Test]
    public async Task Add_NewPause_RaisesPauseAddedAndPending()
    {
        var inbox = new BreakpointPauseInbox();
        var pause = NewPause();
        BreakpointPause? observed = null;
        inbox.PauseAdded += p => observed = p;

        inbox.Add(pause);

        await Assert.That(observed).IsSameReferenceAs(pause);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Resolve calls ResumeWith, removes the pause, and raises PauseResolved.
    /// </summary>
    [Test]
    public async Task Resolve_RegisteredPause_RemovesAndResumes()
    {
        var inbox = new BreakpointPauseInbox();
        var pause = NewPause();
        inbox.Add(pause);
        var modified = NewRequest("https://example.com/modified");
        BreakpointPause? observed = null;
        inbox.PauseResolved += p => observed = p;

        inbox.Resolve(pause, BreakpointDecisions.ResumeRequest(modified));

        await Assert.That(observed).IsSameReferenceAs(pause);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
        await Assert.That(pause.IsResolved).IsTrue();
    }

    /// <summary>
    ///     Resolve on an unknown pause is a no-op and does not raise PauseResolved.
    /// </summary>
    [Test]
    public async Task Resolve_UnknownPause_DoesNotRaiseResolved()
    {
        var inbox = new BreakpointPauseInbox();
        var pause = NewPause();
        var raised = 0;
        inbox.PauseResolved += _ => raised++;

        inbox.Resolve(pause, BreakpointDecisions.Abort());

        await Assert.That(raised).IsEqualTo(0);
        await Assert.That(pause.IsResolved).IsFalse();
    }

    /// <summary>
    ///     Abort removes the pause, calls Abort on it, and raises PauseResolved.
    /// </summary>
    [Test]
    public async Task Abort_RegisteredPause_RemovesAndAborts()
    {
        var inbox = new BreakpointPauseInbox();
        var pause = NewPause();
        inbox.Add(pause);
        var raised = 0;
        inbox.PauseResolved += _ => raised++;

        inbox.Abort(pause);

        await Assert.That(raised).IsEqualTo(1);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     GetPending returns a defensive snapshot.
    /// </summary>
    [Test]
    public async Task GetPending_AfterAdd_ReturnsDefensiveSnapshot()
    {
        var inbox = new BreakpointPauseInbox();
        inbox.Add(NewPause());

        var snapshot = inbox.GetPending();
        inbox.Add(NewPause());

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(inbox.GetPending().Count).IsEqualTo(2);
    }

    private static BreakpointPause NewPause()
    {
        return new BreakpointPause(Guid.NewGuid(), NewRequest("https://example.com/"));
    }

    private static HypertextTransferProtocolRequestData NewRequest(string url)
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
}
