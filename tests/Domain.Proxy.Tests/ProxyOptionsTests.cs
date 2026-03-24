using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>Tests for <see cref="ProxyOptions" />.</summary>
internal sealed class ProxyOptionsTests
{
    /// <summary>Verifies the default value of <see cref="ProxyOptions.Port" />.</summary>
    [Test]
    public async Task Port_Default_IsEqualTo8080()
    {
        var options = new ProxyOptions();
        await Assert.That(options.Port).IsEqualTo(8080);
    }

    /// <summary>Verifies the default value of <see cref="ProxyOptions.AutoStart" />.</summary>
    [Test]
    public async Task AutoStart_Default_IsTrue()
    {
        var options = new ProxyOptions();
        await Assert.That(options.AutoStart).IsTrue();
    }

    /// <summary>Verifies the default value of <see cref="ProxyOptions.RegisterSystemProxy" />.</summary>
    [Test]
    public async Task RegisterSystemProxy_Default_IsTrue()
    {
        var options = new ProxyOptions();
        await Assert.That(options.RegisterSystemProxy).IsTrue();
    }

    /// <summary>Verifies the default value of <see cref="ProxyOptions.MaxConnections" />.</summary>
    [Test]
    public async Task MaxConnections_Default_IsEqualTo1000()
    {
        var options = new ProxyOptions();
        await Assert.That(options.MaxConnections).IsEqualTo(1000);
    }

    /// <summary>Verifies the default value of <see cref="ProxyOptions.ProtocolDetectionTimeout" />.</summary>
    [Test]
    public async Task ProtocolDetectionTimeout_Default_IsEqualTo5Seconds()
    {
        var options = new ProxyOptions();
        await Assert.That(options.ProtocolDetectionTimeout).IsEqualTo(TimeSpan.FromSeconds(5));
    }

    /// <summary>Verifies that setting <see cref="ProxyOptions.Port" /> assigns the specified value.</summary>
    [Test]
    public async Task Port_WhenSet_ReturnsAssignedValue()
    {
        var options = new ProxyOptions { Port = 9090 };
        await Assert.That(options.Port).IsEqualTo(9090);
    }

    /// <summary>Verifies that <see cref="ProxyOptions.SectionKey" /> equals "proxy".</summary>
    [Test]
    public async Task SectionKey_Constant_IsEqualToProxy()
    {
        var sectionKey = ProxyOptions.SectionKey;
        await Assert.That(sectionKey).IsEqualTo("proxy");
    }
}
