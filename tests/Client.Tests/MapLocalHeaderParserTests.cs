using Proxyfan.Client.Tools.ViewModels;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="MapLocalHeaderParser" />.
/// </summary>
public sealed class MapLocalHeaderParserTests
{
    /// <summary>
    ///     Empty input parses to an empty list.
    /// </summary>
    [Test]
    public async Task Parse_EmptyInput_ReturnsEmpty()
    {
        var result = MapLocalHeaderParser.Parse(string.Empty);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Whitespace-only input parses to an empty list.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceInput_ReturnsEmpty()
    {
        var result = MapLocalHeaderParser.Parse("   \n   \n");

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Single line with name and value parses correctly.
    /// </summary>
    [Test]
    public async Task Parse_SingleHeader_ReturnsOnePair()
    {
        var result = MapLocalHeaderParser.Parse("Content-Type: application/json");

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Key).IsEqualTo("Content-Type");
        await Assert.That(result[0].Value).IsEqualTo("application/json");
    }

    /// <summary>
    ///     Multiple lines are parsed independently.
    /// </summary>
    [Test]
    public async Task Parse_MultipleLines_ReturnsMultiplePairs()
    {
        var result = MapLocalHeaderParser.Parse("Content-Type: application/json\nX-Trace: abc");

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[1].Key).IsEqualTo("X-Trace");
    }

    /// <summary>
    ///     Lines without a colon are ignored.
    /// </summary>
    [Test]
    public async Task Parse_LineWithoutColon_IsIgnored()
    {
        var result = MapLocalHeaderParser.Parse("not-a-header\nContent-Type: application/json");

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Key).IsEqualTo("Content-Type");
    }

    /// <summary>
    ///     Leading colon (empty name) is ignored.
    /// </summary>
    [Test]
    public async Task Parse_LeadingColon_IsIgnored()
    {
        var result = MapLocalHeaderParser.Parse(": value-without-name");

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Carriage returns and surrounding whitespace are trimmed.
    /// </summary>
    [Test]
    public async Task Parse_CarriageReturnAndWhitespace_IsTrimmed()
    {
        var result = MapLocalHeaderParser.Parse("  Content-Type :  application/json  \r\n");

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Key).IsEqualTo("Content-Type");
        await Assert.That(result[0].Value).IsEqualTo("application/json");
    }

    /// <summary>
    ///     A line whose name portion is only whitespace (e.g. <c>"  : value"</c>) has a non-zero
    ///     separator position but produces an empty name after trimming. The parser skips it
    ///     rather than emitting a header with an empty name.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceOnlyName_IsIgnored()
    {
        var result = MapLocalHeaderParser.Parse("  : value-without-name");

        await Assert.That(result.Count).IsEqualTo(0);
    }
}
