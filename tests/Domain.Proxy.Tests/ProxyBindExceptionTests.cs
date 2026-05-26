using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ProxyBindException" />.
/// </summary>
public sealed class ProxyBindExceptionTests
{
    /// <summary>
    ///     Verifies that the constructor stores the port and incorporates the inner exception message.
    /// </summary>
    [Test]
    public async Task Constructor_WithValues_StoresPortAndMessage()
    {
        var inner = new SocketException(10048);

        var exception = new ProxyBindException(8080, inner);

        await Assert.That(exception.Port).IsEqualTo(8080);
        await Assert.That(exception.InnerException).IsSameReferenceAs(inner);
        await Assert.That(exception.Message).Contains("8080");
    }
}