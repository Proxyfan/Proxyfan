using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolResponseData" />.
/// </summary>
public sealed class HypertextTransferProtocolResponseDataTests
{
    /// <summary>
    ///     Verifies that construction preserves all provided values.
    /// </summary>
    [Test]
    public async Task Constructor_WhenInitialized_PreservesValues()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/plain");
        byte[] body =
        [
            4,
            5,
            6,
        ];
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = "Accepted",
            StatusCode = 202,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);

        await Assert.That(response.Body.Length).IsEqualTo(3);
        await Assert.That(response.Headers).IsEqualTo(headers);
        await Assert.That(response.ReasonPhrase).IsEqualTo("Accepted");
        await Assert.That(response.StatusCode).IsEqualTo(202);
        await Assert.That(response.Version).IsEqualTo("HTTP/1.1");
    }
}