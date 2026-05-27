using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ProxyAuthorizationHeader" />.
/// </summary>
public sealed class ProxyAuthorizationHeaderTests
{
    /// <summary>
    ///     Verifies the helper returns <see langword="null" /> when no credentials are configured.
    /// </summary>
    [Test]
    public async Task Build_NoCredentials_ReturnsNull()
    {
        var options = new UpstreamProxyOptions { IsEnabled = true, Host = "proxy", Port = 8080 };

        var header = ProxyAuthorizationHeader.Build(options);

        await Assert.That(header).IsNull();
    }

    /// <summary>
    ///     Verifies the helper Base64-encodes the credentials per RFC 7617.
    /// </summary>
    [Test]
    public async Task Build_WithCredentials_ReturnsBasicAuthorizationHeader()
    {
        var options = new UpstreamProxyOptions
        {
            IsEnabled = true,
            Host = "proxy",
            Port = 8080,
            Username = "alice",
            Password = "secret",
        };

        var header = ProxyAuthorizationHeader.Build(options);

        await Assert.That(header).IsEqualTo("Basic YWxpY2U6c2VjcmV0");
    }

    /// <summary>
    ///     Verifies the helper accepts an empty password (RFC 7617 allows empty passwords).
    /// </summary>
    [Test]
    public async Task Build_EmptyPassword_ReturnsHeaderWithTrailingColon()
    {
        var options = new UpstreamProxyOptions
        {
            IsEnabled = true,
            Host = "proxy",
            Port = 8080,
            Username = "alice",
            Password = string.Empty,
        };

        var header = ProxyAuthorizationHeader.Build(options);

        await Assert.That(header).IsEqualTo("Basic YWxpY2U6");
    }

    /// <summary>
    ///     Verifies the helper returns <see langword="null" /> when only the username is set.
    /// </summary>
    [Test]
    public async Task Build_OnlyUsername_ReturnsNull()
    {
        var options = new UpstreamProxyOptions
        {
            IsEnabled = true,
            Host = "proxy",
            Port = 8080,
            Username = "alice",
        };

        var header = ProxyAuthorizationHeader.Build(options);

        await Assert.That(header).IsNull();
    }
}
