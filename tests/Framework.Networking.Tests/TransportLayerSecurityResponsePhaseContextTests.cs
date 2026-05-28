using Proxyfan.Domain.Traffic;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityResponsePhaseContext" />.
/// </summary>
public sealed class TransportLayerSecurityResponsePhaseContextTests
{
    /// <summary>
    ///     The four required init properties are returned by the corresponding getters.
    /// </summary>
    [Test]
    public async Task Properties_AfterInitialization_ReturnInjectedValues()
    {
        var clientPipe = new Pipe();
        var serverPipe = new Pipe();
        var pipes = new TransportLayerSecurityInterceptionPipes(
            clientPipe.Reader,
            clientPipe.Writer,
            serverPipe.Reader,
            serverPipe.Writer);

        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1", DateTimeOffset.UtcNow);
        var request = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            StatusCode = 200,
            ReasonPhrase = "OK",
            Version = "HTTP/1.1",
        });
        var exchange = new HypertextTransferProtocolProxyResponseExchange(Array.Empty<byte>(), Array.Empty<byte>(), response);

        var context = new TransportLayerSecurityResponsePhaseContext
        {
            EffectiveRequest = request,
            Flow = flow,
            Pipes = pipes,
            ResponseExchange = exchange,
        };

        await Assert.That(context.EffectiveRequest).IsSameReferenceAs(request);
        await Assert.That(context.Flow).IsSameReferenceAs(flow);
        await Assert.That(context.Pipes).IsSameReferenceAs(pipes);
        await Assert.That(context.ResponseExchange).IsSameReferenceAs(exchange);
    }
}
