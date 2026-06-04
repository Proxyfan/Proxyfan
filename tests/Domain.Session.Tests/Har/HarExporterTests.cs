using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    ///     Verifies that a flow with a color tag and comment serialises both Proxyfan-specific
    ///     extension fields.
    /// </summary>
    [Test]
    public async Task ExportAsync_FlowWithAnnotations_IncludesColorAndComment()
    {
        var exporter = new HarExporter();
        var flow = CreateCompletedFlow();
        flow.SetColorTag(TrafficFlowColorTag.Red);
        flow.SetComment("Investigate this 500");
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var entry = document.RootElement.GetProperty("log").GetProperty("entries")[0];

        await Assert.That(entry.GetProperty("_proxyfanColorTag").GetString()).IsEqualTo("Red");
        await Assert.That(entry.GetProperty("_proxyfanComment").GetString()).IsEqualTo("Investigate this 500");
    }

    /// <summary>
    ///     Verifies that a flow without annotations omits the optional Proxyfan annotation fields.
    /// </summary>
    [Test]
    public async Task ExportAsync_FlowWithoutAnnotations_OmitsAnnotationFields()
    {
        var exporter = new HarExporter();
        var flow = CreateCompletedFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var entry = document.RootElement.GetProperty("log").GetProperty("entries")[0];

        await Assert.That(entry.TryGetProperty("_proxyfanColorTag", out _)).IsFalse();
        await Assert.That(entry.TryGetProperty("_proxyfanComment", out _)).IsFalse();
    }

    /// <summary>
    ///     Verifies that a binary (non-text) response body is serialised as base64 text with
    ///     the encoding field set, so the body is preserved losslessly.
    /// </summary>
    [Test]
    public async Task ExportAsync_BinaryResponse_WritesBase64TextField()
    {
        var exporter = new HarExporter();
        var flow = CreateBinaryResponseFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var content = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("response").GetProperty("content");

        await Assert.That(content.GetProperty("text").GetString()).IsEqualTo(Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
        await Assert.That(content.GetProperty("encoding").GetString()).IsEqualTo("base64");
        await Assert.That(content.GetProperty("size").GetInt32()).IsEqualTo(4);
    }

    /// <summary>
    ///     Verifies that a response without a Content-Type header serialises an empty mimeType
    ///     string, exercising the null-coalescing branch in WriteContent.
    /// </summary>
    [Test]
    public async Task ExportAsync_ResponseWithoutContentType_EmitsEmptyMimeType()
    {
        var exporter = new HarExporter();
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        flow.SetRequest(new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        }));
        flow.SetResponse(new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        }));
        flow.Complete();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var content = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("response").GetProperty("content");

        await Assert.That(content.GetProperty("mimeType").GetString()).IsEqualTo(string.Empty);
        await Assert.That(content.TryGetProperty("text", out _)).IsTrue();
    }

    /// <summary>
    ///     Verifies that a query-string pair without an "=" sign is serialised with name set
    ///     to the entire token and value set to the empty string, exercising the
    ///     separator-not-found branch in WriteQueryString.
    /// </summary>
    [Test]
    public async Task ExportAsync_QueryStringPairWithoutEquals_EmitsEmptyValue()
    {
        var exporter = new HarExporter();
        var flow = CreateFlowWithQueryString("https://example.com/?flag&lang=en");
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var queryString = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("queryString");

        await Assert.That(queryString.GetArrayLength()).IsEqualTo(2);
        await Assert.That(queryString[0].GetProperty("name").GetString()).IsEqualTo("flag");
        await Assert.That(queryString[0].GetProperty("value").GetString()).IsEqualTo(string.Empty);
        await Assert.That(queryString[1].GetProperty("name").GetString()).IsEqualTo("lang");
        await Assert.That(queryString[1].GetProperty("value").GetString()).IsEqualTo("en");
    }

    /// <summary>
    ///     Verifies that an absolute URL without a query string emits an empty queryString
    ///     array (exercising the empty-query branch in WriteQueryString).
    /// </summary>
    [Test]
    public async Task ExportAsync_AbsoluteUrlWithoutQuery_EmitsEmptyQueryStringArray()
    {
        var exporter = new HarExporter();
        var flow = CreateFlowWithQueryString("https://example.com/path");
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var queryString = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("queryString");

        await Assert.That(queryString.GetArrayLength()).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a cookie pair without an "=" sign is skipped, exercising the
    ///     separator-not-found branch in WriteCookies.
    /// </summary>
    [Test]
    public async Task ExportAsync_CookieWithoutEquals_IsSkipped()
    {
        var exporter = new HarExporter();
        var flow = CreateFlowWithCookie("flag; session=abc123");
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var cookies = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("cookies");

        await Assert.That(cookies.GetArrayLength()).IsEqualTo(1);
        await Assert.That(cookies[0].GetProperty("name").GetString()).IsEqualTo("session");
    }

    /// <summary>
    ///     Verifies that a request without a Cookie header emits an empty cookies array,
    ///     exercising the null-or-whitespace branch in WriteCookies.
    /// </summary>
    [Test]
    public async Task ExportAsync_RequestWithoutCookies_EmitsEmptyCookieArray()
    {
        var exporter = new HarExporter();
        var flow = CreateCompletedFlow();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        using var document = await JsonDocument.ParseAsync(output, cancellationToken: CancellationToken.None);
        var cookies = document.RootElement.GetProperty("log").GetProperty("entries")[0].GetProperty("request").GetProperty("cookies");

        await Assert.That(cookies.GetArrayLength()).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a cancelled token aborts the export before serialization, so callers
    ///     are not blocked on large documents after requesting cancellation.
    /// </summary>
    [Test]
    public async Task ExportAsync_CancelledToken_AbortsBeforeWritingEntries()
    {
        var exporter = new HarExporter();
        var flows = Enumerable.Range(0, 500).Select(_ => CreateCompletedFlow()).ToList();
        using var baseline = new MemoryStream();
        await exporter.ExportAsync(flows, baseline, CancellationToken.None);
        using var output = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(async () => await exporter.ExportAsync(flows, output, cts.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(baseline.Length).IsGreaterThan(10_000);
        await Assert.That(output.Length).IsLessThan(baseline.Length / 100);
    }

    /// <summary>
    ///     Verifies that exporting with gzip compression enabled produces a valid gzip stream
    ///     that can be re-imported and yields the same flows as the original.
    /// </summary>
    [Test]
    public async Task ExportAsync_GzipHarRequested_RoundTripsImportedFlows()
    {
        var flow = CreateCompletedFlow();
        var exporter = new HarExporter(compressWithGzip: true);
        var importer = new HarImporter();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);

        output.Position = 0;
        var firstTwoBytes = new byte[2];
        await output.ReadAsync(firstTwoBytes.AsMemory());
        await Assert.That(firstTwoBytes[0]).IsEqualTo((byte)0x1F);
        await Assert.That(firstTwoBytes[1]).IsEqualTo((byte)0x8B);

        output.Position = 0;
        var importedFlows = await importer.ImportAsync(output, CancellationToken.None);

        await Assert.That(importedFlows.Count).IsEqualTo(1);
        await Assert.That(importedFlows[0].Request!.Method).IsEqualTo("GET");
        await Assert.That(importedFlows[0].Request!.RequestUri.ToString()).IsEqualTo("https://example.com/api/users");
        await Assert.That(importedFlows[0].Response!.StatusCode).IsEqualTo(200);
        await Assert.That(Encoding.UTF8.GetString(importedFlows[0].Response!.Body.Span)).IsEqualTo("hello");
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
