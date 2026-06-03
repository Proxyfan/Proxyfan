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
        var snapshot = KeyValueConfigurationParser.Parse(string.Empty);

        await Assert.That(snapshot.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a basic key=value line is parsed correctly.
    /// </summary>
    [Test]
    public async Task Parse_SingleKeyValueLine_ParsesValue()
    {
        const string text = "proxy.port=8080";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that comment lines starting with # are skipped.
    /// </summary>
    [Test]
    public async Task Parse_CommentLine_IsSkipped()
    {
        const string text = "# this is a comment\nproxy.port=8080";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that empty lines are skipped.
    /// </summary>
    [Test]
    public async Task Parse_EmptyLines_AreSkipped()
    {
        const string text = "\n\nproxy.port=8080\n\n";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that whitespace around keys and values is trimmed.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceAroundEquals_IsTrimmed()
    {
        const string text = "  proxy.port  =  8080  ";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that lines without an equals sign are skipped.
    /// </summary>
    [Test]
    public async Task Parse_LineWithoutEquals_IsSkipped()
    {
        const string text = "no-equals-here\nproxy.port=8080";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that malformed non-empty lines are surfaced as diagnostics.
    /// </summary>
    [Test]
    public async Task ParseWithDiagnostics_LineWithoutEquals_ReturnsDiagnostic()
    {
        const string text = "no-equals-here\nproxy.port=8080";

        var result = KeyValueConfigurationParser.ParseWithDiagnostics(text);

        await Assert.That(result.Snapshot.Count).IsEqualTo(1);
        await Assert.That(result.Diagnostics.Count).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].LineNumber).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Line).IsEqualTo("no-equals-here");
    }

    /// <summary>
    ///     Verifies that multiple keys are parsed.
    /// </summary>
    [Test]
    public async Task Parse_MultipleKeyValueLines_ParsesAll()
    {
        const string text = "a=1\nb=2\nc=3";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Count).IsEqualTo(3);
        await Assert.That(snapshot.Get("a", "missing")).IsEqualTo("1");
        await Assert.That(snapshot.Get("b", "missing")).IsEqualTo("2");
        await Assert.That(snapshot.Get("c", "missing")).IsEqualTo("3");
    }

    /// <summary>
    ///     Verifies that a value containing an equals sign keeps the trailing portion.
    /// </summary>
    [Test]
    public async Task Parse_ValueWithEquals_KeepsTrailingPortion()
    {
        const string text = "url=http://example.com/path?key=value";

        var snapshot = KeyValueConfigurationParser.Parse(text);

        await Assert.That(snapshot.Get("url", "missing")).IsEqualTo("http://example.com/path?key=value");
    }
}
