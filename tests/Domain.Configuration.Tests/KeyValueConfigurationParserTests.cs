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

        await Assert.That(result.Snapshot.Count).IsEqualTo(0);
        await Assert.That(result.HasMalformedLines).IsFalse();
    }

    /// <summary>
    ///     Verifies that a basic key=value line is parsed correctly.
    /// </summary>
    [Test]
    public async Task Parse_SingleKeyValueLine_ParsesValue()
    {
        const string text = "proxy.port=8080";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
        await Assert.That(result.HasMalformedLines).IsFalse();
    }

    /// <summary>
    ///     Verifies that comment lines starting with # are skipped.
    /// </summary>
    [Test]
    public async Task Parse_CommentLine_IsSkipped()
    {
        const string text = "# this is a comment\nproxy.port=8080";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Count).IsEqualTo(1);
        await Assert.That(result.Snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
        await Assert.That(result.HasMalformedLines).IsFalse();
    }

    /// <summary>
    ///     Verifies that empty lines are skipped.
    /// </summary>
    [Test]
    public async Task Parse_EmptyLines_AreSkipped()
    {
        const string text = "\n\nproxy.port=8080\n\n";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Count).IsEqualTo(1);
        await Assert.That(result.HasMalformedLines).IsFalse();
    }

    /// <summary>
    ///     Verifies that whitespace around keys and values is trimmed.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceAroundEquals_IsTrimmed()
    {
        const string text = "  proxy.port  =  8080  ";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
        await Assert.That(result.HasMalformedLines).IsFalse();
    }

    /// <summary>
    ///     Verifies that a line without an equals sign is reported as a malformed-line
    ///     diagnostic rather than silently discarded.
    /// </summary>
    [Test]
    public async Task Parse_LineWithoutEquals_ReportsDiagnostic()
    {
        const string text = "no-equals-here\nproxy.port=8080";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Count).IsEqualTo(1);
        await Assert.That(result.HasMalformedLines).IsTrue();
        await Assert.That(result.MalformedLines.Count).IsEqualTo(1);
        await Assert.That(result.MalformedLines[0].LineNumber).IsEqualTo(1);
        await Assert.That(result.MalformedLines[0].LineContent).IsEqualTo("no-equals-here");
    }

    /// <summary>
    ///     Verifies that multiple malformed lines are each reported with correct line
    ///     numbers when mixed with valid and comment lines.
    /// </summary>
    [Test]
    public async Task Parse_MultipleMalformedLines_ReportsAllDiagnosticsWithCorrectLineNumbers()
    {
        const string text = "bad-line\n# comment\nproxy.port=8080\nanother-bad-line";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Count).IsEqualTo(1);
        await Assert.That(result.MalformedLines.Count).IsEqualTo(2);
        await Assert.That(result.MalformedLines[0].LineNumber).IsEqualTo(1);
        await Assert.That(result.MalformedLines[0].LineContent).IsEqualTo("bad-line");
        await Assert.That(result.MalformedLines[1].LineNumber).IsEqualTo(4);
        await Assert.That(result.MalformedLines[1].LineContent).IsEqualTo("another-bad-line");
    }

    /// <summary>
    ///     Verifies that multiple keys are parsed.
    /// </summary>
    [Test]
    public async Task Parse_MultipleKeyValueLines_ParsesAll()
    {
        const string text = "a=1\nb=2\nc=3";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Count).IsEqualTo(3);
        await Assert.That(result.Snapshot.Get("a", "missing")).IsEqualTo("1");
        await Assert.That(result.Snapshot.Get("b", "missing")).IsEqualTo("2");
        await Assert.That(result.Snapshot.Get("c", "missing")).IsEqualTo("3");
        await Assert.That(result.HasMalformedLines).IsFalse();
    }

    /// <summary>
    ///     Verifies that a value containing an equals sign keeps the trailing portion.
    /// </summary>
    [Test]
    public async Task Parse_ValueWithEquals_KeepsTrailingPortion()
    {
        const string text = "url=http://example.com/path?key=value";

        var result = KeyValueConfigurationParser.Parse(text);

        await Assert.That(result.Snapshot.Get("url", "missing")).IsEqualTo("http://example.com/path?key=value");
        await Assert.That(result.HasMalformedLines).IsFalse();
    }
}
