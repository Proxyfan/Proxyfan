using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="AcceptErrorClassifier" />.
/// </summary>
public sealed class AcceptErrorClassifierTests
{
    /// <summary>
    ///     Verifies cancellation makes the loop terminate regardless of socket error code.
    /// </summary>
    [Test]
    public async Task HasFatalError_CancellationRequested_ReturnsTrue()
    {
        var exception = new SocketException((int)SocketError.ConnectionReset);

        var result = AcceptErrorClassifier.HasFatalError(exception, cancellationRequested: true);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies OperationAborted is fatal.
    /// </summary>
    [Test]
    public async Task HasFatalError_OperationAborted_ReturnsTrue()
    {
        var exception = new SocketException((int)SocketError.OperationAborted);

        var result = AcceptErrorClassifier.HasFatalError(exception, cancellationRequested: false);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies Interrupted is fatal.
    /// </summary>
    [Test]
    public async Task HasFatalError_Interrupted_ReturnsTrue()
    {
        var exception = new SocketException((int)SocketError.Interrupted);

        var result = AcceptErrorClassifier.HasFatalError(exception, cancellationRequested: false);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies recoverable socket errors are not fatal.
    /// </summary>
    [Test]
    [Arguments(SocketError.ConnectionReset)]
    [Arguments(SocketError.ConnectionAborted)]
    [Arguments(SocketError.NetworkDown)]
    public async Task HasFatalError_RecoverableError_ReturnsFalse(SocketError code)
    {
        var exception = new SocketException((int)code);

        var result = AcceptErrorClassifier.HasFatalError(exception, cancellationRequested: false);

        await Assert.That(result).IsFalse();
    }
}
