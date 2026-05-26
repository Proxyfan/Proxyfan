using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Tests.Har;

/// <summary>
///     Tests for <see cref="HarExporter" /> verifying HAR 1.2 conformance.
/// </summary>
public sealed class HarExporterTests
{
    /// <summary>
    ///     Verifies that exporting an empty flow list produces a valid HAR document with the
    ///     expected metadata and an empty entries array.
    /// </summary>
    [Test]
    public async Task ExportAsync_NoFlows_ProducesValidHarDocument()
    {
        var exporter = new HarExporter();
        using var output = new MemoryStream();

        await exporter.ExportAsync(Array.Empty<TrafficFlow>(), output, CancellationToken.None);

        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var log = document.RootElement.GetProperty("log");

        await Assert.That(log.GetProperty("version").GetString()).IsEqualTo("1.2");
        await Assert.That(log.GetProperty("creator").GetProperty("name").GetString()).IsEqualTo("Proxyfan");
        await Assert.That(log.GetProperty("entries").GetArrayLength()).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a single completed flow is serialized into one entry with the expected
    ///     method, URL, status code, headers, and body content.
    /// </summary>
    [Test]
    public async Task ExportAsync_SingleCompletedFlow_ProducesEntryWithRequestAndResponse()
    {
        var exporter = new HarExporter();
        var flow = CreateCompletedFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new List<TrafficFlow> { flow }, output, CancellationToken.None);

        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var entries = document.RootElement.GetProperty("log").GetProperty("entries");
        await Assert.That(entries.GetArrayLength()).IsEqualTo(1);

        var entry = entries[0];
        var request = entry.GetProperty("request");
        var response = entry.GetProperty("response");

        await Assert.That(request.GetProperty("method").GetString()).IsEqualTo("GET");
        await Assert.That(request.GetProperty("url").GetString()).IsEqualTo("https://example.com/api/users");
        await Assert.That(response.GetProperty("status").GetInt32()).IsEqualTo(200);
        await Assert.That(response.GetProperty("statusText").GetString()).IsEqualTo("OK");
        await Assert.That(response.GetProperty("content").GetProperty("text").GetString()).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that request headers are serialized with the canonical {name, value} pair form.
    /// </summary>
    [Test]
    public async Task ExportAsync_RequestWithHeaders_SerializesAsNameValuePairs()
    {
        var exporter = new HarExporter();
        var flow = CreateCompletedFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var headers = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("headers");

        await Assert.That(headers.GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(headers[0].GetProperty("name").GetString()).IsEqualTo("Host");
        await Assert.That(headers[0].GetProperty("value").GetString()).IsEqualTo("example.com");
    }

    /// <summary>
    ///     Verifies that query strings are parsed into the queryString array.
    /// </summary>
    [Test]
    public async Task ExportAsync_RequestWithQueryString_SerializesQueryParameters()
    {
        var exporter = new HarExporter();
        var flow = CreateFlowWithQueryString("https://example.com/search?q=hello&lang=en");
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var queryString = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("queryString");

        await Assert.That(queryString.GetArrayLength()).IsEqualTo(2);
        await Assert.That(queryString[0].GetProperty("name").GetString()).IsEqualTo("q");
        await Assert.That(queryString[0].GetProperty("value").GetString()).IsEqualTo("hello");
        await Assert.That(queryString[1].GetProperty("name").GetString()).IsEqualTo("lang");
        await Assert.That(queryString[1].GetProperty("value").GetString()).IsEqualTo("en");
    }

    /// <summary>
    ///     Verifies that cookies are parsed from the Cookie header into the cookies array.
    /// </summary>
    [Test]
    public async Task ExportAsync_RequestWithCookies_SerializesCookies()
    {
        var exporter = new HarExporter();
        var flow = CreateFlowWithCookie("session=abc123; theme=dark");
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var cookies = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("cookies");

        await Assert.That(cookies.GetArrayLength()).IsEqualTo(2);
        await Assert.That(cookies[0].GetProperty("name").GetString()).IsEqualTo("session");
        await Assert.That(cookies[0].GetProperty("value").GetString()).IsEqualTo("abc123");
    }

    /// <summary>
    ///     Verifies that a flow without a request still emits a valid empty-request stub.
    /// </summary>
    [Test]
    public async Task ExportAsync_FlowWithoutRequest_EmitsEmptyRequestStub()
    {
        var exporter = new HarExporter();
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var request = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request");
        var response = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("response");

        await Assert.That(request.GetProperty("method").GetString()).IsEqualTo(string.Empty);
        await Assert.That(response.GetProperty("status").GetInt32()).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the Proxyfan-specific extension fields are included in each entry.
    /// </summary>
    [Test]
    public async Task ExportAsync_EachEntry_IncludesProxyfanExtensionFields()
    {
        var exporter = new HarExporter();
        var flow = CreateCompletedFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var entry = document.RootElement.GetProperty("log").GetProperty("entries")[0];

        await Assert.That(entry.GetProperty("_proxyfanFlowId").GetString()).IsEqualTo(flow.Id.ToString());
        await Assert.That(entry.GetProperty("_proxyfanClientEndPoint").GetString()).IsEqualTo("127.0.0.1:9000");
        await Assert.That(entry.GetProperty("_proxyfanStatus").GetString()).IsEqualTo("Complete");
    }

    /// <summary>
    ///     Verifies that a binary (non-text) response body is omitted from the content.text field.
    /// </summary>
    [Test]
    public async Task ExportAsync_BinaryResponse_OmitsTextField()
    {
        var exporter = new HarExporter();
        var flow = CreateBinaryResponseFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var content = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("response").GetProperty("content");

        await Assert.That(content.TryGetProperty("text", out _)).IsFalse();
        await Assert.That(content.GetProperty("size").GetInt32()).IsEqualTo(4);
    }

    private static TrafficFlow CreateCompletedFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/api/users"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = HeaderCollection.Empty.Add("Content-Type", "text/plain").Add("Content-Length", "5"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();
        return flow;
    }

    private static TrafficFlow CreateFlowWithQueryString(string url)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        return flow;
    }

    private static TrafficFlow CreateFlowWithCookie(string cookieValue)
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty
                .Add("Host", "example.com")
                .Add("Cookie", cookieValue),
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        return flow;
    }

    private static TrafficFlow CreateBinaryResponseFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/image.png"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            Headers = HeaderCollection.Empty.Add("Content-Type", "image/png"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();
        return flow;
    }
}
