using Proxyfan.Domain.Session.Har;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="HarSummaryRenderer" />.
/// </summary>
public sealed class HarSummaryRendererTests
{
    /// <summary>
    ///     Verifies that an empty HAR file produces a "0 flow(s)" header.
    /// </summary>
    [Test]
    public async Task RenderAsync_EmptyHar_WritesZeroFlows()
    {
        const string harJson = "{\"log\":{\"version\":\"1.2\",\"creator\":{\"name\":\"Test\",\"version\":\"1\"},\"entries\":[]}}";
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));
        using var writer = new StringWriter();
        var renderer = new HarSummaryRenderer(new HarImporter());

        await renderer.RenderAsync(input, writer, CancellationToken.None);
        var text = writer.ToString();

        await Assert.That(text).Contains("0 flow(s)");
    }

    /// <summary>
    ///     Verifies that a HAR file with a single entry produces a single line summary with
    ///     the method, status, and URL.
    /// </summary>
    [Test]
    public async Task RenderAsync_SingleEntry_WritesFlowLine()
    {
        const string harJson = """
            {
              "log": {
                "version": "1.2",
                "creator": { "name": "Test", "version": "1" },
                "entries": [
                  {
                    "startedDateTime": "2025-01-01T00:00:00Z",
                    "time": 100,
                    "request": {
                      "method": "GET",
                      "url": "http://example.com/data",
                      "httpVersion": "HTTP/1.1",
                      "headers": [],
                      "queryString": [],
                      "cookies": [],
                      "headersSize": -1,
                      "bodySize": 0
                    },
                    "response": {
                      "status": 200,
                      "statusText": "OK",
                      "httpVersion": "HTTP/1.1",
                      "headers": [],
                      "cookies": [],
                      "content": { "size": 0, "mimeType": "text/plain", "text": "" },
                      "redirectURL": "",
                      "headersSize": -1,
                      "bodySize": 0
                    },
                    "cache": {},
                    "timings": { "send": 0, "wait": 0, "receive": 0 }
                  }
                ]
              }
            }
            """;
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));
        using var writer = new StringWriter();
        var renderer = new HarSummaryRenderer(new HarImporter());

        await renderer.RenderAsync(input, writer, CancellationToken.None);
        var text = writer.ToString();

        await Assert.That(text).Contains("1 flow(s)");
        await Assert.That(text).Contains("200");
        await Assert.That(text).Contains("GET");
        await Assert.That(text).Contains("http://example.com/data");
    }
}
