using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliSendArgumentParser" />.
/// </summary>
public sealed class CliSendArgumentParserTests
{
    /// <summary>
    ///     Verifies that <c>send --url</c> alone parses with default GET method.
    /// </summary>
    [Test]
    public async Task Parse_UrlOnly_ReturnsRequestWithGetMethod()
    {
        var args = new[] { "send", "--url", "https://example.com" };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Method).IsEqualTo("GET");
        await Assert.That(request.Url).IsEqualTo("https://example.com");
        await Assert.That(request.Headers.Count).IsEqualTo(0);
        await Assert.That(request.Body).IsNull();
    }

    /// <summary>
    ///     Verifies that <c>--method</c> is normalized to uppercase.
    /// </summary>
    [Test]
    public async Task Parse_MethodInLowercase_IsNormalizedToUppercase()
    {
        var args = new[] { "send", "--method", "post", "--url", "https://example.com" };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request!.Method).IsEqualTo("POST");
    }

    /// <summary>
    ///     Verifies that <c>--header</c> with "name: value" form is captured.
    /// </summary>
    [Test]
    public async Task Parse_HeaderWithColon_AddsHeader()
    {
        var args = new[] { "send", "--url", "https://example.com", "--header", "Accept: application/json" };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request!.Headers["Accept"]).IsEqualTo("application/json");
    }

    /// <summary>
    ///     Verifies that multiple <c>--header</c> flags are all captured.
    /// </summary>
    [Test]
    public async Task Parse_MultipleHeaders_AddsAll()
    {
        var args = new[]
        {
            "send", "--url", "https://example.com",
            "--header", "Accept: application/json",
            "--header", "X-Custom: 1",
        };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request!.Headers.Count).IsEqualTo(2);
        await Assert.That(request.Headers["X-Custom"]).IsEqualTo("1");
    }

    /// <summary>
    ///     Verifies that a malformed header (missing colon) is silently dropped.
    /// </summary>
    [Test]
    public async Task Parse_HeaderWithoutColon_IsDropped()
    {
        var args = new[] { "send", "--url", "https://example.com", "--header", "no-colon" };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request!.Headers.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that an absent URL returns null.
    /// </summary>
    [Test]
    public async Task Parse_NoUrl_ReturnsNull()
    {
        var args = new[] { "send", "--method", "GET" };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies that <c>--body</c> is captured.
    /// </summary>
    [Test]
    public async Task Parse_BodyFlag_CapturesBody()
    {
        var args = new[] { "send", "--url", "https://example.com", "--body", "hello" };

        var request = CliSendArgumentParser.Parse(args);

        await Assert.That(request!.Body).IsEqualTo("hello");
    }
}
