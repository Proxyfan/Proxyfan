using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="CliSendFormatter" />.
/// </summary>
public sealed class CliSendFormatterTests
{
    /// <summary>
    ///     Verifies that a GET request without headers or body is formatted to the
    ///     "GET /path HTTP/1.1\r\nHost: ...\r\n\r\n" wire format.
    /// </summary>
    [Test]
    public async Task Format_SimpleGet_WritesExpectedRequestLine()
    {
        var request = new CliSendRequest("GET", "https://example.com/api", new Dictionary<string, string>(), null);

        var output = CliSendFormatter.Format(request);

        await Assert.That(output).StartsWith("GET /api HTTP/1.1\r\n");
        await Assert.That(output).Contains("Host: example.com\r\n");
    }

    /// <summary>
    ///     Verifies that a custom port is appended to the Host header.
    /// </summary>
    [Test]
    public async Task Format_CustomPort_AppendsPortToHostHeader()
    {
        var request = new CliSendRequest("GET", "http://example.com:8080/", new Dictionary<string, string>(), null);

        var output = CliSendFormatter.Format(request);

        await Assert.That(output).Contains("Host: example.com:8080\r\n");
    }

    /// <summary>
    ///     Verifies that custom headers are emitted.
    /// </summary>
    [Test]
    public async Task Format_WithCustomHeaders_EmitsHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["X-Test"] = "value",
        };
        var request = new CliSendRequest("POST", "https://api.example.com/", headers, null);

        var output = CliSendFormatter.Format(request);

        await Assert.That(output).Contains("Accept: application/json\r\n");
        await Assert.That(output).Contains("X-Test: value\r\n");
    }

    /// <summary>
    ///     Verifies that a body is appended after the blank line.
    /// </summary>
    [Test]
    public async Task Format_WithBody_AppendsBody()
    {
        var request = new CliSendRequest("POST", "https://example.com/", new Dictionary<string, string>(), "payload");

        var output = CliSendFormatter.Format(request);

        await Assert.That(output).EndsWith("\r\n\r\npayload");
    }
}
