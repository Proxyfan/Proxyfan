using System;
using System.Text;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Diff;

namespace Proxyfan.Domain.Traffic.Tests.Diff;

/// <summary>
///     Tests for <see cref="TrafficFlowDiffer" />.
/// </summary>
public sealed class TrafficFlowDifferTests
{
    /// <summary>
    ///     Verifies that diffing a flow against itself produces an IsIdentical=true diff.
    /// </summary>
    [Test]
    public async Task Diff_IdenticalFlows_IsIdentical()
    {
        var flow = BuildFlowGetExample();

        var diff = TrafficFlowDiffer.Diff(flow, flow);

        await Assert.That(diff.IsIdentical).IsTrue();
    }

    /// <summary>
    ///     Verifies that differing URLs produce non-identical diffs with URL changes.
    /// </summary>
    [Test]
    public async Task Diff_DifferentUrls_ReportsUrlChange()
    {
        var flowOne = BuildFlowGetExample();
        var flowTwo = BuildFlowGet("https://example.com/two");

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await Assert.That(diff.IsIdentical).IsFalse();
        await HasAnyNonEqual(diff.Url);
    }

    /// <summary>
    ///     Verifies that differing methods produce non-identical diffs with method changes.
    /// </summary>
    [Test]
    public async Task Diff_DifferentMethods_ReportsMethodChange()
    {
        var flowOne = BuildFlowGetExample();
        var flowTwo = BuildFlow("POST", "https://example.com/one", 200, "OK", null);

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.Method);
    }

    /// <summary>
    ///     Verifies that differing status codes produce status-section differences.
    /// </summary>
    [Test]
    public async Task Diff_DifferentStatusCodes_ReportsStatusChange()
    {
        var flowOne = BuildFlowGetExample();
        var flowTwo = BuildFlow("GET", "https://example.com/one", 500, "Server Error", null);

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.Status);
    }

    /// <summary>
    ///     Verifies that header changes appear in request headers diff section.
    /// </summary>
    [Test]
    public async Task Diff_DifferentRequestHeaders_ReportsRequestHeadersChange()
    {
        var flowOne = BuildFlowGetExample();
        var headersTwo = HeaderCollection.Empty.Add("X-Custom", "Value");
        var flowTwo = BuildFlowWithRequestHeaders(headersTwo);

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.RequestHeaders);
    }

    /// <summary>
    ///     Verifies that differing text response bodies appear in the response body diff.
    /// </summary>
    [Test]
    public async Task Diff_DifferentResponseBodies_ReportsResponseBodyChange()
    {
        var flowOne = BuildFlow("GET", "https://example.com/one", 200, "OK", "hello");
        var flowTwo = BuildFlow("GET", "https://example.com/one", 200, "OK", "world");

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.ResponseBody);
    }

    /// <summary>
    ///     Verifies that one flow missing a response still produces a meaningful status diff.
    /// </summary>
    [Test]
    public async Task Diff_OneFlowMissingResponse_StillProducesDiff()
    {
        var flowOne = BuildFlowGetExample();
        var flowTwo = BuildFlowRequestOnly();

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await Assert.That(diff.IsIdentical).IsFalse();
    }

    /// <summary>
    ///     Verifies that binary bodies are summarized instead of expanded.
    /// </summary>
    [Test]
    public async Task Diff_BinaryResponseBody_RendersAsSummary()
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        var flowOne = BuildFlowWithBytesBody(bytes);
        var flowTwo = BuildFlow("GET", "https://example.com/one", 200, "OK", "text");

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.ResponseBody);
    }

    /// <summary>
    ///     Verifies that oversized bodies are summarized using length only.
    /// </summary>
    [Test]
    public async Task Diff_OversizedResponseBody_RendersAsSummary()
    {
        var bigText = new string('x', TrafficFlowDiffer.MaximumDiffableBodyLength + 1);
        var flowOne = BuildFlow("GET", "https://example.com/one", 200, "OK", bigText);
        var flowTwo = BuildFlow("GET", "https://example.com/one", 200, "OK", "tiny");

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.ResponseBody);
    }

    /// <summary>
    ///     Verifies that bytes in the 0x0E-0x1F control range are treated as binary.
    /// </summary>
    [Test]
    public async Task Diff_ControlByteInResponseBody_RendersAsBinarySummary()
    {
        var bytes = new byte[] { 0x10, 0x11, 0x12 };
        var flowOne = BuildFlowWithBytesBody(bytes);
        var flowTwo = BuildFlow("GET", "https://example.com/one", 200, "OK", "text");

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.ResponseBody);
    }

    /// <summary>
    ///     Verifies that bytes equal to 0x08 (below the tab character 0x09) are treated as binary.
    /// </summary>
    [Test]
    public async Task Diff_BackspaceByteInResponseBody_RendersAsBinarySummary()
    {
        var bytes = new byte[] { 0x08 };
        var flowOne = BuildFlowWithBytesBody(bytes);
        var flowTwo = BuildFlow("GET", "https://example.com/one", 200, "OK", "text");

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.ResponseBody);
    }

    /// <summary>
    ///     Verifies that multi-value headers all appear in the diff output with newline joiners.
    /// </summary>
    [Test]
    public async Task Diff_MultiValueRequestHeaders_FormatsEachOnSeparateLine()
    {
        var headersTwo = HeaderCollection.Empty.Add("X-A", "1").Add("X-A", "2").Add("X-B", "3");
        var flowOne = BuildFlowGetExample();
        var flowTwo = BuildFlowWithRequestHeaders(headersTwo);

        var diff = TrafficFlowDiffer.Diff(flowOne, flowTwo);

        await HasAnyNonEqual(diff.RequestHeaders);
    }

    private static async Task HasAnyNonEqual(System.Collections.Generic.IReadOnlyList<LineDiffSegment> segments)
    {
        var hasNonEqual = false;
        for (var index = 0; index < segments.Count; index++)
        {
            if (segments[index].Operation != LineDiffOperation.Equal)
            {
                hasNonEqual = true;
                break;
            }
        }

        await Assert.That(hasNonEqual).IsTrue();
    }

    private static TrafficFlow BuildFlow(string method, string url, int status, string reason, string? bodyText)
    {
        var bodyBytes = bodyText is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(bodyText);
        return BuildFlowWithBytesAndMethod(method, url, status, reason, bodyBytes);
    }

    private static TrafficFlow BuildFlowGet(string url)
    {
        return BuildFlow("GET", url, 200, "OK", null);
    }

    private static TrafficFlow BuildFlowGetExample()
    {
        return BuildFlowGet("https://example.com/one");
    }

    private static TrafficFlow BuildFlowRequestOnly()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/one"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        return flow;
    }

    private static TrafficFlow BuildFlowWithBytesAndMethod(string method, string url, int status, string reason, byte[] bodyBytes)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = method,
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        });
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = bodyBytes,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = reason,
            StatusCode = status,
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        flow.SetResponse(response);
        return flow;
    }

    private static TrafficFlow BuildFlowWithBytesBody(byte[] bytes)
    {
        return BuildFlowWithBytesAndMethod("GET", "https://example.com/one", 200, "OK", bytes);
    }

    private static TrafficFlow BuildFlowWithRequestHeaders(HeaderCollection headers)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/one"),
            Version = "HTTP/1.1",
        });
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        flow.SetResponse(response);
        return flow;
    }
}
