using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptedScriptingRequestRequest" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptedScriptingRequestRequestTests
{
    /// <summary>
    ///     Verifies that the required init properties round-trip the values supplied at
    ///     construction.
    /// </summary>
    [Test]
    public async Task InitProperties_AllAssigned_RoundTrip()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        });
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:5000", DateTimeOffset.UtcNow);
        var logger = NullLogger.Instance;

        var bundle = new TransportLayerSecurityInterceptedScriptingRequestRequest
        {
            EffectiveRequest = request,
            Flow = flow,
            Handler = null,
            Logger = logger,
        };

        await Assert.That(bundle.EffectiveRequest).IsSameReferenceAs(request);
        await Assert.That(bundle.Flow).IsSameReferenceAs(flow);
        await Assert.That(bundle.Handler).IsNull();
        await Assert.That(bundle.Logger).IsSameReferenceAs(logger);
    }
}
