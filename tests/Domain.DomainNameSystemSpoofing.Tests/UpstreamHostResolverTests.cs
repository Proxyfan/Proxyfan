using System;
using System.Net;
using System.Threading.Tasks;

namespace Proxyfan.Domain.DomainNameSystemSpoofing.Tests;

/// <summary>
///     Tests for <see cref="UpstreamHostResolver" />.
/// </summary>
public sealed class UpstreamHostResolverTests
{
    /// <summary>
    ///     Verifies that an unregistered hostname is returned unchanged so the operating system
    ///     resolver handles it.
    /// </summary>
    [Test]
    public async Task Resolve_WithoutOverride_ReturnsOriginalHostname()
    {
        var map = new DomainNameSystemOverrideMap();
        var resolver = new UpstreamHostResolver(map);

        var result = resolver.Resolve("api.example.com");

        await Assert.That(result).IsEqualTo("api.example.com");
    }

    /// <summary>
    ///     Verifies that an IPv4 override returns the override address rendered as a string.
    /// </summary>
    [Test]
    public async Task Resolve_WithIpv4Override_ReturnsOverrideAddress()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Parse("10.0.0.5")));
        var resolver = new UpstreamHostResolver(map);

        var result = resolver.Resolve("api.example.com");

        await Assert.That(result).IsEqualTo("10.0.0.5");
    }

    /// <summary>
    ///     Verifies that an IPv6 override returns the override address rendered as a string.
    /// </summary>
    [Test]
    public async Task Resolve_WithIpv6Override_ReturnsOverrideAddress()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("api.example.com", IPAddress.Parse("::1")));
        var resolver = new UpstreamHostResolver(map);

        var result = resolver.Resolve("api.example.com");

        await Assert.That(result).IsEqualTo("::1");
    }

    /// <summary>
    ///     Verifies that hostname matching is case-insensitive (underlying map uses
    ///     OrdinalIgnoreCase semantics).
    /// </summary>
    [Test]
    public async Task Resolve_CaseInsensitiveLookup_ReturnsOverride()
    {
        var map = new DomainNameSystemOverrideMap();
        map.Add(new DomainNameSystemOverrideEntry("API.Example.com", IPAddress.Loopback));
        var resolver = new UpstreamHostResolver(map);

        var result = resolver.Resolve("api.example.COM");

        await Assert.That(result).IsEqualTo("127.0.0.1");
    }

    /// <summary>
    ///     Verifies that a null hostname throws <see cref="ArgumentNullException" />.
    /// </summary>
    [Test]
    public async Task Resolve_NullHostname_Throws()
    {
        var resolver = new UpstreamHostResolver(new DomainNameSystemOverrideMap());

        await Assert.That(() => resolver.Resolve(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    ///     Verifies that a whitespace hostname throws <see cref="ArgumentException" />.
    /// </summary>
    [Test]
    public async Task Resolve_WhitespaceHostname_Throws()
    {
        var resolver = new UpstreamHostResolver(new DomainNameSystemOverrideMap());

        await Assert.That(() => resolver.Resolve("   ")).Throws<ArgumentException>();
    }
}
