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
