using Proxyfan.Client.Inspector;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="WebSocketByteSizeFormatter" /> covering the binary unit
///     formatting boundaries (B / KB / MB) used by the WebSocket message list.
/// </summary>
public sealed class WebSocketByteSizeFormatterTests
{
    /// <summary>
    ///     Verifies that small byte counts render as integers with the <c>B</c> suffix.
    /// </summary>
    [Test]
    [Arguments(0, "0 B")]
    [Arguments(1, "1 B")]
    [Arguments(1023, "1023 B")]
    public async Task Format_LessThanOneKilobyte_RendersAsBytes(int byteCount, string expected)
    {
        var result = WebSocketByteSizeFormatter.Format(byteCount);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that byte counts between 1 KB and 1 MB render in kilobytes with one
    ///     decimal place.
    /// </summary>
    [Test]
    [Arguments(1024, "1.0 KB")]
    [Arguments(2048, "2.0 KB")]
    [Arguments(1024 * 1024 - 1, "1024.0 KB")]
    public async Task Format_BetweenKilobyteAndMegabyte_RendersAsKilobytes(int byteCount, string expected)
    {
        var result = WebSocketByteSizeFormatter.Format(byteCount);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that large byte counts render in megabytes with one decimal place.
    /// </summary>
    [Test]
    [Arguments(1024 * 1024, "1.0 MB")]
    [Arguments(3 * 1024 * 1024 + 512 * 1024, "3.5 MB")]
    public async Task Format_OneMegabyteOrMore_RendersAsMegabytes(int byteCount, string expected)
    {
        var result = WebSocketByteSizeFormatter.Format(byteCount);

        await Assert.That(result).IsEqualTo(expected);
    }
}
