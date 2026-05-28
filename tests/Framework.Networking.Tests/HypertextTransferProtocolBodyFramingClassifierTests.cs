using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolBodyFramingClassifier" /> per RFC 7230 § 3.3.3.
/// </summary>
public sealed class HypertextTransferProtocolBodyFramingClassifierTests
{
    /// <summary>
    ///     Chunked transfer-coding (as the last token) takes precedence over Content-Length per
    ///     RFC 7230 § 3.3.3.
    /// </summary>
    [Test]
    public async Task ClassifyRequest_ChunkedAndContentLength_ReturnsChunked()
    {
        var headers = HeaderCollection.Empty
            .Add("Transfer-Encoding", "chunked")
            .Add("Content-Length", "10");
        var request = CreateRequest("POST", headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.Chunked);
    }

    /// <summary>
    ///     A request with Content-Length: 0 has no body.
    /// </summary>
    [Test]
    public async Task ClassifyRequest_ContentLengthZero_ReturnsNone()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        var request = CreateRequest("POST", headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     A request with a positive Content-Length is framed by Content-Length.
    /// </summary>
    [Test]
    public async Task ClassifyRequest_ContentLengthPositive_ReturnsContentLength()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "42");
        var request = CreateRequest("POST", headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.ContentLength);
    }

    /// <summary>
    ///     A request without Transfer-Encoding or Content-Length has no body (requests cannot use
    ///     read-until-close framing per RFC 7230).
    /// </summary>
    [Test]
    public async Task ClassifyRequest_NoFramingHeaders_ReturnsNone()
    {
        var request = CreateRequest("GET", HeaderCollection.Empty);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     A request whose Content-Length value is not a number is treated as having no body.
    /// </summary>
    [Test]
    public async Task ClassifyRequest_NonNumericContentLength_ReturnsNone()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "abc");
        var request = CreateRequest("POST", headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     Chunked must be the LAST coding in the Transfer-Encoding list per RFC 7230 § 3.3.1.
    ///     A chunked token that is not last must not be treated as chunked framing.
    /// </summary>
    [Test]
    public async Task ClassifyRequest_TransferEncodingChunkedNotLast_ReturnsNone()
    {
        var headers = HeaderCollection.Empty.Add("Transfer-Encoding", "chunked, gzip");
        var request = CreateRequest("POST", headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     Detection of "chunked" is case-insensitive per RFC 7230.
    /// </summary>
    [Test]
    public async Task ClassifyRequest_TransferEncodingMixedCaseChunked_ReturnsChunked()
    {
        var headers = HeaderCollection.Empty.Add("Transfer-Encoding", "gzip, ChUnKeD");
        var request = CreateRequest("POST", headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyRequest(request);

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.Chunked);
    }

    /// <summary>
    ///     A response to a HEAD request never has a body, even if Content-Length is present.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_HeadRequestWithContentLength_ReturnsNone()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "HEAD");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     HEAD detection is case-insensitive.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_HeadRequestLowercase_ReturnsNone()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "head");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     1xx informational responses must never have a body.
    /// </summary>
    [Test]
    [Arguments(100)]
    [Arguments(101)]
    [Arguments(199)]
    public async Task ClassifyResponse_OneHundredRangeStatusCode_ReturnsNone(int statusCode)
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(statusCode, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     204 No Content and 304 Not Modified responses must never have a body.
    /// </summary>
    [Test]
    [Arguments(204)]
    [Arguments(304)]
    public async Task ClassifyResponse_NoBodyStatusCode_ReturnsNone(int statusCode)
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(statusCode, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     A response with chunked transfer-coding is framed by chunks.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_ChunkedEncoding_ReturnsChunked()
    {
        var headers = HeaderCollection.Empty.Add("Transfer-Encoding", "chunked");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.Chunked);
    }

    /// <summary>
    ///     A response with Content-Length: 0 has no body.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_ContentLengthZero_ReturnsNone()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.None);
    }

    /// <summary>
    ///     A response with a positive Content-Length is framed by Content-Length.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_ContentLengthPositive_ReturnsContentLength()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "42");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.ContentLength);
    }

    /// <summary>
    ///     A response without framing headers falls back to read-until-close (RFC 7230 § 3.3.3 step 7).
    /// </summary>
    [Test]
    public async Task ClassifyResponse_NoFramingHeaders_ReturnsUntilClose()
    {
        var response = CreateResponse(200, HeaderCollection.Empty);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.UntilClose);
    }

    /// <summary>
    ///     A negative Content-Length value is treated as missing and falls through to read-until-close.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_NegativeContentLength_ReturnsUntilClose()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "-5");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.UntilClose);
    }

    /// <summary>
    ///     200 OK responses to GET behave normally.
    /// </summary>
    [Test]
    public async Task ClassifyResponse_NormalGetResponse_HonorsContentLength()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "5");
        var response = CreateResponse(200, headers);

        var framing = HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse(response, "GET");

        await Assert.That(framing).IsEqualTo(HypertextTransferProtocolBodyFraming.ContentLength);
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string method, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = method,
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse(int statusCode, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
