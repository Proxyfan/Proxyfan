using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="Socks5GreetingParser" />.
/// </summary>
public sealed class Socks5GreetingParserTests
{
    /// <summary>
    ///     Verifies that a complete greeting with one method (NoAuth) parses.
    /// </summary>
    [Test]
    public async Task TryParse_OneMethod_ParsesGreeting()
    {
        var bytes = new byte[] { 0x05, 0x01, 0x00 };

        var greeting = Socks5GreetingParser.TryParse(bytes);

        await Assert.That(greeting).IsNotNull();
        await Assert.That(greeting!.Methods.Count).IsEqualTo(1);
        await Assert.That(greeting.Methods[0]).IsEqualTo((byte)0x00);
        await Assert.That(greeting.TotalLength).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that a complete greeting with multiple methods captures all.
    /// </summary>
    [Test]
    public async Task TryParse_ThreeMethods_CapturesAll()
    {
        var bytes = new byte[] { 0x05, 0x03, 0x00, 0x02, 0x09 };

        var greeting = Socks5GreetingParser.TryParse(bytes);

        await Assert.That(greeting!.Methods.Count).IsEqualTo(3);
        await Assert.That(greeting.Methods[1]).IsEqualTo((byte)0x02);
    }

    /// <summary>
    ///     Verifies that a buffer truncated mid-methods returns null.
    /// </summary>
    [Test]
    public async Task TryParse_MethodsTruncated_ReturnsNull()
    {
        var bytes = new byte[] { 0x05, 0x03, 0x00 };

        var greeting = Socks5GreetingParser.TryParse(bytes);

        await Assert.That(greeting).IsNull();
    }

    /// <summary>
    ///     Verifies that a buffer with fewer than two bytes returns null.
    /// </summary>
    [Test]
    public async Task TryParse_OneByteBuffer_ReturnsNull()
    {
        var bytes = new byte[] { 0x05 };

        var greeting = Socks5GreetingParser.TryParse(bytes);

        await Assert.That(greeting).IsNull();
    }

    /// <summary>
    ///     Verifies that a non-SOCKS5 first byte throws.
    /// </summary>
    [Test]
    public async Task TryParse_WrongVersion_Throws()
    {
        var bytes = new byte[] { 0x04, 0x01, 0x00 };

        await Assert.That(() => Socks5GreetingParser.TryParse(bytes)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a zero method count throws.
    /// </summary>
    [Test]
    public async Task TryParse_ZeroMethodCount_Throws()
    {
        var bytes = new byte[] { 0x05, 0x00 };

        await Assert.That(() => Socks5GreetingParser.TryParse(bytes)).Throws<InvalidDataException>();
    }
}
