using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="HarSummaryMarkdownFormatter" />.
/// </summary>
public sealed class HarSummaryMarkdownFormatterTests
{
    /// <summary>
    ///     Verifies the table header and separator are always emitted.
    /// </summary>
    [Test]
    public async Task Format_EmptyList_EmitsHeaderOnly()
    {
        var result = HarSummaryMarkdownFormatter.Format(Array.Empty<TrafficFlow>());

        await Assert.That(result.Contains("| # | Status | Method | URL |", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("|---|--------|--------|-----|", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies a single flow renders status, method and URL.
    /// </summary>
    [Test]
    public async Task Format_SingleFlow_RendersStatusMethodUrl()
    {
        var flow = BuildFlow("GET", "https://example.com/", 200);
        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        await Assert.That(result.Contains("| 1 | 200 | GET | `https://example.com/` |", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies URLs containing backticks are escaped.
    /// </summary>
    [Test]
    public async Task Format_UrlWithBacktick_EscapesIt()
    {
        var flow = BuildFlow("GET", "https://example.com/path%60backtick", 200);
        var url = flow.Request!.RequestUri.ToString();
        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        if (url.Contains('`', StringComparison.Ordinal))
        {
            await Assert.That(result.Contains("\\`", StringComparison.Ordinal)).IsTrue();
        }
        else
        {
            await Assert.That(result.Contains(url, StringComparison.Ordinal)).IsTrue();
        }
    }

    /// <summary>
    ///     Verifies multiple flows render sequential indices.
    /// </summary>
    [Test]
    public async Task Format_MultipleFlows_IndexesSequentially()
    {
        var first = BuildFlow("GET", "https://a.example.com/", 200);
        var second = BuildFlow("POST", "https://b.example.com/", 201);
        var result = HarSummaryMarkdownFormatter.Format(new[] { first, second });

        await Assert.That(result.Contains("| 1 |", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("| 2 |", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("POST", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("201", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies flows without responses render dashes.
    /// </summary>
    [Test]
    public async Task Format_FlowWithoutResponse_RendersDashes()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));

        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        await Assert.That(result.Contains("| --- |", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies flows without a request render placeholders for method, URL and status.
    /// </summary>
    [Test]
    public async Task Format_FlowWithoutRequest_RendersPlaceholders()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);

        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        await Assert.That(result.Contains("(no request)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("| - |", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies pipe characters in the request method are escaped so they cannot split the table cell.
    /// </summary>
    [Test]
    public async Task Format_MethodWithPipe_EscapesPipe()
    {
        var flow = BuildFlow("GE|T", "https://example.com/", 200);
        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        await Assert.That(result.Contains("GE\\|T", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("| GE|T |", StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>
    ///     Verifies newlines in the request method are normalized so they cannot break the table row.
    /// </summary>
    [Test]
    public async Task Format_MethodWithNewline_NormalizesNewline()
    {
        var flow = BuildFlow("GE\r\nT", "https://example.com/", 200);
        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        await Assert.That(result.Contains("GE T", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("GE\r\nT", StringComparison.Ordinal)).IsFalse();
        await Assert.That(result.Contains("GE\nT", StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>
    ///     Verifies pipe characters in the URL are escaped even inside the inline-code wrapper.
    /// </summary>
    [Test]
    public async Task Format_UrlWithPipe_EscapesPipe()
    {
        var flow = BuildFlow("GET", "https://example.com/?a=1%7C2", 200);
        var url = flow.Request!.RequestUri.ToString();
        var result = HarSummaryMarkdownFormatter.Format(new[] { flow });

        if (url.Contains('|', StringComparison.Ordinal))
        {
            await Assert.That(result.Contains("\\|", StringComparison.Ordinal)).IsTrue();
        }
        else
        {
            await Assert.That(result.Contains(url, StringComparison.Ordinal)).IsTrue();
        }
    }

    private static TrafficFlow BuildFlow(string method, string url, int status)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            StatusCode = status,
            ReasonPhrase = "OK",
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();
        return flow;
    }
}
