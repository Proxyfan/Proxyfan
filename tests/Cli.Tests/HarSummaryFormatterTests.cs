using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Cli.Tests;

/// <summary>
///     Tests for <see cref="HarSummaryFormatter" />.
/// </summary>
public sealed class HarSummaryFormatterTests
{
    /// <summary>
    ///     Verifies that a flow with no request shows the "(no request)" placeholder.
    /// </summary>
    [Test]
    public async Task BuildFlowLine_NoRequest_ShowsPlaceholder()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);

        var line = HarSummaryFormatter.BuildFlowLine(1, flow);

        await Assert.That(line).Contains("(no request)");
        await Assert.That(line).Contains("---");
    }

    /// <summary>
    ///     Verifies that a flow with a request and response shows method, status, and URL.
    /// </summary>
    [Test]
    public async Task BuildFlowLine_RequestAndResponse_ShowsFullLine()
    {
        var flow = BuildFlowWithRequestAndResponse();

        var line = HarSummaryFormatter.BuildFlowLine(5, flow);

        await Assert.That(line).Contains("5.");
        await Assert.That(line).Contains("200");
        await Assert.That(line).Contains("GET");
        await Assert.That(line).Contains("https://example.com/");
    }

    /// <summary>
    ///     Verifies that a flow with a request but no response shows the status placeholder.
    /// </summary>
    [Test]
    public async Task BuildFlowLine_RequestOnly_ShowsStatusPlaceholder()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "POST",
            RequestUri = new Uri("https://api.example.com/data"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));

        var line = HarSummaryFormatter.BuildFlowLine(2, flow);

        await Assert.That(line).Contains("---");
        await Assert.That(line).Contains("POST");
        await Assert.That(line).Contains("https://api.example.com/data");
    }

    private static TrafficFlow BuildFlowWithRequestAndResponse()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        return flow;
    }
}
