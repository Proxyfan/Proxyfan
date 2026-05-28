using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     Hand-rolled test stub for <see cref="IScriptingHandler" /> that lets tests assert
///     whether each phase was invoked and what request/response it produced.
/// </summary>
public sealed class StubScriptingHandler : IScriptingHandler
{
    /// <summary>
    ///     Gets the number of times the request-phase hook was invoked.
    /// </summary>
    public int RequestInvocationCount { get; private set; }

    /// <summary>
    ///     Gets the number of times the response-phase hook was invoked.
    /// </summary>
    public int ResponseInvocationCount { get; private set; }

    /// <summary>
    ///     Gets or sets a function that transforms the inbound request.
    /// </summary>
    public Func<HypertextTransferProtocolRequestData, HypertextTransferProtocolRequestData>? RequestTransformer { get; set; }

    /// <summary>
    ///     Gets or sets a function that transforms the inbound response.
    /// </summary>
    public Func<HypertextTransferProtocolResponseData, HypertextTransferProtocolResponseData>? ResponseTransformer { get; set; }

    /// <summary>
    ///     Gets or sets the exception to throw from the request-phase hook.
    /// </summary>
    public Exception? RequestException { get; set; }

    /// <summary>
    ///     Gets or sets the exception to throw from the response-phase hook.
    /// </summary>
    public Exception? ResponseException { get; set; }

    /// <inheritdoc />
    public Task<HypertextTransferProtocolRequestData> ApplyRequestAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        RequestInvocationCount++;
        if (RequestException is not null)
        {
            throw RequestException;
        }

        var transformed = RequestTransformer is not null ? RequestTransformer(request) : request;
        return Task.FromResult(transformed);
    }

    /// <inheritdoc />
    public Task<HypertextTransferProtocolResponseData> ApplyResponseAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken)
    {
        ResponseInvocationCount++;
        if (ResponseException is not null)
        {
            throw ResponseException;
        }

        var transformed = ResponseTransformer is not null ? ResponseTransformer(response) : response;
        return Task.FromResult(transformed);
    }
}
