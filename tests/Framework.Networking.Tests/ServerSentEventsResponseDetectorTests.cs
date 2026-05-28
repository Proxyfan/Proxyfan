using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsResponseDetector" />.
/// </summary>
public sealed class ServerSentEventsResponseDetectorTests
{
    /// <summary>
    ///     Verifies that a plain <c>text/event-stream</c> Content-Type is detected.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeIsEventStream_ReturnsTrue()
    {
        var response = CreateResponseWithContentType("text/event-stream");

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsTrue();
    }

    /// <summary>
    ///     Verifies that <c>text/event-stream; charset=utf-8</c> is still detected.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeHasParameters_ReturnsTrue()
    {
        var response = CreateResponseWithContentType("text/event-stream; charset=utf-8");

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsTrue();
    }

    /// <summary>
    ///     Verifies that detection is case insensitive.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeIsUppercase_ReturnsTrue()
    {
        var response = CreateResponseWithContentType("TEXT/EVENT-STREAM");

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsTrue();
    }

    /// <summary>
    ///     Verifies that whitespace around the media type does not prevent detection.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeHasWhitespace_ReturnsTrue()
    {
        var response = CreateResponseWithContentType("   text/event-stream  ");

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsTrue();
    }

    /// <summary>
    ///     Verifies that an unrelated Content-Type is not detected as SSE.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeIsJson_ReturnsFalse()
    {
        var response = CreateResponseWithContentType("application/json");

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsFalse();
    }

    /// <summary>
    ///     Verifies that a missing Content-Type header is not detected as SSE.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeMissing_ReturnsFalse()
    {
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = System.ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsFalse();
    }

    /// <summary>
    ///     Verifies that an empty Content-Type header is not detected as SSE.
    /// </summary>
    [Test]
    public async Task HasServerSentEventsResponse_WhenContentTypeIsBlank_ReturnsFalse()
    {
        var response = CreateResponseWithContentType("   ");

        var detected = ServerSentEventsResponseDetector.HasServerSentEventsResponse(response);

        await Assert.That(detected).IsFalse();
    }

    private static HypertextTransferProtocolResponseData CreateResponseWithContentType(string contentType)
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", contentType);
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = System.ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        return response;
    }
}
