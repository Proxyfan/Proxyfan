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
        var result = KeyValueConfigurationParser.Parse(string.Empty);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a basic key=value line is parsed correctly.
    /// </summary>
    [Test]
    public async Task Parse_SingleKeyValueLine_ParsesValue()
    {
        const string text = "proxy.port=8080";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that comment lines starting with # are skipped.
    /// </summary>
    [Test]
    public async Task Parse_CommentLine_IsSkipped()
    {
        const string text = "# this is a comment\nproxy.port=8080";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Count).IsEqualTo(1);
        await Assert.That(result.Value.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that empty lines are skipped.
    /// </summary>
    [Test]
    public async Task Parse_EmptyLines_AreSkipped()
    {
        const string text = "\n\nproxy.port=8080\n\n";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that whitespace around keys and values is trimmed.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceAroundEquals_IsTrimmed()
    {
        const string text = "  proxy.port  =  8080  ";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that a line without an equals sign causes the result to fail and names
    ///     the offending line in the parse error.
    /// </summary>
    [Test]
    public async Task Parse_LineWithoutEquals_ReturnsFailureWithMalformedLine()
    {
        const string text = "no-equals-here\nproxy.port=8080";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsTypeOf<ConfigurationParseError>();
        var parseError = (ConfigurationParseError)result.Error!;
        await Assert.That(parseError.MalformedLines.Count).IsEqualTo(1);
        await Assert.That(parseError.MalformedLines[0]).IsEqualTo("no-equals-here");
    }

    /// <summary>
    ///     Verifies that multiple malformed lines are all reported in the parse error.
    /// </summary>
    [Test]
    public async Task Parse_MultiplemalformedLines_ReportsAllInError()
    {
        const string text = "bad-line-one\nproxy.port=8080\nbad-line-two";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsFalse();
        var parseError = (ConfigurationParseError)result.Error!;
        await Assert.That(parseError.MalformedLines.Count).IsEqualTo(2);
        await Assert.That(parseError.MalformedLines[0]).IsEqualTo("bad-line-one");
        await Assert.That(parseError.MalformedLines[1]).IsEqualTo("bad-line-two");
    }

    /// <summary>
    ///     Verifies that multiple keys are parsed.
    /// </summary>
    [Test]
    public async Task Parse_MultipleKeyValueLines_ParsesAll()
    {
        const string text = "a=1\nb=2\nc=3";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Count).IsEqualTo(3);
        await Assert.That(result.Value.Get("a", "missing")).IsEqualTo("1");
        await Assert.That(result.Value.Get("b", "missing")).IsEqualTo("2");
        await Assert.That(result.Value.Get("c", "missing")).IsEqualTo("3");
    }

    /// <summary>
    ///     Verifies that a value containing an equals sign keeps the trailing portion.
    /// </summary>
    [Test]
    public async Task Parse_ValueWithEquals_KeepsTrailingPortion()
    {
        const string text = "url=http://example.com/path?key=value";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Get("url", "missing")).IsEqualTo("http://example.com/path?key=value");
    }
}
