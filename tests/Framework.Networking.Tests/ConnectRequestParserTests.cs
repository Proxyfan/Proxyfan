using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ConnectRequestParser" />.
/// </summary>
public sealed class ConnectRequestParserTests
{
    /// <summary>
    ///     Verifies that a valid CONNECT request with host and port returns a <see cref="ConnectTarget" />.
    /// </summary>
    [Test]
    public async Task Parse_ValidHostAndPort_ReturnsConnectTarget()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT api.example.com:443 HTTP/1.1\r\nHost: api.example.com:443\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Host).IsEqualTo("api.example.com");
        await Assert.That(result.Port).IsEqualTo(443);
    }

    /// <summary>
    ///     Verifies that a CONNECT request with a numeric IP target returns the correct host and port.
    /// </summary>
    [Test]
    public async Task Parse_NumericIpTarget_ReturnsConnectTarget()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT 192.168.1.100:8443 HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Host).IsEqualTo("192.168.1.100");
        await Assert.That(result.Port).IsEqualTo(8443);
    }

    /// <summary>
    ///     Verifies that a CONNECT request with no port uses the default port 443.
    /// </summary>
    [Test]
    public async Task Parse_NoPortInAuthority_ReturnsDefaultPort443()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT example.com HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Host).IsEqualTo("example.com");
        await Assert.That(result.Port).IsEqualTo(443);
    }

    /// <summary>
    ///     Verifies that a request with a non-numeric port returns null.
    /// </summary>
    [Test]
    public async Task Parse_NonNumericPort_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT example.com:abc HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a request with a port of zero returns null.
    /// </summary>
    [Test]
    public async Task Parse_PortZero_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT example.com:0 HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a request with a port above 65535 returns null.
    /// </summary>
    [Test]
    public async Task Parse_PortAboveMaximum_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT example.com:99999 HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that an empty byte array returns null.
    /// </summary>
    [Test]
    public async Task Parse_EmptyInput_ReturnsNull()
    {
        var result = ConnectRequestParser.Parse(Array.Empty<byte>());

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that bytes with no line terminator return null.
    /// </summary>
    [Test]
    public async Task Parse_NoLineTerminatorInInput_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a malformed request line with no authority returns null.
    /// </summary>
    [Test]
    public async Task Parse_RequestLineWithNoAuthority_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a request line with a method+authority but no HTTP version (missing
    ///     the second space) returns null. Exercises the second <c>spaceIndex &lt; 0</c>
    ///     branch in <c>ParseRequestLine</c>.
    /// </summary>
    [Test]
    public async Task Parse_RequestLineMissingHttpVersion_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT example.com:443\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that an authority whose host contains a carriage return is rejected,
    ///     exercising the <c>!HasValidTarget</c> branch after the colon-separated parser
    ///     extracts a valid port.
    /// </summary>
    [Test]
    public async Task Parse_AuthorityHostContainsCarriageReturn_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT bad\rhost:443 HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that an authority with only whitespace before the implicit default port
    ///     is rejected, exercising the <c>!HasValidTarget</c> branch in the colonless
    ///     authority path.
    /// </summary>
    [Test]
    public async Task Parse_AuthorityWhitespaceOnlyHost_ReturnsNull()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "CONNECT \t HTTP/1.1\r\n\r\n");

        var result = ConnectRequestParser.Parse(bytes);

        await Assert.That(result).IsNull();
    }
}
