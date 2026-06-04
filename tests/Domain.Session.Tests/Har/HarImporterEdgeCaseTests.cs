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

    /// <summary>
    ///     Verifies that whitespace _proxyfanClientEndPoint falls back to the "unknown" sentinel,
    ///     exercising the white-space false branch in ExtractClientEndPoint.
    /// </summary>
    [Test]
    public async Task ImportAsync_WhitespaceClientEndPoint_FallsBackToUnknown()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanClientEndPoint":"   ",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].ClientEndPoint).IsEqualTo("unknown");
    }

    /// <summary>
    ///     Verifies that a non-string _proxyfanClientEndPoint is ignored (number) and the
    ///     fallback "unknown" is returned (exercising the value-kind guard).
    /// </summary>
    [Test]
    public async Task ImportAsync_NumericClientEndPoint_FallsBackToUnknown()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanClientEndPoint":1234,
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].ClientEndPoint).IsEqualTo("unknown");
    }

    /// <summary>
    ///     Verifies that a populated _proxyfanComment is preserved on the imported flow.
    /// </summary>
    [Test]
    public async Task ImportAsync_WithComment_PreservesComment()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanComment":"important capture",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Comment).IsEqualTo("important capture");
    }

    /// <summary>
    ///     Verifies that a whitespace-only _proxyfanComment is dropped (null), exercising the
    ///     IsNullOrWhiteSpace false branch in ExtractComment.
    /// </summary>
    [Test]
    public async Task ImportAsync_WhitespaceComment_DropsToNull()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanComment":"   ",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Comment).IsNull();
    }

    /// <summary>
    ///     Verifies that a recognised _proxyfanColorTag is preserved on the imported flow.
    /// </summary>
    [Test]
    public async Task ImportAsync_WithColorTag_PreservesTag()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanColorTag":"Red",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].ColorTag).IsEqualTo(Proxyfan.Domain.Traffic.TrafficFlowColorTag.Red);
    }

    /// <summary>
    ///     Verifies that an unrecognised _proxyfanColorTag value falls back to None.
    /// </summary>
    [Test]
    public async Task ImportAsync_UnknownColorTag_FallsBackToNone()
    {
        const string harJson = """
            {"log":{"entries":[
                {"_proxyfanColorTag":"Magenta",
                 "request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].ColorTag).IsEqualTo(Proxyfan.Domain.Traffic.TrafficFlowColorTag.None);
    }

    /// <summary>
    ///     Verifies that an empty-string httpVersion on the request falls back to the
    ///     "HTTP/1.1" default, exercising the IsNullOrEmpty branch in ExtractStringOrDefault.
    /// </summary>
    [Test]
    public async Task ImportAsync_EmptyHttpVersion_FallsBackToHttp11Default()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","httpVersion":"","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Version).IsEqualTo("HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that a response without a "content" property still imports with an empty body.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithoutContent_HasEmptyBody()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[]}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response!.Body.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that headers whose value field is missing are skipped.
    /// </summary>
    [Test]
    public async Task ImportAsync_HeaderMissingValue_IsSkipped()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[{"name":"X"},{"name":"X","value":"y"}]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Headers.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that response content marked with encoding "base64" is decoded into raw bytes.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithBase64Encoding_DecodesBody()
    {
        var originalBytes = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD };
        var base64 = System.Convert.ToBase64String(originalBytes);
        var harJson = "{\"log\":{\"entries\":[{\"request\":{\"method\":\"GET\",\"url\":\"https://example.com/\",\"headers\":[]},"
            + "\"response\":{\"status\":200,\"statusText\":\"OK\",\"headers\":[],\"content\":{\"text\":\"" + base64 + "\",\"encoding\":\"base64\"}}}]}}";
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response!.Body.ToArray()).IsEquivalentTo(originalBytes);
    }

    /// <summary>
    ///     Verifies that base64 encoding matching is case-insensitive.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithBase64EncodingMixedCase_DecodesBody()
    {
        var originalBytes = new byte[] { 0x10, 0x20, 0x30 };
        var base64 = System.Convert.ToBase64String(originalBytes);
        var harJson = "{\"log\":{\"entries\":[{\"request\":{\"method\":\"GET\",\"url\":\"https://example.com/\",\"headers\":[]},"
            + "\"response\":{\"status\":200,\"statusText\":\"OK\",\"headers\":[],\"content\":{\"text\":\"" + base64 + "\",\"encoding\":\"Base64\"}}}]}}";
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response!.Body.ToArray()).IsEquivalentTo(originalBytes);
    }

    /// <summary>
    ///     Verifies that malformed base64 text yields an empty body rather than corrupting data.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithInvalidBase64_HasEmptyBody()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{"text":"not_valid_base64!!!","encoding":"base64"}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response!.Body.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that content without an encoding field continues to be treated as UTF-8 text.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithoutEncoding_TreatsTextAsUtf8()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{"text":"hello"}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(Encoding.UTF8.GetString(flows[0].Response!.Body.Span)).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that headers with non-string name/value properties are skipped instead of
    ///     throwing and aborting the import.
    /// </summary>
    [Test]
    public async Task ImportAsync_HeaderWithNonStringName_IsSkipped()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[{"name":1,"value":"v"},{"name":"X","value":2},{"name":"Y","value":"z"}]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Headers.Count).IsEqualTo(1);
        await Assert.That(flows[0].Request!.Headers.HasHeader("Y")).IsTrue();
    }

    /// <summary>
    ///     Verifies that a response with an out-of-range status (e.g. 99999) omits the response
    ///     instead of throwing.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithOutOfRangeStatus_OmitsResponse()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":99999,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response).IsNull();
    }

    /// <summary>
    ///     Verifies that a response whose numeric status is not representable as Int32 omits the
    ///     response instead of throwing.
    /// </summary>
    [Test]
    public async Task ImportAsync_ResponseWithNonIntegerStatus_OmitsResponse()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"GET","url":"https://example.com/","headers":[]},
                 "response":{"status":200.5,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Response).IsNull();
    }

    /// <summary>
    ///     Verifies that a legacy HAR without a postData block loads with an empty request body
    ///     (regression guard for v1 files).
    /// </summary>
    [Test]
    public async Task Import_LegacyV1HarMissingRequestBody_LoadsAsEmpty()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"POST","url":"https://example.com/","headers":[]},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        await Assert.That(flows[0].Request!.Body.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a postData block with a params array (as produced by browser DevTools)
    ///     is reconstructed into a URL-encoded request body.
    /// </summary>
    [Test]
    public async Task ImportAsync_PostDataWithParams_ReconstructsUrlEncodedBody()
    {
        const string harJson = """
            {"log":{"entries":[
                {"request":{"method":"POST","url":"https://example.com/","headers":[],
                  "postData":{"mimeType":"application/x-www-form-urlencoded",
                    "params":[{"name":"key1","value":"hello"},{"name":"key2","value":"world"}]}},
                 "response":{"status":200,"statusText":"OK","headers":[],"content":{}}}
            ]}}
            """;
        var flows = await ImportAsync(harJson);

        var body = Encoding.UTF8.GetString(flows[0].Request!.Body.Span);
        await Assert.That(body).IsEqualTo("key1=hello&key2=world");
    }

    private static async Task<System.Collections.Generic.IReadOnlyList<Proxyfan.Domain.Traffic.TrafficFlow>> ImportAsync(string harJson)
    {
        var importer = new HarImporter();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(harJson));
        return await importer.ImportAsync(stream, CancellationToken.None);
    }
}
