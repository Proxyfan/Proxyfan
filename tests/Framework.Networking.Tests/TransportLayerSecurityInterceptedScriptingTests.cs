using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptedScripting" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptedScriptingTests
{
    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptedScripting.ApplyRequestAsync" />
    ///     returns the supplied request unchanged when no handler is configured.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_NullHandler_ReturnsOriginalRequest()
    {
        var original = BuildRequest();
        var requestBundle = new TransportLayerSecurityInterceptedScriptingRequestRequest
        {
            EffectiveRequest = original,
            Flow = BuildFlow(),
            Handler = null,
            Logger = NullLogger.Instance,
        };

        var projected = await TransportLayerSecurityInterceptedScripting.ApplyRequestAsync(requestBundle, CancellationToken.None);

        await Assert.That(projected).IsSameReferenceAs(original);
    }

    /// <summary>
    ///     Verifies that a handler that projects the request returns the projected value.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_HandlerProjects_ReturnsProjectedRequest()
    {
        var original = BuildRequest();
        var projected = BuildRequest();
        var handler = new StubScriptingHandler
        {
            RequestProjection = projected,
        };
        var requestBundle = new TransportLayerSecurityInterceptedScriptingRequestRequest
        {
            EffectiveRequest = original,
            Flow = BuildFlow(),
            Handler = handler,
            Logger = NullLogger.Instance,
        };

        var result = await TransportLayerSecurityInterceptedScripting.ApplyRequestAsync(requestBundle, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(projected);
    }

    /// <summary>
    ///     Verifies that a handler that throws a non-cancellation exception is swallowed and
    ///     the original request is returned.
    /// </summary>
    [Test]
    public async Task ApplyRequestAsync_HandlerThrows_SwallowsExceptionAndReturnsOriginal()
    {
        var original = BuildRequest();
        var handler = new StubScriptingHandler
        {
            RequestException = new InvalidOperationException("boom"),
        };
        var requestBundle = new TransportLayerSecurityInterceptedScriptingRequestRequest
        {
            EffectiveRequest = original,
            Flow = BuildFlow(),
            Handler = handler,
            Logger = NullLogger.Instance,
        };

        var result = await TransportLayerSecurityInterceptedScripting.ApplyRequestAsync(requestBundle, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(original);
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptedScripting.ApplyResponseAsync" />
    ///     returns the supplied response unchanged when no handler is configured.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_NullHandler_ReturnsOriginalResponse()
    {
        var original = BuildResponse();
        var responseBundle = new TransportLayerSecurityInterceptedScriptingResponseRequest
        {
            EffectiveRequest = BuildRequest(),
            FinalResponse = original,
            Flow = BuildFlow(),
            Handler = null,
            Logger = NullLogger.Instance,
        };

        var projected = await TransportLayerSecurityInterceptedScripting.ApplyResponseAsync(responseBundle, CancellationToken.None);

        await Assert.That(projected).IsSameReferenceAs(original);
    }

    /// <summary>
    ///     Verifies that a handler that projects the response returns the projected value.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_HandlerProjects_ReturnsProjectedResponse()
    {
        var original = BuildResponse();
        var projected = BuildResponse();
        var handler = new StubScriptingHandler
        {
            ResponseProjection = projected,
        };
        var responseBundle = new TransportLayerSecurityInterceptedScriptingResponseRequest
        {
            EffectiveRequest = BuildRequest(),
            FinalResponse = original,
            Flow = BuildFlow(),
            Handler = handler,
            Logger = NullLogger.Instance,
        };

        var result = await TransportLayerSecurityInterceptedScripting.ApplyResponseAsync(responseBundle, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(projected);
    }

    /// <summary>
    ///     Verifies that a handler that throws a non-cancellation exception during the
    ///     response hook is swallowed and the original response is returned.
    /// </summary>
    [Test]
    public async Task ApplyResponseAsync_HandlerThrows_SwallowsExceptionAndReturnsOriginal()
    {
        var original = BuildResponse();
        var handler = new StubScriptingHandler
        {
            ResponseException = new InvalidOperationException("boom"),
        };
        var responseBundle = new TransportLayerSecurityInterceptedScriptingResponseRequest
        {
            EffectiveRequest = BuildRequest(),
            FinalResponse = original,
            Flow = BuildFlow(),
            Handler = handler,
            Logger = NullLogger.Instance,
        };

        var result = await TransportLayerSecurityInterceptedScripting.ApplyResponseAsync(responseBundle, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(original);
    }

    private static TrafficFlow BuildFlow()
    {
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:50001", DateTimeOffset.UtcNow);
        return flow;
    }

    private static HypertextTransferProtocolRequestData BuildRequest()
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

    private static HypertextTransferProtocolResponseData BuildResponse()
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

    private sealed class StubScriptingHandler : IScriptingHandler
    {
        public Exception? RequestException { get; set; }

        public HypertextTransferProtocolRequestData? RequestProjection { get; set; }

        public Exception? ResponseException { get; set; }

        public HypertextTransferProtocolResponseData? ResponseProjection { get; set; }

        public Task<HypertextTransferProtocolRequestData> ApplyRequestAsync(string flowId, HypertextTransferProtocolRequestData request, CancellationToken cancellationToken)
        {
            _ = flowId;
            _ = cancellationToken;
            if (RequestException is not null)
            {
                return Task.FromException<HypertextTransferProtocolRequestData>(RequestException);
            }

            return Task.FromResult(RequestProjection ?? request);
        }

        public Task<HypertextTransferProtocolResponseData> ApplyResponseAsync(string flowId, HypertextTransferProtocolRequestData request, HypertextTransferProtocolResponseData response, CancellationToken cancellationToken)
        {
            _ = flowId;
            _ = request;
            _ = cancellationToken;
            if (ResponseException is not null)
            {
                return Task.FromException<HypertextTransferProtocolResponseData>(ResponseException);
            }

            return Task.FromResult(ResponseProjection ?? response);
        }
    }
}
