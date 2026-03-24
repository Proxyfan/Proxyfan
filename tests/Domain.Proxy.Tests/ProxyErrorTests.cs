using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>Tests for <see cref="ProxyError" /> and its derived records.</summary>
internal sealed class ProxyErrorTests
{
    /// <summary>Verifies the code for <see cref="ProxyBindError" />.</summary>
    [Test]
    public async Task ProxyBindError_Code_IsProxyBindFailed()
    {
        var ex = new SocketException();
        var error = new ProxyBindError(8080, ex);
        await Assert.That(error.Code).IsEqualTo("PROXY_BIND_FAILED");
    }

    /// <summary>Verifies that <see cref="ProxyBindError.Port" /> is set.</summary>
    [Test]
    public async Task ProxyBindError_Port_ReturnsProvidedPort()
    {
        var ex = new SocketException();
        var error = new ProxyBindError(9090, ex);
        await Assert.That(error.Port).IsEqualTo(9090);
    }

    /// <summary>Verifies that <see cref="DomainError.InnerException" /> is set.</summary>
    [Test]
    public async Task ProxyBindError_InnerException_ReturnsProvidedException()
    {
        var ex = new SocketException();
        var error = new ProxyBindError(8080, ex);
        await Assert.That(error.InnerException).IsSameReferenceAs(ex);
    }

    /// <summary>Verifies the code for <see cref="ProxyAlreadyRunningError" />.</summary>
    [Test]
    public async Task ProxyAlreadyRunningError_Code_IsProxyAlreadyRunning()
    {
        var error = new ProxyAlreadyRunningError();
        await Assert.That(error.Code).IsEqualTo("PROXY_ALREADY_RUNNING");
    }

    /// <summary>Verifies the code for <see cref="ProxyNotRunningError" />.</summary>
    [Test]
    public async Task ProxyNotRunningError_Code_IsProxyNotRunning()
    {
        var error = new ProxyNotRunningError();
        await Assert.That(error.Code).IsEqualTo("PROXY_NOT_RUNNING");
    }

    /// <summary>Verifies the code for <see cref="ProxyFaultedError" />.</summary>
    [Test]
    public async Task ProxyFaultedError_Code_IsProxyFaulted()
    {
        var error = new ProxyFaultedError("Start", new InvalidOperationException("boom"));
        await Assert.That(error.Code).IsEqualTo("PROXY_FAULTED");
    }

    /// <summary>Verifies that <see cref="ProxyFaultedError.Operation" /> is set.</summary>
    [Test]
    public async Task ProxyFaultedError_Operation_ReturnsProvidedOperation()
    {
        var error = new ProxyFaultedError("Stop", new InvalidOperationException());
        await Assert.That(error.Operation).IsEqualTo("Stop");
    }
}
