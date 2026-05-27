using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="FlowSummaryFormatter" />.
/// </summary>
public sealed class FlowSummaryFormatterTests
{
    /// <summary>
    ///     Verifies that a null flow produces an empty string.
    /// </summary>
    [Test]
    public async Task Format_NullFlow_ReturnsEmpty()
    {
        var result = FlowSummaryFormatter.Format(null!);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that flow metadata is always present.
    /// </summary>
    [Test]
    public async Task Format_PendingFlow_IncludesMetadata()
    {
        var flow = CreatePendingFlow();

        var result = FlowSummaryFormatter.Format(flow);

        await Assert.That(result.Contains("Flow Id:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Status: Pending", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("127.0.0.1:1", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that request data is rendered.
    /// </summary>
    [Test]
    public async Task Format_FlowWithRequest_IncludesRequestSection()
    {
        var flow = CreatePendingFlow();
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/json");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);
        flow.SetRequest(request);

        var result = FlowSummaryFormatter.Format(flow);

        await Assert.That(result.Contains("Request", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Method: GET", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("https://example.com/api", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Content-Type: application/json", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Body bytes: 0", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that response data is rendered including content encoding.
    /// </summary>
    [Test]
    public async Task Format_FlowWithResponse_IncludesResponseSection()
    {
        var flow = CreatePendingFlow();
        var requestHeaders = HeaderCollection.Empty;
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);
        flow.SetRequest(request);

        var responseHeaders = HeaderCollection.Empty
            .Add("Content-Type", "text/html")
            .Add("Content-Encoding", "gzip");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = new byte[] { 1, 2, 3 },
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        flow.SetResponse(response);
        flow.Complete();

        var result = FlowSummaryFormatter.Format(flow);

        await Assert.That(result.Contains("Status: 200 OK", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Content-Type: text/html", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Content-Encoding: gzip", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Body bytes: 3", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Total duration:", StringComparison.Ordinal)).IsTrue();
    }

    private static TrafficFlow CreatePendingFlow()
    {
        var startedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1", startedAt);
    }
}
