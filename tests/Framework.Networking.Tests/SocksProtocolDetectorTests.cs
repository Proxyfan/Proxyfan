using System.Buffers;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="SocksProtocolDetector" />.
/// </summary>
public sealed class SocksProtocolDetectorTests
{
    /// <summary>
    ///     Verifies that 0x04 returns SOCKS4.
    /// </summary>
    [Test]
    public async Task Detect_FirstByteFour_ReturnsSocks4()
    {
        var bytes = new byte[] { 0x04 };

        var detected = SocksProtocolDetector.Detect(new ReadOnlySequence<byte>(bytes));

        await Assert.That(detected).IsEqualTo(SocksVersion.Four);
    }

    /// <summary>
    ///     Verifies that 0x05 returns SOCKS5.
    /// </summary>
    [Test]
    public async Task Detect_FirstByteFive_ReturnsSocks5()
    {
        var bytes = new byte[] { 0x05 };

        var detected = SocksProtocolDetector.Detect(new ReadOnlySequence<byte>(bytes));

        await Assert.That(detected).IsEqualTo(SocksVersion.Five);
    }

    /// <summary>
    ///     Verifies that a non-SOCKS first byte returns null.
    /// </summary>
    [Test]
    public async Task Detect_NonSocksFirstByte_ReturnsNull()
    {
        var bytes = new byte[] { 0x47 };

        var detected = SocksProtocolDetector.Detect(new ReadOnlySequence<byte>(bytes));

        await Assert.That(detected).IsNull();
    }

    /// <summary>
    ///     Verifies that an empty buffer returns null.
    /// </summary>
    [Test]
    public async Task Detect_EmptyBuffer_ReturnsNull()
    {
        var detected = SocksProtocolDetector.Detect(ReadOnlySequence<byte>.Empty);

        await Assert.That(detected).IsNull();
    }
}
