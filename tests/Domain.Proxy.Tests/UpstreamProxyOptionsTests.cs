using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="UpstreamProxyOptions" />.
/// </summary>
public sealed class UpstreamProxyOptionsTests
{
    /// <summary>
    ///     Verifies that the default constructor sets sensible disabled defaults.
    /// </summary>
    [Test]
    public async Task Constructor_Default_HasDisabledStateAndDefaultPort()
    {
        var options = new UpstreamProxyOptions();

        await Assert.That(options.IsEnabled).IsFalse();
        await Assert.That(options.Port).IsEqualTo(8080);
        await Assert.That(options.Host).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="UpstreamProxyOptions.HasValidConfiguration" /> returns false when
    ///     the configuration is disabled.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_Disabled_ReturnsFalse()
    {
        var options = new UpstreamProxyOptions
        {
            IsEnabled = false,
            Host = "upstream.example",
            Port = 8080,
        };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that an enabled configuration with no host returns false.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_EnabledNoHost_ReturnsFalse()
    {
        var options = new UpstreamProxyOptions { IsEnabled = true, Port = 8080 };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that an enabled configuration with a whitespace host returns false.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_EnabledWhitespaceHost_ReturnsFalse()
    {
        var options = new UpstreamProxyOptions { IsEnabled = true, Host = "   ", Port = 8080 };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that an enabled configuration with port out of range returns false.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_EnabledPortOutOfRange_ReturnsFalse()
    {
        var options = new UpstreamProxyOptions { IsEnabled = true, Host = "upstream.example", Port = 0 };

        await Assert.That(options.HasValidConfiguration()).IsFalse();
    }

    /// <summary>
    ///     Verifies that an enabled configuration with valid host and port returns true.
    /// </summary>
    [Test]
    public async Task HasValidConfiguration_ValidConfiguration_ReturnsTrue()
    {
        var options = new UpstreamProxyOptions { IsEnabled = true, Host = "upstream.example", Port = 3128 };

        await Assert.That(options.HasValidConfiguration()).IsTrue();
    }
}
