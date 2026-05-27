using System.Buffers;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests targeting <see cref="ServerNameIndicationExtractor" /> early-return branches
///     inside <c>HasValidTransportLayerSecurityRecord</c> and <c>HasExtensionsRange</c>.
/// </summary>
public sealed class ServerNameIndicationExtractorEarlyReturnTests
{
    /// <summary>
    ///     Verifies that a record whose record-length field exceeds the buffer returns null.
    /// </summary>
    [Test]
    public async Task Extract_RecordLengthExceedsBuffer_ReturnsNull()
    {
        var bytes = new byte[10];
        bytes[0] = 0x16;
        bytes[1] = 0x03;
        bytes[2] = 0x01;
        bytes[3] = 0xFF;
        bytes[4] = 0xFF;
        bytes[5] = 0x01;

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a ClientHello whose handshake-length field exceeds the buffer returns null.
    /// </summary>
    [Test]
    public async Task Extract_HandshakeLengthExceedsBuffer_ReturnsNull()
    {
        var bytes = new byte[100];
        bytes[0] = 0x16;
        bytes[1] = 0x03;
        bytes[2] = 0x01;
        bytes[3] = 0x00;
        bytes[4] = 95;
        bytes[5] = 0x01;
        bytes[6] = 0xFF;
        bytes[7] = 0xFF;
        bytes[8] = 0xFF;

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a ClientHello with a session-ID length that runs past the buffer returns null.
    /// </summary>
    [Test]
    public async Task Extract_SessionIdLengthOverflow_ReturnsNull()
    {
        var bytes = new byte[100];
        bytes[0] = 0x16;
        bytes[1] = 0x03;
        bytes[2] = 0x01;
        bytes[3] = 0x00;
        bytes[4] = 95;
        bytes[5] = 0x01;
        bytes[6] = 0x00;
        bytes[7] = 0x00;
        bytes[8] = 91;

        for (var index = 11; index < 43; index++)
        {
            bytes[index] = (byte)index;
        }

        bytes[43] = 0xFF;

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that a ClientHello with cipher-suite length overflow returns null.
    /// </summary>
    [Test]
    public async Task Extract_CipherSuitesLengthOverflow_ReturnsNull()
    {
        var bytes = new byte[100];
        bytes[0] = 0x16;
        bytes[1] = 0x03;
        bytes[2] = 0x01;
        bytes[3] = 0x00;
        bytes[4] = 95;
        bytes[5] = 0x01;
        bytes[6] = 0x00;
        bytes[7] = 0x00;
        bytes[8] = 91;

        for (var index = 11; index < 43; index++)
        {
            bytes[index] = (byte)index;
        }

        bytes[43] = 0;
        bytes[44] = 0xFF;
        bytes[45] = 0xFF;

        var result = ServerNameIndicationExtractor.Extract(new ReadOnlySequence<byte>(bytes));

        await Assert.That(result).IsNull();
    }
}
