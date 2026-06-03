using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="Socks5ConnectRequestParser" />.
/// </summary>
public sealed class Socks5ConnectRequestParserTests
{
    /// <summary>
    ///     Verifies an IPv4 SOCKS5 CONNECT request parses.
    /// </summary>
    [Test]
    public async Task TryParse_Ipv4Connect_ReturnsRequest()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x01, 192, 168, 1, 1, 0x01, 0xBB };
        var request = Socks5ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.DestinationAddressType).IsEqualTo(Socks5AddressType.InternetProtocolVersionFour);
        await Assert.That(request.DestinationAddress).IsEqualTo("192.168.1.1");
        await Assert.That(request.DestinationPort).IsEqualTo(443);
        await Assert.That(request.TotalLength).IsEqualTo(10);
    }

    /// <summary>
    ///     Verifies a domain-name CONNECT request parses.
    /// </summary>
    [Test]
    public async Task TryParse_DomainConnect_ReturnsRequest()
    {
        var hostname = "example.com";
        var hostnameBytes = System.Text.Encoding.ASCII.GetBytes(hostname);
        var buffer = new byte[5 + hostnameBytes.Length + 2];
        buffer[0] = 0x05;
        buffer[1] = 0x01;
        buffer[2] = 0x00;
        buffer[3] = 0x03;
        buffer[4] = (byte)hostnameBytes.Length;
        Array.Copy(hostnameBytes, 0, buffer, 5, hostnameBytes.Length);
        buffer[^2] = 0x00;
        buffer[^1] = 0x50;

        var request = Socks5ConnectRequestParser.TryParse(buffer);

        await Assert.That(request!.DestinationAddressType).IsEqualTo(Socks5AddressType.DomainName);
        await Assert.That(request.DestinationAddress).IsEqualTo("example.com");
        await Assert.That(request.DestinationPort).IsEqualTo(80);
        await Assert.That(request.TotalLength).IsEqualTo(buffer.Length);
    }

    /// <summary>
    ///     Verifies an IPv6 CONNECT request parses.
    /// </summary>
    [Test]
    public async Task TryParse_Ipv6Connect_ReturnsRequest()
    {
        var addressBytes = new byte[] { 0x20, 0x01, 0x0D, 0xB8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x01 };
        var buffer = new byte[4 + 16 + 2];
        buffer[0] = 0x05;
        buffer[1] = 0x01;
        buffer[2] = 0x00;
        buffer[3] = 0x04;
        Array.Copy(addressBytes, 0, buffer, 4, 16);
        buffer[20] = 0x00;
        buffer[21] = 0x50;

        var request = Socks5ConnectRequestParser.TryParse(buffer);

        await Assert.That(request!.DestinationAddressType).IsEqualTo(Socks5AddressType.InternetProtocolVersionSix);
        await Assert.That(request.DestinationPort).IsEqualTo(80);
        await Assert.That(request.TotalLength).IsEqualTo(22);
    }

    /// <summary>
    ///     Verifies incomplete buffers return null.
    /// </summary>
    [Test]
    public async Task TryParse_TooShort_ReturnsNull()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00 };
        var request = Socks5ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies IPv4 with insufficient bytes returns null.
    /// </summary>
    [Test]
    public async Task TryParse_Ipv4Truncated_ReturnsNull()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x01, 192, 168 };
        var request = Socks5ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies IPv6 with insufficient bytes returns null.
    /// </summary>
    [Test]
    public async Task TryParse_Ipv6Truncated_ReturnsNull()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x04, 0x20, 0x01 };
        var request = Socks5ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies a domain-name request missing the port bytes returns null.
    /// </summary>
    [Test]
    public async Task TryParse_DomainTruncated_ReturnsNull()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x03, 11, (byte)'e', (byte)'x', (byte)'a' };
        var request = Socks5ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies a non-SOCKS5 version byte throws.
    /// </summary>
    [Test]
    public async Task TryParse_BadVersion_Throws()
    {
        var bytes = new byte[] { 0x04, 0x01, 0x00, 0x01 };

        await Assert.That(() => Socks5ConnectRequestParser.TryParse(bytes)).Throws<System.IO.InvalidDataException>();
    }

    /// <summary>
    ///     Verifies BIND (0x02) throws (only CONNECT supported).
    /// </summary>
    [Test]
    public async Task TryParse_BindCommand_Throws()
    {
        var bytes = new byte[] { 0x05, 0x02, 0x00, 0x01 };

        await Assert.That(() => Socks5ConnectRequestParser.TryParse(bytes)).Throws<System.IO.InvalidDataException>();
    }

    /// <summary>
    ///     Verifies an unknown ATYP throws.
    /// </summary>
    [Test]
    public async Task TryParse_UnknownAddressType_Throws()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x09 };

        await Assert.That(() => Socks5ConnectRequestParser.TryParse(bytes)).Throws<System.IO.InvalidDataException>();
    }

    /// <summary>
    ///     Verifies a non-zero reserved byte (RFC 1928 § 4 requires 0x00) throws.
    /// </summary>
    [Test]
    public async Task TryParse_NonZeroReservedByte_Throws()
    {
        var bytes = new byte[] { 0x05, 0x01, 0xFF, 0x01, 192, 168, 1, 1, 0x01, 0xBB };

        await Assert.That(() => Socks5ConnectRequestParser.TryParse(bytes)).Throws<System.IO.InvalidDataException>();
    }

    /// <summary>
    ///     A domain-name address type with the buffer exactly at the 4-byte boundary (so
    ///     the length byte itself is absent) must return null rather than throwing.
    /// </summary>
    [Test]
    public async Task TryParse_DomainAtypNoLengthByte_ReturnsNull()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x03 };

        var request = Socks5ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }
}
