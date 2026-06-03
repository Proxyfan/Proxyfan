using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2ResponseTranslation" />, covering
///     <c>:status</c> hoisting, lowercase normalisation of header names, multi-value expansion,
///     connection-header stripping, and out-of-range status validation.
/// </summary>
public sealed class HypertextTransferProtocolVersion2ResponseTranslationTests
{
    /// <summary>
    ///     The first entry of the translated header list is always <c>:status</c> with the
    ///     decimal status code as its value.
    /// </summary>
    [Test]
    public async Task Translate_ValidResponse_PlacesStatusPseudoHeaderFirst()
    {
        var response = BuildResponse(200, "OK", HeaderCollection.Empty, ReadOnlyMemory<byte>.Empty);

        var result = HypertextTransferProtocolVersion2ResponseTranslation.Translate(response);

        await Assert.That(result.Headers.Count).IsEqualTo(1);
        await Assert.That(result.Headers[0].Name).IsEqualTo(":status");
        await Assert.That(result.Headers[0].Value).IsEqualTo("200");
    }

    /// <summary>
    ///     Regular header names are normalised to lowercase per RFC 7540 § 8.1.2.
    /// </summary>
    [Test]
    public async Task Translate_MixedCaseHeader_LowercasesName()
    {
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "application/json")
            .Add("X-Trace-Id", "abc-123");
        var response = BuildResponse(200, "OK", headers, ReadOnlyMemory<byte>.Empty);

        var result = HypertextTransferProtocolVersion2ResponseTranslation.Translate(response);

        var names = new System.Collections.Generic.HashSet<string>();
        for (var index = 1; index < result.Headers.Count; index++)
        {
            names.Add(result.Headers[index].Name);
        }
        await Assert.That(names.Contains("content-type")).IsTrue();
        await Assert.That(names.Contains("x-trace-id")).IsTrue();
    }

    /// <summary>
    ///     Multi-valued headers (e.g. <c>Set-Cookie</c>) expand to one HPACK entry per value.
    /// </summary>
    [Test]
    public async Task Translate_MultipleValues_EmitsOneEntryPerValue()
    {
        var headers = HeaderCollection.Empty
            .Add("Set-Cookie", "a=1; Path=/")
            .Add("Set-Cookie", "b=2; Path=/");
        var response = BuildResponse(200, "OK", headers, ReadOnlyMemory<byte>.Empty);

        var result = HypertextTransferProtocolVersion2ResponseTranslation.Translate(response);

        var setCookieCount = 0;
        for (var index = 0; index < result.Headers.Count; index++)
        {
            if (string.Equals(result.Headers[index].Name, "set-cookie", StringComparison.Ordinal))
            {
                setCookieCount++;
            }
        }
        await Assert.That(setCookieCount).IsEqualTo(2);
    }

    /// <summary>
    ///     RFC 7540 § 8.1.2.2 forbids connection-specific headers — they must be stripped
    ///     from the translated response.
    /// </summary>
    [Test]
    public async Task Translate_ForbiddenConnectionHeaders_StripsThem()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "keep-alive")
            .Add("Transfer-Encoding", "chunked")
            .Add("Upgrade", "h2c")
            .Add("Keep-Alive", "timeout=5")
            .Add("Proxy-Connection", "keep-alive")
            .Add("Content-Type", "text/plain");
        var response = BuildResponse(200, "OK", headers, ReadOnlyMemory<byte>.Empty);

        var result = HypertextTransferProtocolVersion2ResponseTranslation.Translate(response);

        for (var index = 0; index < result.Headers.Count; index++)
        {
            var name = result.Headers[index].Name;
            await Assert.That(string.Equals(name, "connection", StringComparison.OrdinalIgnoreCase)).IsFalse();
            await Assert.That(string.Equals(name, "transfer-encoding", StringComparison.OrdinalIgnoreCase)).IsFalse();
            await Assert.That(string.Equals(name, "upgrade", StringComparison.OrdinalIgnoreCase)).IsFalse();
            await Assert.That(string.Equals(name, "keep-alive", StringComparison.OrdinalIgnoreCase)).IsFalse();
            await Assert.That(string.Equals(name, "proxy-connection", StringComparison.OrdinalIgnoreCase)).IsFalse();
        }
    }

    [Test]
    public async Task Translate_ConnectionListedHeaders_StripsThem()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "X-Internal, X-Trace")
            .Add("Connection", "X-Another")
            .Add("X-Internal", "secret")
            .Add("X-Trace", "trace-1")
            .Add("X-Another", "value")
            .Add("Content-Type", "text/plain");
        var response = BuildResponse(200, "OK", headers, ReadOnlyMemory<byte>.Empty);

        var result = HypertextTransferProtocolVersion2ResponseTranslation.Translate(response);
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < result.Headers.Count; index++)
        {
            names.Add(result.Headers[index].Name);
        }

        await Assert.That(names.Contains("x-internal")).IsFalse();
        await Assert.That(names.Contains("x-trace")).IsFalse();
        await Assert.That(names.Contains("x-another")).IsFalse();
        await Assert.That(names.Contains("content-type")).IsTrue();
    }

    /// <summary>
    ///     The body view is carried through verbatim.
    /// </summary>
    [Test]
    public async Task Translate_BodyBytes_PassThroughVerbatim()
    {
        var payload = Encoding.UTF8.GetBytes("hello world");
        var response = BuildResponse(200, "OK", HeaderCollection.Empty, payload);

        var result = HypertextTransferProtocolVersion2ResponseTranslation.Translate(response);

        await Assert.That(result.Body.Length).IsEqualTo(payload.Length);
        await Assert.That(result.Body.Span.SequenceEqual(payload)).IsTrue();
    }

    /// <summary>
    ///     Status codes outside the [100, 999] range are rejected with
    ///     <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [Test]
    [Arguments(99)]
    [Arguments(1000)]
    [Arguments(0)]
    public async Task Translate_StatusOutOfRange_Throws(int statusCode)
    {
        var response = BuildResponse(statusCode, "?", HeaderCollection.Empty, ReadOnlyMemory<byte>.Empty);

        await Assert.That(() => HypertextTransferProtocolVersion2ResponseTranslation.Translate(response))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static HypertextTransferProtocolResponseData BuildResponse(
        int statusCode,
        string reasonPhrase,
        HeaderCollection headers,
        ReadOnlyMemory<byte> body)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = reasonPhrase,
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        return response;
    }
}
