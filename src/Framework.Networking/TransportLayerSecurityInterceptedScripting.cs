using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static scripting wrappers used by <see cref="TransportLayerSecurityInterceptorHandler" />
///     to safely invoke optional <see cref="Proxyfan.Domain.Scripting.IScriptingHandler" />
///     request/response hooks. The helpers swallow non-cancellation exceptions so that one
///     misbehaving script never disrupts an otherwise-healthy proxy flow.
/// </summary>
public static class TransportLayerSecurityInterceptedScripting
{
    /// <summary>
    ///     Invokes the request-phase script hook on the supplied handler when present; returns
    ///     the supplied request unchanged when no handler is configured or when the script
    ///     throws a non-cancellation exception.
    /// </summary>
    /// <param name="request">The bundled scripting invocation arguments.</param>
    /// <param name="cancellationToken">A token that cancels the script invocation.</param>
    /// <returns>The script-projected request, or the original request when no projection occurred.</returns>
    public static async Task<HypertextTransferProtocolRequestData> ApplyRequestAsync(
        TransportLayerSecurityInterceptedScriptingRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Handler is null)
        {
            return request.EffectiveRequest;
        }

        try
        {
            var flowId = request.Flow.Id.ToString();
            var projected = await request.Handler.ApplyRequestAsync(flowId, request.EffectiveRequest, cancellationToken).ConfigureAwait(false);
            return projected;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            request.Logger.LogWarning(ex, "TLS scripting request-phase hook threw; continuing with unmodified request");
            return request.EffectiveRequest;
        }
    }

    /// <summary>
    ///     Invokes the response-phase script hook on the supplied handler when present; returns
    ///     the supplied response unchanged when no handler is configured or when the script
    ///     throws a non-cancellation exception.
    /// </summary>
    /// <param name="request">The bundled scripting invocation arguments.</param>
    /// <param name="cancellationToken">A token that cancels the script invocation.</param>
    /// <returns>The script-projected response, or the original response when no projection occurred.</returns>
    public static async Task<HypertextTransferProtocolResponseData> ApplyResponseAsync(
        TransportLayerSecurityInterceptedScriptingResponseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Handler is null)
        {
            return request.FinalResponse;
        }

        try
        {
            var flowId = request.Flow.Id.ToString();
            var projected = await request.Handler.ApplyResponseAsync(flowId, request.EffectiveRequest, request.FinalResponse, cancellationToken).ConfigureAwait(false);
            return projected;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            request.Logger.LogWarning(ex, "TLS scripting response-phase hook threw; continuing with unmodified response");
            return request.FinalResponse;
        }
    }
}
