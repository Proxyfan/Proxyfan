using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ProxyOptions" />.
/// </summary>
public sealed class ProxyOptionsTests
{
    /// <summary>
    ///     Verifies the default value of <see cref="ProxyOptions.BindAddress" />.
    /// </summary>
    [Test]
    public async Task BindAddress_Default_IsEqualToLoopback()
    {
        var options = new ProxyOptions();
        await Assert.That(options.BindAddress).IsEqualTo("127.0.0.1");
    }

    /// <summary>
    ///     Verifies the default value of <see cref="ProxyOptions.IsAutoStart" />.
    /// </summary>
    [Test]
    public async Task IsAutoStart_Default_IsTrue()
    {
        var options = new ProxyOptions();
        await Assert.That(options.IsAutoStart).IsTrue();
    }

    /// <summary>
    ///     Verifies the default value of <see cref="ProxyOptions.MaxConnections" />.
    /// </summary>
    [Test]
    public async Task MaxConnections_Default_IsEqualTo1000()
    {
        var options = new ProxyOptions();
        await Assert.That(options.MaxConnections).IsEqualTo(1000);
    }

    /// <summary>
    ///     Verifies the default value of <see cref="ProxyOptions.Port" />.
    /// </summary>
    [Test]
    public async Task Port_Default_IsEqualTo8080()
    {
        var options = new ProxyOptions();
        await Assert.That(options.Port).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that setting <see cref="ProxyOptions.Port" /> assigns the specified value.
    /// </summary>
    [Test]
    public async Task Port_WhenSet_ReturnsAssignedValue()
    {
        var options = new ProxyOptions { Port = 9090 };
        await Assert.That(options.Port).IsEqualTo(9090);
    }

    /// <summary>
    ///     Verifies the default value of <see cref="ProxyOptions.ProtocolDetectionTimeout" />.
    /// </summary>
    [Test]
    public async Task ProtocolDetectionTimeout_Default_IsEqualTo5Seconds()
    {
        var options = new ProxyOptions();
        await Assert.That(options.ProtocolDetectionTimeout).IsEqualTo(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    ///     Verifies the default value of <see cref="ProxyOptions.IsRegisterSystemProxy" />.
    /// </summary>
    [Test]
    public async Task IsRegisterSystemProxy_Default_IsTrue()
    {
        var options = new ProxyOptions();
        await Assert.That(options.IsRegisterSystemProxy).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyOptions.SectionKey" /> equals "proxy".
    /// </summary>
    [Test]
    public async Task SectionKey_Constant_IsEqualToProxy()
    {
        var sectionKey = ProxyOptions.SectionKey;
        await Assert.That(sectionKey).IsEqualTo("proxy");
    }
}