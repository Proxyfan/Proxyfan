using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptedForwardContext" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptedForwardContextTests
{
    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptedForwardContext" /> stores
    ///     all init properties and that <see cref="TransportLayerSecurityInterceptedForwardContext.ServeLocal" />
    ///     defaults to <see langword="null" /> and can be set via <c>with</c>.
    /// </summary>
    [Test]
    public async Task InitProperties_DefaultsAndAssignment_RoundTrip()
    {
        var connection = new StubProxyConnection();
        var effective = BuildRequestData();
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var requestExchange = new HypertextTransferProtocolProxyRequestExchange(ReadOnlyMemory<byte>.Empty, Array.Empty<byte>(), BuildRequestData());
        var clientReaderPipe = new System.IO.Pipelines.Pipe();
        var clientWriterPipe = new System.IO.Pipelines.Pipe();
        var serverReaderPipe = new System.IO.Pipelines.Pipe();
        var serverWriterPipe = new System.IO.Pipelines.Pipe();
        var pipes = new TransportLayerSecurityInterceptionPipes(
            clientReaderPipe.Reader,
            clientWriterPipe.Writer,
            serverReaderPipe.Reader,
            serverWriterPipe.Writer);

        var ctx = new TransportLayerSecurityInterceptedForwardContext
        {
            Connection = connection,
            EffectiveRequest = effective,
            Flow = flow,
            Pipes = pipes,
            RequestExchange = requestExchange,
        };

        await Assert.That(ctx.Connection).IsSameReferenceAs(connection);
        await Assert.That(ctx.EffectiveRequest).IsSameReferenceAs(effective);
        await Assert.That(ctx.Flow).IsSameReferenceAs(flow);
        await Assert.That(ctx.Pipes).IsSameReferenceAs(pipes);
        await Assert.That(ctx.RequestExchange).IsSameReferenceAs(requestExchange);
        await Assert.That(ctx.ServeLocal).IsNull();

        var localResponse = BuildResponseData();
        var local = new Proxyfan.Domain.Rules.Pipeline.RequestPipelineAction.ServeLocalResponse(localResponse);
        var ctxWithLocal = ctx with { ServeLocal = local };
        await Assert.That(ctxWithLocal.ServeLocal).IsSameReferenceAs(local);
    }

    private static HypertextTransferProtocolRequestData BuildRequestData()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        return new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        });
    }

    private static HypertextTransferProtocolResponseData BuildResponseData()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        return new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
    }
}
