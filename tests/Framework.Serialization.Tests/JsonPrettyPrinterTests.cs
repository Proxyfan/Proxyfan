using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="JsonPrettyPrinter" />.
/// </summary>
public sealed class JsonPrettyPrinterTests
{
    /// <summary>
    ///     Verifies that valid JSON is indented.
    /// </summary>
    [Test]
    public async Task PrettyPrint_ValidJson_IndentsOutput()
    {
        var input = "{\"a\":1,\"b\":2}";

        var output = JsonPrettyPrinter.PrettyPrint(input);

        await Assert.That(output).Contains("\n");
        await Assert.That(output).Contains("\"a\": 1");
    }

    /// <summary>
    ///     Verifies that invalid JSON is returned unchanged.
    /// </summary>
    [Test]
    public async Task PrettyPrint_InvalidJson_ReturnsOriginalText()
    {
        var input = "not json";

        var output = JsonPrettyPrinter.PrettyPrint(input);

        await Assert.That(output).IsEqualTo("not json");
    }

    /// <summary>
    ///     Verifies that an empty string is returned unchanged.
    /// </summary>
    [Test]
    public async Task PrettyPrint_EmptyString_ReturnsEmpty()
    {
        var output = JsonPrettyPrinter.PrettyPrint(string.Empty);

        await Assert.That(output).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that nested JSON is indented properly.
    /// </summary>
    [Test]
    public async Task PrettyPrint_NestedObject_IndentsAllLevels()
    {
        var input = "{\"outer\":{\"inner\":[1,2,3]}}";

        var output = JsonPrettyPrinter.PrettyPrint(input);

        await Assert.That(output).Contains("\"outer\"");
        await Assert.That(output).Contains("\"inner\"");
        await Assert.That(output).Contains("1,");
    }
}
