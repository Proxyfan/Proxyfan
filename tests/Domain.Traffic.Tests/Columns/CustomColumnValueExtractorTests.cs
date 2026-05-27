using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Columns;

namespace Proxyfan.Domain.Traffic.Tests.Columns;

/// <summary>
///     Tests for <see cref="CustomColumnValueExtractor" />.
/// </summary>
public sealed class CustomColumnValueExtractorTests
{
    /// <summary>
    ///     Verifies extracting a request header that exists returns its value.
    /// </summary>
    [Test]
    public async Task Extract_RequestHeaderPresent_ReturnsValue()
    {
        var flow = BuildFlowWithRequestHeader("X-Trace", "abc-123");
        var column = BuildColumn(CustomColumnSource.Request, "X-Trace");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo("abc-123");
    }

    /// <summary>
    ///     Verifies extracting a request header is case-insensitive.
    /// </summary>
    [Test]
    public async Task Extract_RequestHeaderCaseInsensitive_ReturnsValue()
    {
        var flow = BuildFlowWithRequestHeader("Content-Type", "application/json");
        var column = BuildColumn(CustomColumnSource.Request, "content-type");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo("application/json");
    }

    /// <summary>
    ///     Verifies extracting from a response header returns its value.
    /// </summary>
    [Test]
    public async Task Extract_ResponseHeaderPresent_ReturnsValue()
    {
        var flow = BuildFlowWithResponseHeader("X-Request-Id", "req-001");
        var column = BuildColumn(CustomColumnSource.Response, "X-Request-Id");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo("req-001");
    }

    /// <summary>
    ///     Verifies a missing request returns empty string.
    /// </summary>
    [Test]
    public async Task Extract_RequestMissing_ReturnsEmpty()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var column = BuildColumn(CustomColumnSource.Request, "Anything");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies a missing response returns empty string.
    /// </summary>
    [Test]
    public async Task Extract_ResponseMissing_ReturnsEmpty()
    {
        var flow = BuildFlowWithRequestHeader("X-Trace", "abc-123");
        var column = BuildColumn(CustomColumnSource.Response, "X-Trace");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies a missing header key returns empty string.
    /// </summary>
    [Test]
    public async Task Extract_HeaderKeyMissing_ReturnsEmpty()
    {
        var flow = BuildFlowWithRequestHeader("Foo", "bar");
        var column = BuildColumn(CustomColumnSource.Request, "Other-Key");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies multiple values are joined with ", ".
    /// </summary>
    [Test]
    public async Task Extract_MultipleValues_JoinedWithCommaAndSpace()
    {
        var headers = HeaderCollection.Empty.Add("Set-Cookie", "a=1").Add("Set-Cookie", "b=2");
        var flow = BuildFlowWithResponseHeaders(headers);
        var column = BuildColumn(CustomColumnSource.Response, "Set-Cookie");

        var value = CustomColumnValueExtractor.Extract(column, flow);

        await Assert.That(value).IsEqualTo("a=1, b=2");
    }

    private static CustomColumnDefinition BuildColumn(CustomColumnSource source, string headerKey)
    {
        return new CustomColumnDefinition
        {
            DisplayName = headerKey,
            HeaderKey = headerKey,
            Id = Guid.NewGuid(),
            Source = source,
        };
    }

    private static TrafficFlow BuildFlowWithRequestHeader(string key, string value)
    {
        var headers = HeaderCollection.Empty.Add(key, value);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        return flow;
    }

    private static TrafficFlow BuildFlowWithResponseHeader(string key, string value)
    {
        var headers = HeaderCollection.Empty.Add(key, value);
        return BuildFlowWithResponseHeaders(headers);
    }

    private static TrafficFlow BuildFlowWithResponseHeaders(HeaderCollection headers)
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
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        flow.SetRequest(request);
        flow.SetResponse(response);
        return flow;
    }
}
