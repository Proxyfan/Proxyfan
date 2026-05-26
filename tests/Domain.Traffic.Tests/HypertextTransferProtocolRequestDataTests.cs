using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolRequestData" />.
/// </summary>
public sealed class HypertextTransferProtocolRequestDataTests
{
    /// <summary>
    ///     Verifies that construction preserves all provided values.
    /// </summary>
    [Test]
    public async Task Constructor_WhenInitialized_PreservesValues()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        byte[] body =
        [
            1,
            2,
            3,
        ];
        var requestUri = new Uri("https://example.com/api");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = headers,
            Method = "POST",
            RequestUri = requestUri,
            Version = "HTTP/2",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        await Assert.That(request.Body.Length).IsEqualTo(3);
        await Assert.That(request.Headers).IsEqualTo(headers);
        await Assert.That(request.Method).IsEqualTo("POST");
        await Assert.That(request.RequestUri).IsEqualTo(requestUri);
        await Assert.That(request.Version).IsEqualTo("HTTP/2");
    }
}