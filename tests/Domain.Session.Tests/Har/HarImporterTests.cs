using Proxyfan.Domain.Session.Har;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Tests.Har;

/// <summary>
///     Tests for <see cref="HarImporter" /> including round-trip with <see cref="HarExporter" />.
/// </summary>
public sealed class HarImporterTests
{
    /// <summary>
    ///     Verifies that an empty HAR document returns no flows.
    /// </summary>
    [Test]
    public async Task ImportAsync_EmptyEntries_ReturnsEmptyList()
    {
        const string harJson = "{\"log\":{\"version\":\"1.2\",\"creator\":{\"name\":\"Test\",\"version\":\"1\"},\"entries\":[]}}";
        var importer = new HarImporter();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));

        var flows = await importer.ImportAsync(input, CancellationToken.None);

        await Assert.That(flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a HAR document with no log object returns an empty list.
    /// </summary>
    [Test]
    public async Task ImportAsync_NoLogObject_ReturnsEmptyList()
    {
        const string harJson = "{}";
        var importer = new HarImporter();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));

        var flows = await importer.ImportAsync(input, CancellationToken.None);

        await Assert.That(flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a HAR document with a log object but no entries returns an empty list.
    /// </summary>
    [Test]
    public async Task ImportAsync_LogWithoutEntries_ReturnsEmptyList()
    {
        const string harJson = "{\"log\":{\"version\":\"1.2\"}}";
        var importer = new HarImporter();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));

        var flows = await importer.ImportAsync(input, CancellationToken.None);

        await Assert.That(flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies the round-trip: export a flow, import the bytes, and the imported flow matches
    ///     the original on all key fields.
    /// </summary>
    [Test]
    public async Task ImportAsync_RoundTrip_PreservesFlowFields()
    {
        var originalFlow = CreateCompletedFlow();
        var exporter = new HarExporter();
        var importer = new HarImporter();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { originalFlow }, output, CancellationToken.None);
        output.Position = 0;
        var importedFlows = await importer.ImportAsync(output, CancellationToken.None);

        await Assert.That(importedFlows.Count).IsEqualTo(1);
        var imported = importedFlows[0];
        await Assert.That(imported.Id).IsEqualTo(originalFlow.Id);
        await Assert.That(imported.ClientEndPoint).IsEqualTo(originalFlow.ClientEndPoint);
        await Assert.That(imported.Request!.Method).IsEqualTo("GET");
        await Assert.That(imported.Request!.RequestUri.ToString()).IsEqualTo("https://example.com/api");
        await Assert.That(imported.Response!.StatusCode).IsEqualTo(200);
        await Assert.That(imported.Response!.ReasonPhrase).IsEqualTo("OK");
    }

    /// <summary>
    ///     Verifies that text-content response bodies are preserved through round-trip.
    /// </summary>
    [Test]
    public async Task ImportAsync_RoundTrip_PreservesTextResponseBody()
    {
        var originalFlow = CreateCompletedFlow();
        var exporter = new HarExporter();
        var importer = new HarImporter();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { originalFlow }, output, CancellationToken.None);
        output.Position = 0;
        var importedFlows = await importer.ImportAsync(output, CancellationToken.None);
        var bodyText = Encoding.UTF8.GetString(importedFlows[0].Response!.Body.Span);

        await Assert.That(bodyText).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that color tags and comments are preserved through a HAR round-trip.
    /// </summary>
    [Test]
    public async Task ImportAsync_RoundTrip_PreservesAnnotations()
    {
        var originalFlow = CreateCompletedFlow();
        originalFlow.SetColorTag(TrafficFlowColorTag.Blue);
        originalFlow.SetComment("Annotated by tester");
        var exporter = new HarExporter();
        var importer = new HarImporter();
        using var output = new MemoryStream();

        await exporter.ExportAsync(new[] { originalFlow }, output, CancellationToken.None);
        output.Position = 0;
        var importedFlows = await importer.ImportAsync(output, CancellationToken.None);

        await Assert.That(importedFlows[0].ColorTag).IsEqualTo(TrafficFlowColorTag.Blue);
        await Assert.That(importedFlows[0].Comment).IsEqualTo("Annotated by tester");
    }

    /// <summary>
    ///     Verifies that an entry with no request property is gracefully tolerated.
    /// </summary>
    [Test]
    public async Task ImportAsync_EntryWithoutRequest_StillReturnsFlow()
    {
        const string harJson = """
            {"log":{"version":"1.2","entries":[
              {"startedDateTime":"2025-01-01T00:00:00Z","time":0}
            ]}}
            """;
        var importer = new HarImporter();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));

        var flows = await importer.ImportAsync(input, CancellationToken.None);

        await Assert.That(flows.Count).IsEqualTo(1);
        await Assert.That(flows[0].Request).IsNull();
    }

    /// <summary>
    ///     Verifies that an entry with invalid request method/url is skipped (request remains null).
    /// </summary>
    [Test]
    public async Task ImportAsync_RequestMissingMethod_LeavesRequestNull()
    {
        const string harJson = """
            {"log":{"version":"1.2","entries":[
              {"startedDateTime":"2025-01-01T00:00:00Z","time":0,"request":{"url":"https://example.com/"}}
            ]}}
            """;
        var importer = new HarImporter();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));

        var flows = await importer.ImportAsync(input, CancellationToken.None);

        await Assert.That(flows[0].Request).IsNull();
    }

    /// <summary>
    ///     Verifies that an entry with a status=0 response is treated as null (per the empty-response stub format).
    /// </summary>
    [Test]
    public async Task ImportAsync_ZeroStatusResponse_LeavesResponseNull()
    {
        const string harJson = """
            {"log":{"version":"1.2","entries":[
              {"startedDateTime":"2025-01-01T00:00:00Z","time":0,
               "request":{"method":"GET","url":"https://example.com/","httpVersion":"HTTP/1.1"},
               "response":{"status":0,"statusText":""}}
            ]}}
            """;
        var importer = new HarImporter();
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(harJson));

        var flows = await importer.ImportAsync(input, CancellationToken.None);

        await Assert.That(flows[0].Response).IsNull();
    }

    /// <summary>
    ///     Verifies that a text request body and a text response body are both preserved
    ///     through a full export/import round-trip.
    /// </summary>
    [Test]
    public async Task RoundTrip_TextRequestAndResponse_PreservesBodies()
    {
        var requestBody = Encoding.UTF8.GetBytes("name=value&other=data");
        var responseBody = Encoding.UTF8.GetBytes("{\"ok\":true}");
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = requestBody,
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/x-www-form-urlencoded"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/submit"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = responseBody,
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/json"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();

        var exporter = new HarExporter();
        var importer = new HarImporter();
        using var output = new MemoryStream();
        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        var imported = (await importer.ImportAsync(output, CancellationToken.None))[0];

        await Assert.That(imported.Request!.Body.ToArray()).IsEquivalentTo(requestBody);
        await Assert.That(imported.Response!.Body.ToArray()).IsEquivalentTo(responseBody);
    }

    /// <summary>
    ///     Verifies that a binary response body is preserved through a full export/import
    ///     round-trip via base64 encoding.
    /// </summary>
    [Test]
    public async Task RoundTrip_BinaryResponseBody_PreservesViaBase64()
    {
        var binaryBody = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/image.png"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = binaryBody,
            Headers = HeaderCollection.Empty.Add("Content-Type", "image/png"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();

        var exporter = new HarExporter();
        var importer = new HarImporter();
        using var output = new MemoryStream();
        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        var imported = (await importer.ImportAsync(output, CancellationToken.None))[0];

        await Assert.That(imported.Response!.Body.ToArray()).IsEquivalentTo(binaryBody);
    }

    /// <summary>
    ///     Verifies that a binary request body is preserved through a full export/import
    ///     round-trip via base64 encoding.
    /// </summary>
    [Test]
    public async Task RoundTrip_PostDataWithFormParams_PreservesRequestBody()
    {
        var binaryBody = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE };
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = binaryBody,
            Headers = HeaderCollection.Empty.Add("Content-Type", "application/octet-stream"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/upload"),
            Version = "HTTP/1.1",
        };
        flow.SetRequest(new HypertextTransferProtocolRequestData(requestParameters));
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "No Content",
            StatusCode = 204,
            Version = "HTTP/1.1",
        };
        flow.SetResponse(new HypertextTransferProtocolResponseData(responseParameters));
        flow.Complete();

        var exporter = new HarExporter();
        var importer = new HarImporter();
        using var output = new MemoryStream();
        await exporter.ExportAsync(new[] { flow }, output, CancellationToken.None);
        output.Position = 0;
        var imported = (await importer.ImportAsync(output, CancellationToken.None))[0];

        await Assert.That(imported.Request!.Body.ToArray()).IsEquivalentTo(binaryBody);
    }

    private static TrafficFlow CreateCompletedFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/api"),
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
}
