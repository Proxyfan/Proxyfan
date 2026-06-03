using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="WebSocketUpgradeDetector" />.
/// </summary>
public sealed class WebSocketUpgradeDetectorTests
{
    /// <summary>Both headers present with correct tokens → upgrade detected.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_BothHeadersValid_ReturnsTrue()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsTrue();
    }

    /// <summary>Detection is case-insensitive for header tokens.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_MixedCase_ReturnsTrue()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "WebSocket")
            .Add("Connection", "Upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsTrue();
    }

    /// <summary>Multi-token Connection header is correctly parsed.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_ConnectionMultiToken_ReturnsTrue()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "keep-alive, Upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsTrue();
    }

    /// <summary>Missing Upgrade header → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_MissingUpgrade_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty.Add("Connection", "upgrade"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Missing Connection header → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_MissingConnection_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty.Add("Upgrade", "websocket"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Upgrade header without "websocket" token → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_UpgradeTokenMismatch_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "h2c")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Connection header without "upgrade" token → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_ConnectionMissingUpgradeToken_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "keep-alive")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Empty header values → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_EmptyHeaderValues_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", string.Empty)
            .Add("Connection", string.Empty));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Missing Sec-WebSocket-Key → no upgrade (RFC 6455 §4.1).</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_MissingSecWebSocketKey_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Sec-WebSocket-Key not base64 of 16 bytes → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_WrongLengthSecWebSocketKey_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGVzdA==")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Sec-WebSocket-Key with invalid base64 → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_NonBase64SecWebSocketKey_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "not!base64@@@")
            .Add("Sec-WebSocket-Version", "13"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Missing Sec-WebSocket-Version → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_MissingSecWebSocketVersion_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ=="));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Sec-WebSocket-Version other than 13 → no upgrade (RFC 6455 §4.1).</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_UnsupportedSecWebSocketVersion_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "8"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Successful upgrade requires request + 101 + Upgrade + matching Accept.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_AllConditionsMet_ReturnsTrue()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));
        var response = BuildResponse(101, HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Sec-WebSocket-Accept", "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsTrue();
    }

    /// <summary>Non-101 status code → no successful upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_NonSwitchingStatus_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));
        var response = BuildResponse(200, HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Sec-WebSocket-Accept", "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Missing Upgrade header on response → no successful upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_ResponseMissingUpgrade_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));
        var response = BuildResponse(101, HeaderCollection.Empty);

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Wrong response upgrade token → no successful upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_ResponseWrongUpgradeToken_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));
        var response = BuildResponse(101, HeaderCollection.Empty
            .Add("Upgrade", "h2c")
            .Add("Sec-WebSocket-Accept", "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Missing Sec-WebSocket-Accept on response → no successful upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_ResponseMissingAccept_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));
        var response = BuildResponse(101, HeaderCollection.Empty.Add("Upgrade", "websocket"));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Sec-WebSocket-Accept that does not match the SHA-1 of key+GUID → no upgrade.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_ResponseMismatchedAccept_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Connection", "upgrade")
            .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
            .Add("Sec-WebSocket-Version", "13"));
        var response = BuildResponse(101, HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Sec-WebSocket-Accept", "dGVzdA=="));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Invalid request short-circuits the success check.</summary>
    [Test]
    public async Task HasWebSocketUpgradeSuccess_InvalidRequest_ReturnsFalse()
    {
        var request = BuildRequest(HeaderCollection.Empty);
        var response = BuildResponse(101, HeaderCollection.Empty
            .Add("Upgrade", "websocket")
            .Add("Sec-WebSocket-Accept", "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="));

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Non-GET method → no upgrade (RFC 6455 §4.1 mandates GET).</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_NonGetMethod_ReturnsFalse()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty
                .Add("Upgrade", "websocket")
                .Add("Connection", "upgrade")
                .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
                .Add("Sec-WebSocket-Version", "13"),
            Method = "POST",
            RequestUri = new Uri("/chat", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    /// <summary>HTTP/1.0 request → no upgrade (RFC 6455 §4.1 mandates HTTP/1.1).</summary>
    [Test]
    public async Task HasWebSocketUpgradeRequest_NonHttp11Version_ReturnsFalse()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty
                .Add("Upgrade", "websocket")
                .Add("Connection", "upgrade")
                .Add("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
                .Add("Sec-WebSocket-Version", "13"),
            Method = "GET",
            RequestUri = new Uri("/chat", UriKind.Relative),
            Version = "HTTP/1.0",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var result = WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(request);

        await Assert.That(result).IsFalse();
    }

    private static HypertextTransferProtocolRequestData BuildRequest(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("/chat", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData BuildResponse(int statusCode, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = statusCode == 101 ? "Switching Protocols" : "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
