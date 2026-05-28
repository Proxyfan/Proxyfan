using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="TrafficFilter" />.
/// </summary>
public sealed class TrafficFilterTests
{
    /// <summary>
    ///     Verifies that an empty query matches all flows.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Apply_EmptyQuery_ReturnsAllFlows(string query)
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter(query);

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(flows.Length);
    }

    /// <summary>
    ///     Verifies that filtering by URL substring returns only matching flows.
    /// </summary>
    [Test]
    public async Task Apply_UrlSubstring_ReturnsMatching()
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter("api");

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that filtering by HTTP method returns only flows with that method.
    /// </summary>
    [Test]
    public async Task Apply_MethodMatch_ReturnsMatching()
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter("POST");

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that filtering by HTTP status code returns matching flows.
    /// </summary>
    [Test]
    public async Task Apply_StatusCodeMatch_ReturnsMatching()
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter("404");

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that filtering by reason phrase returns matching flows.
    /// </summary>
    [Test]
    public async Task Apply_ReasonPhraseMatch_ReturnsMatching()
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter("Not Found");

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that no match returns an empty list.
    /// </summary>
    [Test]
    public async Task Apply_NoMatch_ReturnsEmpty()
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter("completely-not-present");

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFilter.HasMatch" /> on a flow without a request returns false.
    /// </summary>
    [Test]
    public async Task Matches_FlowWithoutRequestOrResponse_ReturnsFalse()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var filter = new TrafficFilter("anything");

        var matched = filter.HasMatch(flow);

        await Assert.That(matched).IsFalse();
    }

    /// <summary>
    ///     Verifies that filtering by Host header matches.
    /// </summary>
    [Test]
    public async Task Apply_HostHeaderMatch_ReturnsMatching()
    {
        var flows = BuildFlows();
        var filter = new TrafficFilter("api.example.com");

        var result = filter.Apply(flows);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a flow without a Host header but matching URL still matches.
    /// </summary>
    [Test]
    public async Task Apply_RequestWithoutHostHeader_StillMatchesByUrl()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://nohost.example/api"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);

        var filter = new TrafficFilter("nohost");

        await Assert.That(filter.HasMatch(flow)).IsTrue();
    }

    /// <summary>
    ///     Verifies that a response without ReasonPhrase still matches by status code.
    /// </summary>
    [Test]
    public async Task Apply_ResponseWithoutReasonPhrase_StillMatchesByStatusCode()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = string.Empty,
            StatusCode = 500,
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        flow.SetResponse(response);

        var filter = new TrafficFilter("500");

        await Assert.That(filter.HasMatch(flow)).IsTrue();
    }

    /// <summary>
    ///     Verifies that a flow with only a response (no request) returns false (cannot have
    ///     a response without a request being set first).
    /// </summary>
    [Test]
    public async Task Apply_RequestSetButNoMatchingResponseField_ReturnsFalse()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);

        var filter = new TrafficFilter("unmatched-substring-xyz123");

        await Assert.That(filter.HasMatch(flow)).IsFalse();
    }

    /// <summary>
    ///     Verifies that a request with an empty Host header is correctly handled
    ///     (the Host-header branch is taken with an empty/null value).
    /// </summary>
    [Test]
    public async Task Apply_RequestWithEmptyHostHeader_DoesNotThrow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", string.Empty),
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);

        var filter = new TrafficFilter("unmatched-needle-xyz");

        await Assert.That(filter.HasMatch(flow)).IsFalse();
    }

    /// <summary>
    ///     Verifies that the filter matches when the query substring appears in a flow's comment.
    /// </summary>
    [Test]
    public async Task Apply_CommentMatch_ReturnsMatching()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        flow.SetComment("Customer reported issue X-123");

        var filter = new TrafficFilter("x-123");

        await Assert.That(filter.HasMatch(flow)).IsTrue();
    }

    /// <summary>
    ///     Verifies that the filter matches when the query substring appears in a flow's color tag name.
    /// </summary>
    [Test]
    public async Task Apply_ColorTagMatch_ReturnsMatching()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        flow.SetColorTag(TrafficFlowColorTag.Yellow);

        var filter = new TrafficFilter("yellow");

        await Assert.That(filter.HasMatch(flow)).IsTrue();
    }

    /// <summary>
    ///     Verifies that calling HasMatch directly with an empty-query filter returns true
    ///     (covers the true branch of the query-length guard inside HasMatch itself, which
    ///     Apply normally short-circuits before reaching).
    /// </summary>
    [Test]
    public async Task HasMatch_EmptyQuery_ReturnsTrue()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var filter = new TrafficFilter(string.Empty);

        await Assert.That(filter.HasMatch(flow)).IsTrue();
    }

    /// <summary>
    ///     Verifies that a request with a Host header that does not contain the query returns false
    ///     (covers the false branch of hostHeader.Contains).
    /// </summary>
    [Test]
    public async Task HasMatch_HostHeaderPresentButNoMatch_ReturnsFalse()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "irrelevant.example"),
            Method = "GET",
            RequestUri = new Uri("https://irrelevant.example/foo"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);

        var filter = new TrafficFilter("query-not-anywhere-xyz");

        await Assert.That(filter.HasMatch(flow)).IsFalse();
    }

    /// <summary>
    ///     Verifies that a response with a ReasonPhrase that does not contain the query
    ///     returns false (covers the false branch of reasonPhrase.Contains).
    /// </summary>
    [Test]
    public async Task HasMatch_ResponseReasonPhrasePresentButNoMatch_ReturnsFalse()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "Forbidden",
            StatusCode = 403,
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        flow.SetResponse(response);

        var filter = new TrafficFilter("query-not-anywhere-xyz");

        await Assert.That(filter.HasMatch(flow)).IsFalse();
    }

    /// <summary>
    ///     Verifies that the Host-header branch matches when neither the URL nor the
    ///     method contains the query but the Host header does. This is the only path that
    ///     reaches the <c>return true</c> on the Host-header arm of
    ///     <c>HasRequestMatch</c>.
    /// </summary>
    [Test]
    public async Task HasMatch_OnlyHostHeaderContainsQuery_ReturnsTrue()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "needle-host.example"),
            Method = "GET",
            RequestUri = new Uri("https://otherhost.test/path"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);

        var filter = new TrafficFilter("needle-host");

        await Assert.That(filter.HasMatch(flow)).IsTrue();
    }

    private static TrafficFlow[] BuildFlows()
    {
        var flowOne = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var requestOne = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "api.example.com"),
            Method = "GET",
            RequestUri = new Uri("https://api.example.com/users"),
            Version = "HTTP/1.1",
        });
        var responseOne = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        flowOne.SetRequest(requestOne);
        flowOne.SetResponse(responseOne);

        var flowTwo = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var requestTwo = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "static.contoso.com"),
            Method = "POST",
            RequestUri = new Uri("https://static.contoso.com/upload"),
            Version = "HTTP/1.1",
        });
        var responseTwo = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "Not Found",
            StatusCode = 404,
            Version = "HTTP/1.1",
        });
        flowTwo.SetRequest(requestTwo);
        flowTwo.SetResponse(responseTwo);

        return new[] { flowOne, flowTwo };
    }
}
