using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="Socks4ConnectRequestParser" />.
/// </summary>
public sealed class Socks4ConnectRequestParserTests
{
    /// <summary>
    ///     Verifies that a standard SOCKS4 CONNECT request parses successfully.
    /// </summary>
    [Test]
    public async Task TryParse_ValidConnectRequest_Parses()
    {
        var bytes = new byte[] { 0x04, 0x01, 0x00, 0x50, 127, 0, 0, 1, 0x00 };

        var request = Socks4ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.DestinationPort).IsEqualTo(80);
        await Assert.That(request.DestinationAddress).IsEqualTo(IPAddress.Loopback);
        await Assert.That(request.UserId).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a CONNECT request with a USERID captures it.
    /// </summary>
    [Test]
    public async Task TryParse_WithUserId_CapturesUserId()
    {
        var prefix = new byte[] { 0x04, 0x01, 0x00, 0x50, 127, 0, 0, 1 };
        var userIdBytes = Encoding.ASCII.GetBytes("alice");
        var bytes = new byte[prefix.Length + userIdBytes.Length + 1];
        prefix.CopyTo(bytes, 0);
        userIdBytes.CopyTo(bytes, prefix.Length);
        bytes[^1] = 0x00;

        var request = Socks4ConnectRequestParser.TryParse(bytes);

        await Assert.That(request!.UserId).IsEqualTo("alice");
    }

    /// <summary>
    ///     Verifies that a buffer shorter than 8 bytes returns null.
    /// </summary>
    [Test]
    public async Task TryParse_BufferTooShort_ReturnsNull()
    {
        var bytes = new byte[] { 0x04, 0x01, 0x00 };

        var request = Socks4ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies that a buffer missing the NUL terminator returns null.
    /// </summary>
    [Test]
    public async Task TryParse_MissingNullTerminator_ReturnsNull()
    {
        var bytes = new byte[] { 0x04, 0x01, 0x00, 0x50, 127, 0, 0, 1, (byte)'a' };

        var request = Socks4ConnectRequestParser.TryParse(bytes);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies that a non-SOCKS4 version byte throws.
    /// </summary>
    [Test]
    public async Task TryParse_WrongVersion_Throws()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00, 0x50, 127, 0, 0, 1, 0x00 };

        await Assert.That(() => Socks4ConnectRequestParser.TryParse(bytes)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a non-CONNECT command byte throws.
    /// </summary>
    [Test]
    public async Task TryParse_NonConnectCommand_Throws()
    {
        var bytes = new byte[] { 0x04, 0x02, 0x00, 0x50, 127, 0, 0, 1, 0x00 };

        await Assert.That(() => Socks4ConnectRequestParser.TryParse(bytes)).Throws<InvalidDataException>();
    }
}
