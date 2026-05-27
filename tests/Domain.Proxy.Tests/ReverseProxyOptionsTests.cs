using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyOptions" />.
/// </summary>
public sealed class ReverseProxyOptionsTests
{
    /// <summary>
    ///     Verifies that the default constructor returns disabled and listen port 8888.
    /// </summary>
    [Test]
    public async Task Constructor_Defaults_AreSensible()
    {
        var options = new ReverseProxyOptions();

        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(options.ListenPort).IsEqualTo(8888);
        await Assert.That(options.UpstreamPort).IsEqualTo(80);
        await Assert.That(options.AllowTransportLayerSecurityToUpstream).IsFalse();
    }

    /// <summary>
    ///     Verifies that disabled options report invalid.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_WhenDisabled_ReturnsFalse()
    {
        var options = new ReverseProxyOptions
        {
            IsEnabled = false,
            UpstreamHost = "backend.local",
        };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that an enabled config with valid host and ports is reported as valid.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_FullyConfigured_ReturnsTrue()
    {
        var options = new ReverseProxyOptions
        {
            IsEnabled = true,
            ListenPort = 9090,
            UpstreamHost = "backend.local",
            UpstreamPort = 8080,
            AllowTransportLayerSecurityToUpstream = true,
        };

        await Assert.That(options.HasValidConfiguration()).IsTrue();
    }

    /// <summary>
    ///     Verifies that an invalid listen port reports invalid.
    /// </summary>
    /// <param name="port">The port to test.</param>
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(70000)]
    public async Task HasValidConfiguration_BadListenPort_ReturnsFalse(int port)
    {
        var options = new ReverseProxyOptions
        {
            IsEnabled = true,
            ListenPort = port,
            UpstreamHost = "backend.local",
            UpstreamPort = 80,
        };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that an invalid upstream port reports invalid.
    /// </summary>
    /// <param name="port">The port to test.</param>
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(70000)]
    public async Task HasValidConfiguration_BadUpstreamPort_ReturnsFalse(int port)
    {
        var options = new ReverseProxyOptions
        {
            IsEnabled = true,
            ListenPort = 8888,
            UpstreamHost = "backend.local",
            UpstreamPort = port,
        };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that a blank or null upstream host reports invalid.
    /// </summary>
    /// <param name="host">The host string to test.</param>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task HasValidConfiguration_BlankUpstreamHost_ReturnsFalse(string? host)
    {
        var options = new ReverseProxyOptions
        {
            IsEnabled = true,
            ListenPort = 8888,
            UpstreamHost = host,
            UpstreamPort = 80,
        };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }
}
