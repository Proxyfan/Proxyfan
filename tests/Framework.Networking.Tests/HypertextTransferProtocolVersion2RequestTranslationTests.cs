using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2RequestTranslation" />, covering
///     pseudo-header hoisting, URI reconstruction, Host header injection from <c>:authority</c>,
///     and connection-specific header stripping per RFC 7540 § 8.1.2.
/// </summary>
public sealed class HypertextTransferProtocolVersion2RequestTranslationTests
{
    /// <summary>
    ///     A minimal HEADERS block with the four required pseudo-headers translates to a
    ///     request whose URI is reconstructed from scheme + authority + path.
    /// </summary>
    [Test]
    public async Task Translate_ValidPseudoHeaders_ReconstructsAbsoluteRequestUri()
    {
        var headers = BuildHeaderList(
            (":method", "GET"),
            (":scheme", "https"),
            (":authority", "example.com"),
            (":path", "/index.html"));

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(request.Method).IsEqualTo("GET");
        await Assert.That(request.RequestUri.ToString()).IsEqualTo("https://example.com/index.html");
        await Assert.That(request.Version).IsEqualTo("HTTP/2");
    }

    /// <summary>
    ///     When the HEADERS block does not include an explicit Host header, the translator
    ///     synthesises one from the <c>:authority</c> pseudo-header so downstream HTTP/1.1
    ///     forwarders can write a valid request line.
    /// </summary>
    [Test]
    public async Task Translate_MissingHostHeader_SynthesisesHostFromAuthority()
    {
        var headers = BuildHeaderList(
            (":method", "GET"),
            (":scheme", "https"),
            (":authority", "api.example.com"),
            (":path", "/v1/widgets"),
            ("accept", "application/json"));

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(request.Headers.Get("Host")).IsEqualTo("api.example.com");
        await Assert.That(request.Headers.Get("accept")).IsEqualTo("application/json");
    }

    /// <summary>
    ///     When the HEADERS block includes an explicit Host header, the translator preserves
    ///     it rather than overwriting from <c>:authority</c>.
    /// </summary>
    [Test]
    public async Task Translate_ExplicitHostHeader_PreservesProvidedValue()
    {
        var headers = BuildHeaderList(
            (":method", "GET"),
            (":scheme", "https"),
            (":authority", "api.example.com"),
            (":path", "/"),
            ("host", "override.example.com"));

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(request.Headers.Get("Host")).IsEqualTo("override.example.com");
    }

    /// <summary>
    ///     RFC 7540 § 8.1.2.2 forbids the connection-specific headers
    ///     (<c>Connection</c>, <c>Keep-Alive</c>, <c>Proxy-Connection</c>,
    ///     <c>Transfer-Encoding</c>, <c>Upgrade</c>) — when present in an HTTP/2 request
    ///     header list, the translator strips them.
    /// </summary>
    [Test]
    public async Task Translate_ForbiddenConnectionHeaders_StripsThem()
    {
        var headers = BuildHeaderList(
            (":method", "POST"),
            (":scheme", "https"),
            (":authority", "example.com"),
            (":path", "/upload"),
            ("connection", "keep-alive"),
            ("transfer-encoding", "chunked"),
            ("upgrade", "h2c"),
            ("keep-alive", "timeout=5"),
            ("proxy-connection", "keep-alive"),
            ("content-type", "application/json"));

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(request.Headers.HasHeader("Connection")).IsFalse();
        await Assert.That(request.Headers.HasHeader("Transfer-Encoding")).IsFalse();
        await Assert.That(request.Headers.HasHeader("Upgrade")).IsFalse();
        await Assert.That(request.Headers.HasHeader("Keep-Alive")).IsFalse();
        await Assert.That(request.Headers.HasHeader("Proxy-Connection")).IsFalse();
        await Assert.That(request.Headers.Get("content-type")).IsEqualTo("application/json");
    }

    /// <summary>
    ///     The body view is carried through verbatim.
    /// </summary>
    [Test]
    public async Task Translate_BodyBytes_PassThroughVerbatim()
    {
        var headers = BuildHeaderList(
            (":method", "POST"),
            (":scheme", "https"),
            (":authority", "example.com"),
            (":path", "/echo"));
        var payload = Encoding.UTF8.GetBytes("{\"hello\":\"world\"}");

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, payload);

        await Assert.That(request.Body.Length).IsEqualTo(payload.Length);
        await Assert.That(request.Body.Span.SequenceEqual(payload)).IsTrue();
    }

    /// <summary>
    ///     When the HEADERS block omits <c>:method</c> the translator throws
    ///     <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Translate_MissingMethodPseudoHeader_Throws()
    {
        var headers = BuildHeaderList(
            (":scheme", "https"),
            (":authority", "example.com"),
            (":path", "/"));

        await Assert.That(() => HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty))
            .Throws<FormatException>();
    }

    /// <summary>
    ///     When the HEADERS block omits <c>:authority</c> the translator throws
    ///     <see cref="FormatException" /> — we require an explicit authority so the request
    ///     URI is unambiguous.
    /// </summary>
    [Test]
    public async Task Translate_MissingAuthorityPseudoHeader_Throws()
    {
        var headers = BuildHeaderList(
            (":method", "GET"),
            (":scheme", "https"),
            (":path", "/"));

        await Assert.That(() => HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty))
            .Throws<FormatException>();
    }

    /// <summary>
    ///     When the HEADERS block omits <c>:scheme</c>, the translator defaults to <c>https</c>
    ///     since the proxy operates over TLS-intercepted connections.
    /// </summary>
    [Test]
    public async Task Translate_MissingSchemePseudoHeader_DefaultsToHttps()
    {
        var headers = BuildHeaderList(
            (":method", "GET"),
            (":authority", "example.com"),
            (":path", "/"));

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(request.RequestUri.Scheme).IsEqualTo("https");
    }

    /// <summary>
    ///     When the HEADERS block omits <c>:path</c>, the translator falls back to <c>/</c>.
    /// </summary>
    [Test]
    public async Task Translate_MissingPathPseudoHeader_DefaultsToRootPath()
    {
        var headers = BuildHeaderList(
            (":method", "GET"),
            (":scheme", "https"),
            (":authority", "example.com"));

        var request = HypertextTransferProtocolVersion2RequestTranslation.Translate(headers, ReadOnlyMemory<byte>.Empty);

        await Assert.That(request.RequestUri.AbsolutePath).IsEqualTo("/");
    }

    private static List<HypertextTransferProtocolVersion2HpackHeaderField> BuildHeaderList(
        params (string Name, string Value)[] fields)
    {
        var list = new List<HypertextTransferProtocolVersion2HpackHeaderField>(fields.Length);
        for (var index = 0; index < fields.Length; index++)
        {
            var entry = new HypertextTransferProtocolVersion2HpackHeaderField(fields[index].Name, fields[index].Value);
            list.Add(entry);
        }
        return list;
    }
}
