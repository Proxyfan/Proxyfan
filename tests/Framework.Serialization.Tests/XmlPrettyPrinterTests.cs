using System.Threading.Tasks;
using Proxyfan.Framework.Serialization;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="XmlPrettyPrinter" />.
/// </summary>
public sealed class XmlPrettyPrinterTests
{
    /// <summary>
    ///     Verifies that null or empty input is returned unchanged.
    /// </summary>
    /// <param name="raw">The raw input string.</param>
    [Test]
    [Arguments("")]
    public async Task PrettyPrint_BlankInput_ReturnsInputUnchanged(string raw)
    {
        var result = XmlPrettyPrinter.PrettyPrint(raw);

        await Assert.That(result).IsEqualTo(raw);
    }

    /// <summary>
    ///     Verifies that malformed XML is returned unchanged.
    /// </summary>
    [Test]
    public async Task PrettyPrint_MalformedXml_ReturnsInputUnchanged()
    {
        var raw = "<not valid";

        var result = XmlPrettyPrinter.PrettyPrint(raw);

        await Assert.That(result).IsEqualTo(raw);
    }

    /// <summary>
    ///     Verifies that nested XML is indented with two-space increments.
    /// </summary>
    [Test]
    public async Task PrettyPrint_NestedElements_IndentsChildren()
    {
        var raw = "<root><child><leaf>value</leaf></child></root>";

        var result = XmlPrettyPrinter.PrettyPrint(raw);

        await Assert.That(result.Contains("  <child>", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("    <leaf>value</leaf>", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that XML attributes are preserved.
    /// </summary>
    [Test]
    public async Task PrettyPrint_AttributedElement_PreservesAttribute()
    {
        var raw = "<root id=\"1\"><child name=\"a\"/></root>";

        var result = XmlPrettyPrinter.PrettyPrint(raw);

        await Assert.That(result.Contains("id=\"1\"", System.StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("name=\"a\"", System.StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that DTD declarations are rejected (treated as malformed).
    /// </summary>
    [Test]
    public async Task PrettyPrint_DocumentTypeDeclaration_ReturnsInputUnchanged()
    {
        var raw = "<!DOCTYPE root SYSTEM \"file:///etc/passwd\"><root/>";

        var result = XmlPrettyPrinter.PrettyPrint(raw);

        await Assert.That(result).IsEqualTo(raw);
    }
}
