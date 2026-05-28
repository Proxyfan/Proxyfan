using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="InspectorCookieFormatter" />.
/// </summary>
public sealed class InspectorCookieFormatterTests
{
    [Test]
    public async Task FormatRequest_NoCookieHeader_ReturnsEmptyString()
    {
        var request = BuildRequest(HeaderCollection.Empty);

        var result = InspectorCookieFormatter.FormatRequest(request);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FormatRequest_WithCookieHeader_RendersTable()
    {
        var headers = HeaderCollection.Empty.Add("Cookie", "session=abc; theme=dark");
        var request = BuildRequest(headers);

        var result = InspectorCookieFormatter.FormatRequest(request);

        await Assert.That(result.Contains("session")).IsTrue();
        await Assert.That(result.Contains("abc")).IsTrue();
        await Assert.That(result.Contains("theme")).IsTrue();
        await Assert.That(result.Contains("dark")).IsTrue();
    }

    [Test]
    public async Task FormatResponse_NoSetCookieHeader_ReturnsEmptyString()
    {
        var response = BuildResponse(HeaderCollection.Empty);

        var result = InspectorCookieFormatter.FormatResponse(response);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task FormatResponse_WithSetCookieHeaders_RendersAllParsedEntries()
    {
        var headers = HeaderCollection.Empty
            .Add("Set-Cookie", "session=abc; Path=/")
            .Add("Set-Cookie", "theme=dark; Secure");
        var response = BuildResponse(headers);

        var result = InspectorCookieFormatter.FormatResponse(response);

        await Assert.That(result.Contains("session")).IsTrue();
        await Assert.That(result.Contains("theme")).IsTrue();
    }

    [Test]
    public async Task FormatResponse_UnparseableSetCookie_IsSkipped()
    {
        var headers = HeaderCollection.Empty
            .Add("Set-Cookie", string.Empty)
            .Add("Set-Cookie", "good=cookie");
        var response = BuildResponse(headers);

        var result = InspectorCookieFormatter.FormatResponse(response);

        await Assert.That(result.Contains("good")).IsTrue();
    }

    private static HypertextTransferProtocolRequestData BuildRequest(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData BuildResponse(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
