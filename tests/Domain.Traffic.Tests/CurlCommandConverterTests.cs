using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="CurlCommandConverter" />.
/// </summary>
public sealed class CurlCommandConverterTests
{
    /// <summary>
    ///     Verifies that a simple GET produces "curl -X GET ...".
    /// </summary>
    [Test]
    public async Task ToCurl_SimpleGet_ProducesExpectedCommand()
    {
        var request = BuildRequest("GET", "https://example.com/api", null);

        var command = CurlCommandConverter.ToCurl(request);

        await Assert.That(command).StartsWith("curl -X GET \"https://example.com/api\"");
    }

    /// <summary>
    ///     Verifies that headers are emitted via -H "name: value".
    /// </summary>
    [Test]
    public async Task ToCurl_WithHeaders_EmitsDashH()
    {
        var headers = HeaderCollection.Empty.Add("Accept", "application/json").Add("X-Custom", "value");
        var request = BuildRequest("GET", "https://example.com/", null, headers);

        var command = CurlCommandConverter.ToCurl(request);

        await Assert.That(command).Contains("-H \"Accept: application/json\"");
        await Assert.That(command).Contains("-H \"X-Custom: value\"");
    }

    /// <summary>
    ///     Verifies that a body is included via --data.
    /// </summary>
    [Test]
    public async Task ToCurl_WithBody_EmitsDashDashData()
    {
        var request = BuildRequest("POST", "https://example.com/", Encoding.UTF8.GetBytes("hello"));

        var command = CurlCommandConverter.ToCurl(request);

        await Assert.That(command).Contains("--data 'hello'");
    }

    /// <summary>
    ///     Verifies that single quotes in the body are properly escaped.
    /// </summary>
    [Test]
    public async Task ToCurl_BodyWithSingleQuote_EscapesIt()
    {
        var request = BuildRequest("POST", "https://example.com/", Encoding.UTF8.GetBytes("it's a test"));

        var command = CurlCommandConverter.ToCurl(request);

        await Assert.That(command).Contains("it'\\''s a test");
    }

    /// <summary>
    ///     Verifies that double quotes in header values are escaped.
    /// </summary>
    [Test]
    public async Task ToCurl_HeaderValueWithDoubleQuote_EscapesIt()
    {
        var headers = HeaderCollection.Empty.Add("X-Echo", "value \"with quotes\"");
        var request = BuildRequest("GET", "https://example.com/", null, headers);

        var command = CurlCommandConverter.ToCurl(request);

        await Assert.That(command).Contains("\\\"with quotes\\\"");
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string method, string url, byte[]? body, HeaderCollection? headers = null)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body ?? Array.Empty<byte>(),
            Headers = headers ?? HeaderCollection.Empty,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
