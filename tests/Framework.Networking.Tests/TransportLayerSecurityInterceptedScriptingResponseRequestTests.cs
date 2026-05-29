using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptedScriptingResponseRequest" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptedScriptingResponseRequestTests
{
    /// <summary>
    ///     Verifies that the required init properties round-trip the values supplied at
    ///     construction.
    /// </summary>
    [Test]
    public async Task InitProperties_AllAssigned_RoundTrip()
    {
        var requestHeaders = HeaderCollection.Empty.Add("Host", "example.com");
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        });
        var responseHeaders = HeaderCollection.Empty.Add("Content-Length", "0");
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:5000", DateTimeOffset.UtcNow);
        var logger = NullLogger.Instance;

        var bundle = new TransportLayerSecurityInterceptedScriptingResponseRequest
        {
            EffectiveRequest = request,
            FinalResponse = response,
            Flow = flow,
            Handler = null,
            Logger = logger,
        };

        await Assert.That(bundle.EffectiveRequest).IsSameReferenceAs(request);
        await Assert.That(bundle.FinalResponse).IsSameReferenceAs(response);
        await Assert.That(bundle.Flow).IsSameReferenceAs(flow);
        await Assert.That(bundle.Handler).IsNull();
        await Assert.That(bundle.Logger).IsSameReferenceAs(logger);
    }
}
