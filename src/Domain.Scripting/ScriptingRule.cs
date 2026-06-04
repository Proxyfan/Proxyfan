using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     First-class <see cref="IAsyncRequestPhaseRule" /> and <see cref="IAsyncResponsePhaseRule" />
///     adapter that integrates <see cref="IScriptingHandler" /> into the rule engine.
///     <para>
///         For the <b>request phase</b> the rule runs at priority 20 000 — after the breakpoint
///         rule at 10 000. A <see cref="RequestPipelineAction.ModifyRequest" /> is returned when
///         the script mutates the request; <see langword="null" /> when there is no active script,
///         the script has no request hook, or the script leaves the request unchanged. Scripting
///         failures (runtime errors, sandbox violations) are logged as warnings and the original
///         request is forwarded unchanged.
///     </para>
///     <para>
///         For the <b>response phase</b> the rule runs at priority 10 000 — before the breakpoint
///         rule at 20 000 — so the user sees the script-projected response when the breakpoint
///         pauses. A <see cref="ResponsePipelineAction.ModifyResponse" /> is returned when the
///         script mutates the response; <see langword="null" /> otherwise. Scripting failures are
///         also logged as warnings.
///     </para>
/// </summary>
public sealed class ScriptingRule : IAsyncRequestPhaseRule, IAsyncResponsePhaseRule
{
    private const int RequestPhasePriority = 20_000;
    private const string RequestScriptErrorLogPrefix = "Scripting request-phase hook reported failure";
    private const int ResponsePhasePriority = 10_000;
    private const string ResponseScriptErrorLogPrefix = "Scripting response-phase hook reported failure";
    private readonly IScriptingHandler _handler;
    private readonly ILogger<ScriptingRule> _logger;

    /// <summary>
    ///     Initializes a new <see cref="ScriptingRule" />.
    /// </summary>
    /// <param name="handler">The scripting handler that runs user scripts against each flow.</param>
    /// <param name="logger">The logger used to surface non-fatal scripting errors.</param>
    public ScriptingRule(IScriptingHandler handler, ILogger<ScriptingRule> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    int IAsyncRequestPhaseRule.Priority => RequestPhasePriority;

    int IAsyncResponsePhaseRule.Priority => ResponsePhasePriority;

    /// <inheritdoc />
    public async Task<RequestPipelineAction?> EvaluateRequestAsync(
        HypertextTransferProtocolRequestData request,
        string flowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _handler.ApplyRequestAsync(flowId, request, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "{Prefix} {ErrorCode}: {ErrorMessage}; continuing with unmodified request",
                    RequestScriptErrorLogPrefix,
                    result.Error!.Code,
                    result.Error.Message);
                return null;
            }

            var modified = result.Value;
            if (ReferenceEquals(modified, request))
            {
                return null;
            }

            return new RequestPipelineAction.ModifyRequest(modified);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scripting request-phase hook threw; continuing with unmodified request");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ResponsePipelineAction?> EvaluateResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        string flowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _handler.ApplyResponseAsync(flowId, request, response, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "{Prefix} {ErrorCode}: {ErrorMessage}; continuing with unmodified response",
                    ResponseScriptErrorLogPrefix,
                    result.Error!.Code,
                    result.Error.Message);
                return null;
            }

            var modified = result.Value;
            if (ReferenceEquals(modified, response))
            {
                return null;
            }

            return new ResponsePipelineAction.ModifyResponse(modified);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scripting response-phase hook threw; continuing with unmodified response");
            return null;
        }
    }

    /// <inheritdoc cref="IAsyncRequestPhaseRule.IsEnabled" />
    public bool IsEnabled => true;
}
