using System.Threading.Tasks;

namespace Proxyfan.Domain.Configuration.Tests;

/// <summary>
///     Tests for <see cref="KeyValueConfigurationParser" />.
/// </summary>
public sealed class KeyValueConfigurationParserTests
{
    /// <summary>
    ///     Verifies that empty text returns an empty snapshot.
    /// </summary>
    [Test]
    public async Task Parse_EmptyText_ReturnsEmptySnapshot()
    {
        var parseResult = KeyValueConfigurationParser.Parse(string.Empty);

        await Assert.That(parseResult.Snapshot.Count).IsEqualTo(0);
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a basic key=value line is parsed correctly.
    /// </summary>
    [Test]
    public async Task Parse_SingleKeyValueLine_ParsesValue()
    {
        const string text = "proxy.port=8080";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that comment lines starting with # are skipped.
    /// </summary>
    [Test]
    public async Task Parse_CommentLine_IsSkipped()
    {
        const string text = "# this is a comment\nproxy.port=8080";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Count).IsEqualTo(1);
        await Assert.That(parseResult.Snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that empty lines are skipped.
    /// </summary>
    [Test]
    public async Task Parse_EmptyLines_AreSkipped()
    {
        const string text = "\n\nproxy.port=8080\n\n";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Count).IsEqualTo(1);
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that whitespace around keys and values is trimmed.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceAroundEquals_IsTrimmed()
    {
        const string text = "  proxy.port  =  8080  ";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that malformed lines are reported as diagnostics.
    /// </summary>
    [Test]
    public async Task Parse_MalformedLine_ReportsLineNumber()
    {
        const string text = "no-equals-here\nproxy.port=8080";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Count).IsEqualTo(1);
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(1);
        await Assert.That(parseResult.MalformedLineNumbers[0]).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that multiple keys are parsed.
    /// </summary>
    [Test]
    public async Task Parse_MultipleKeyValueLines_ParsesAll()
    {
        const string text = "a=1\nb=2\nc=3";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Count).IsEqualTo(3);
        await Assert.That(parseResult.Snapshot.Get("a", "missing")).IsEqualTo("1");
        await Assert.That(parseResult.Snapshot.Get("b", "missing")).IsEqualTo("2");
        await Assert.That(parseResult.Snapshot.Get("c", "missing")).IsEqualTo("3");
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a value containing an equals sign keeps the trailing portion.
    /// </summary>
    [Test]
    public async Task Parse_ValueWithEquals_KeepsTrailingPortion()
    {
        const string text = "url=http://example.com/path?key=value";

        var parseResult = KeyValueConfigurationParser.Parse(text);

        await Assert.That(parseResult.Snapshot.Get("url", "missing")).IsEqualTo("http://example.com/path?key=value");
        await Assert.That(parseResult.MalformedLineNumbers.Count).IsEqualTo(0);
    }
}
