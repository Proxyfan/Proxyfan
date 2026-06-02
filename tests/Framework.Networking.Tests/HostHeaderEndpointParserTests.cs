using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HostHeaderEndpointParser" />, covering plain hostnames,
///     IPv4 literals, and bracketed IPv6 syntax (with and without an explicit port).
/// </summary>
public sealed class HostHeaderEndpointParserTests
{
    private const int DefaultPort = 80;

    /// <summary>
    ///     A plain hostname without a port defaults to the supplied default port.
    /// </summary>
    [Test]
    public async Task Parse_PlainHostWithoutPort_UsesDefaultPort()
    {
        var target = HostHeaderEndpointParser.Parse("example.com", DefaultPort);

        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Host).IsEqualTo("example.com");
        await Assert.That(target.Port).IsEqualTo(DefaultPort);
    }

    /// <summary>
    ///     A plain hostname with an explicit port parses both segments.
    /// </summary>
    [Test]
    public async Task Parse_PlainHostWithExplicitPort_ParsesHostAndPort()
    {
        var target = HostHeaderEndpointParser.Parse("example.com:8080", DefaultPort);

        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Host).IsEqualTo("example.com");
        await Assert.That(target.Port).IsEqualTo(8080);
    }

    /// <summary>
    ///     A bracketed IPv6 literal without a port strips the brackets and defaults the port.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6WithoutPort_StripsBracketsAndUsesDefaultPort()
    {
        var target = HostHeaderEndpointParser.Parse("[2001:db8::1]", DefaultPort);

        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Host).IsEqualTo("2001:db8::1");
        await Assert.That(target.Port).IsEqualTo(DefaultPort);
    }

    /// <summary>
    ///     A bracketed IPv6 literal with an explicit port strips the brackets and parses the port.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6WithExplicitPort_StripsBracketsAndParsesPort()
    {
        var target = HostHeaderEndpointParser.Parse("[2001:db8::1]:8443", DefaultPort);

        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Host).IsEqualTo("2001:db8::1");
        await Assert.That(target.Port).IsEqualTo(8443);
    }

    /// <summary>
    ///     A bracketed IPv6 loopback literal is recognised as a valid host.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6Loopback_StripsBrackets()
    {
        var target = HostHeaderEndpointParser.Parse("[::1]:9000", DefaultPort);

        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Host).IsEqualTo("::1");
        await Assert.That(target.Port).IsEqualTo(9000);
    }

    /// <summary>
    ///     A bracketed value missing its closing bracket is rejected.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6MissingClosingBracket_ReturnsNull()
    {
        var target = HostHeaderEndpointParser.Parse("[2001:db8::1", DefaultPort);

        await Assert.That(target).IsNull();
    }

    /// <summary>
    ///     A bracketed value with garbage after the closing bracket (not <c>:port</c>) is rejected.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6WithTrailingGarbage_ReturnsNull()
    {
        var target = HostHeaderEndpointParser.Parse("[2001:db8::1]x", DefaultPort);

        await Assert.That(target).IsNull();
    }

    /// <summary>
    ///     A bracketed value with an empty host (<c>[]</c>) is rejected.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6WithEmptyHost_ReturnsNull()
    {
        var target = HostHeaderEndpointParser.Parse("[]:80", DefaultPort);

        await Assert.That(target).IsNull();
    }

    /// <summary>
    ///     A bracketed IPv6 literal followed by a non-numeric port is rejected.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6WithNonNumericPort_ReturnsNull()
    {
        var target = HostHeaderEndpointParser.Parse("[2001:db8::1]:abc", DefaultPort);

        await Assert.That(target).IsNull();
    }

    /// <summary>
    ///     A bracketed IPv6 literal followed by an out-of-range port is rejected.
    /// </summary>
    [Test]
    public async Task Parse_BracketedIpv6WithOutOfRangePort_ReturnsNull()
    {
        var target = HostHeaderEndpointParser.Parse("[2001:db8::1]:99999", DefaultPort);

        await Assert.That(target).IsNull();
    }

    /// <summary>
    ///     A whitespace-only Host value is rejected.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceOnlyValue_ReturnsNull()
    {
        var target = HostHeaderEndpointParser.Parse("   ", DefaultPort);

        await Assert.That(target).IsNull();
    }
}
