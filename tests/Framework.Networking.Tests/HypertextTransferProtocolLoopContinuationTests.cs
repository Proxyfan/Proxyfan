using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolLoopContinuation" />.
/// </summary>
public sealed class HypertextTransferProtocolLoopContinuationTests
{
    /// <summary>
    ///     Verifies an aborted request terminates the loop.
    /// </summary>
    [Test]
    public async Task CanContinue_AbortedRequest_ReturnsFalse()
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.1", null, hadAbortedRequest: true);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies "Connection: close" header terminates the loop.
    /// </summary>
    [Test]
    [Arguments("close")]
    [Arguments("Close")]
    [Arguments("CLOSE")]
    public async Task CanContinue_ConnectionClose_ReturnsFalse(string connectionHeaderValue)
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.1", connectionHeaderValue, hadAbortedRequest: false);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies HTTP/1.1 default keeps the loop alive.
    /// </summary>
    [Test]
    public async Task CanContinue_HttpOneOneDefault_ReturnsTrue()
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.1", null, hadAbortedRequest: false);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies HTTP/1.0 defaults to closing the connection.
    /// </summary>
    [Test]
    public async Task CanContinue_HttpOneZeroDefault_ReturnsFalse()
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.0", null, hadAbortedRequest: false);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies HTTP/1.0 with explicit keep-alive is honored.
    /// </summary>
    [Test]
    public async Task CanContinue_HttpOneZeroKeepAlive_ReturnsTrue()
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.0", "keep-alive", hadAbortedRequest: false);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies null version defaults to HTTP/1.1 behavior.
    /// </summary>
    [Test]
    public async Task CanContinue_NullVersion_ReturnsTrue()
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue(null, null, hadAbortedRequest: false);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies "close" is recognised when present as a token within a comma-separated
    ///     Connection header list (RFC 7230 token-list semantics).
    /// </summary>
    [Test]
    [Arguments("close, foo")]
    [Arguments("foo, close")]
    [Arguments("upgrade, CLOSE")]
    [Arguments(" close ")]
    public async Task CanContinue_HttpOneOneConnectionCloseToken_ReturnsFalse(string connectionHeaderValue)
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.1", connectionHeaderValue, hadAbortedRequest: false);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies HTTP/1.0 honors keep-alive when it appears as a token in a comma-separated
    ///     Connection header list.
    /// </summary>
    [Test]
    [Arguments("keep-alive, upgrade")]
    [Arguments("upgrade, keep-alive")]
    [Arguments("Keep-Alive , Foo")]
    public async Task CanContinue_HttpOneZeroKeepAliveToken_ReturnsTrue(string connectionHeaderValue)
    {
        var result = HypertextTransferProtocolLoopContinuation.CanContinue("HTTP/1.0", connectionHeaderValue, hadAbortedRequest: false);

        await Assert.That(result).IsTrue();
    }
}
