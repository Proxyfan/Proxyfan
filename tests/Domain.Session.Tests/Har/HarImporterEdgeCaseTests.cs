using Proxyfan.Domain.Session.Har;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Tests.Har;

/// <summary>
///     Edge-case tests for <see cref="HarImporter" /> targeting branch coverage gaps in
///     <see cref="HarEntryParser" /> private helpers.
/// </summary>
public sealed class HarImporterEdgeCaseTests
{
    /// <summary>
    ///     Verifies that an entry with no startedDateTime is still importable (uses fallback).
    /// </summary>
    [Test]
    public async Task ImportAsync_EntryWithoutStartedDateTime_StillImports()
    {
        const string harJson = """
            {"log":{"version":"1.2","creator":{"name":"T","version":"1"},"entries":[
                {"request":{"method":"GET","url":"https://example.com/","httpVersion":"HTTP/1.1","headers":[]},
                 "response":{"status":200,"statusText":"OK","httpVersion":"HTTP/1.1","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an entry whose request lacks "method" produces no request.
    /// </summary>
    [Test]
    public async Task ImportAsync_RequestWithoutMethod_OmitsRequest()
    {
        const string harJson = """
            {"log":{"entries":[
                {"startedDateTime":"2025-01-01T00:00:00Z","request":{"url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows.Count).IsEqualTo(1);
        await Assert.That(flows[0].Request).IsNull();
    }

    /// <summary>
    ///     Verifies that an entry with invalid URI produces no request.
    /// </summary>
    [Test]
    public async Task ImportAsync_RequestWithBlankMethod_OmitsRequest()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"","url":"https://example.com/","headers":[]}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request).IsNull();
    }

    /// <summary>
    ///     Verifies that an entry with a response missing "status" is treated as no response.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithoutStatus_OmitsResponse()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"statusText":"OK"}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response).IsNull();
    }

    /// <summary>
    ///     Verifies that an entry with non-numeric status omits the response.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithStringStatus_OmitsResponse()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":"200","statusText":"OK"}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response).IsNull();
    }

    /// <summary>
    ///     Verifies that headers with missing "name" properties are skipped.
    /// </summary>
    [Test]
    public async Task ImportAsync_HeaderWithoutName_IsSkipped()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[{"value":"v"},{"name":"X","value":"y"}]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Headers.HasHeader("X")).IsTrue();
        await Assert.That(flows[0].Request!.Headers.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that headers with blank names are skipped.
    /// </summary>
    [Test]
    public async Task ImportAsync_HeaderWithBlankName_IsSkipped()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[{"name":"","value":"v"},{"name":"X","value":"y"}]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Headers.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a custom _proxyfanFlowId GUID is preserved.
    /// </summary>
    [Test]
    public async Task ImportAsync_WithCustomFlowId_UsesIt()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanFlowId":"11111111-2222-3333-4444-555555555555",
                 "_proxyfanClientEndPoint":"10.0.0.1",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Id.ToString()).IsEqualTo("11111111-2222-3333-4444-555555555555");
        await Assert.That(flows[0].ClientEndPoint).IsEqualTo("10.0.0.1");
    }

    /// <summary>
    ///     Verifies that response content with non-string text omits the body.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithNonStringText_OmitsBody()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{"text":12345}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response!.Body.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that response with empty text yields an empty body.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithEmptyText_HasEmptyBody()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{"text":""}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response!.Body.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a status of 0 is treated as no response.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithStatusZero_OmitsResponse()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":0,"statusText":""}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response).IsNull();
    }

    /// <summary>
    ///     Verifies that an invalid flow id falls back to a new GUID.
    /// </summary>
    [Test]
    public async Task ImportAsync_WithInvalidFlowId_GeneratesNewGuid()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanFlowId":"not-a-guid",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Id).IsNotEqualTo(System.Guid.Empty);
    }

    /// <summary>
    ///     Verifies that an entry whose request has no headers array still imports.
    /// </summary>
    [Test]
    public async Task ImportAsync_RequestWithoutHeadersArray_StillImports()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/"},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Headers.Count).IsEqualTo(0);
    }

    private static async Task<System.Collections.Generic.IReadOnlyList<Proxyfan.Domain.Traffic.TrafficFlow>> ImportAsync(string harJson)
    {
        var importer = new HarImporter();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(harJson));
        return await importer.ImportAsync(stream, CancellationToken.None);
    }
}
