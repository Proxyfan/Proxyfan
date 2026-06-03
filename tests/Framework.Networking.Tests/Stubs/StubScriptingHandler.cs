using Proxyfan.Domain;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     Hand-rolled test stub for <see cref="IScriptingHandler" /> that lets tests assert
///     whether each phase was invoked and what request/response it produced. Supports both
///     thrown exceptions (legacy raw-throw path) and <see cref="ScriptError" /> failure
///     results (Result-contract path) so tests can exercise either branch of the caller.
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

    /// <summary>
    ///     Gets or sets a <see cref="ScriptError" /> the request-phase hook should return as
    ///     a failure result instead of a transformed request.
    /// </summary>
    public ScriptError? RequestError { get; set; }

    /// <summary>
    ///     Gets or sets a <see cref="ScriptError" /> the response-phase hook should return as
    ///     a failure result instead of a transformed response.
    /// </summary>
    public ScriptError? ResponseError { get; set; }

    /// <inheritdoc />
    public Task<Result<HypertextTransferProtocolRequestData>> ApplyRequestAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        RequestInvocationCount++;
        if (RequestException is not null)
        {
            throw RequestException;
        }

        if (RequestError is not null)
        {
            return Task.FromResult(Result.Failure<HypertextTransferProtocolRequestData>(RequestError));
        }

        var transformed = RequestTransformer is not null ? RequestTransformer(request) : request;
        return Task.FromResult(Result.Success(transformed));
    }

    /// <inheritdoc />
    public Task<Result<HypertextTransferProtocolResponseData>> ApplyResponseAsync(
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

        if (ResponseError is not null)
        {
            return Task.FromResult(Result.Failure<HypertextTransferProtocolResponseData>(ResponseError));
        }

        var transformed = ResponseTransformer is not null ? ResponseTransformer(response) : response;
        return Task.FromResult(Result.Success(transformed));
    }
}
