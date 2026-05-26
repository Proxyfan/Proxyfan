using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="InspectorTextFormatter" />.
/// </summary>
public sealed class InspectorTextFormatterTests
{
    /// <summary>
    ///     Verifies that an empty body returns an empty string.
    /// </summary>
    [Test]
    public async Task FormatBody_EmptyBody_ReturnsEmptyString()
    {
        var body = ReadOnlyMemory<byte>.Empty;

        var result = InspectorTextFormatter.FormatBody(body);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a UTF-8 encoded body is decoded correctly.
    /// </summary>
    [Test]
    public async Task FormatBody_Utf8Body_ReturnsDecodedText()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello world");
        var body = new ReadOnlyMemory<byte>(bytes);

        var result = InspectorTextFormatter.FormatBody(body);

        await Assert.That(result).IsEqualTo("hello world");
    }

    /// <summary>
    ///     Verifies that an empty header collection produces an empty string.
    /// </summary>
    [Test]
    public async Task FormatHeaders_EmptyHeaders_ReturnsEmptyString()
    {
        var headers = HeaderCollection.Empty;

        var result = InspectorTextFormatter.FormatHeaders(headers);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that headers are formatted as name: value lines.
    /// </summary>
    [Test]
    public async Task FormatHeaders_WithHeaders_ReturnsFormattedLines()
    {
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "application/json")
            .Add("X-Trace", "abc123");

        var result = InspectorTextFormatter.FormatHeaders(headers);

        await Assert.That(result).Contains("Content-Type: application/json");
        await Assert.That(result).Contains("X-Trace: abc123");
    }
}