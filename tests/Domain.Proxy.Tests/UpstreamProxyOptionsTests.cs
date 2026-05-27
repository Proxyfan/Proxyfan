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

    /// <summary>
    ///     Verifies that the default constructor creates an empty mutable bypass-pattern list.
    /// </summary>
    [Test]
    public async Task Constructor_Default_HasEmptyBypassList()
    {
        var options = new UpstreamProxyOptions();

        await Assert.That(options.BypassPatterns).IsNotNull();
        await Assert.That(options.BypassPatterns.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="UpstreamProxyOptions.HasCredentials" /> returns false when no
    ///     username is configured.
    /// </summary>
    [Test]
    public async Task HasCredentials_NoUsername_ReturnsFalse()
    {
        var options = new UpstreamProxyOptions { Password = "secret" };

        await Assert.That(options.HasCredentials()).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="UpstreamProxyOptions.HasCredentials" /> returns false when the
    ///     password is null even though a username is present.
    /// </summary>
    [Test]
    public async Task HasCredentials_UsernameButNullPassword_ReturnsFalse()
    {
        var options = new UpstreamProxyOptions { Username = "alice" };

        await Assert.That(options.HasCredentials()).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="UpstreamProxyOptions.HasCredentials" /> returns true when both
    ///     username and (possibly empty) password are set.
    /// </summary>
    [Test]
    public async Task HasCredentials_UsernameAndEmptyPassword_ReturnsTrue()
    {
        var options = new UpstreamProxyOptions { Username = "alice", Password = string.Empty };

        await Assert.That(options.HasCredentials()).IsTrue();
    }
}
