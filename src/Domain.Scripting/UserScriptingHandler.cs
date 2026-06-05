using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Default <see cref="IScriptingHandler" /> implementation that runs the active script
///     from a <see cref="MutableScriptingConfiguration" /> against each flow, projecting the
///     resulting <see cref="ScriptableRequest" />/<see cref="ScriptableResponse" /> back onto
///     immutable domain data.
/// </summary>
public sealed class UserScriptingHandler : IScriptingHandler
{
    private const string RequestScriptErrorCode = "SCRIPT_REQUEST_FAILED";
    private const string ResponseScriptErrorCode = "SCRIPT_RESPONSE_FAILED";
    private readonly MutableScriptingConfiguration _configuration;
    private readonly ConcurrentDictionary<ScriptSharedStateKey, IDictionary<string, object?>> _sharedStatesByFlow;

    /// <summary>
    ///     Initializes a new <see cref="UserScriptingHandler" />.
    /// </summary>
    /// <param name="configuration">The scripting configuration to consult.</param>
    public UserScriptingHandler(MutableScriptingConfiguration configuration)
    {
        _configuration = configuration;
        var sharedStatesByFlow = new ConcurrentDictionary<ScriptSharedStateKey, IDictionary<string, object?>>();
        _sharedStatesByFlow = sharedStatesByFlow;
        _configuration.Changed += OnConfigurationChanged;
    }

    /// <inheritdoc />
    public async Task<Result<HypertextTransferProtocolRequestData>> ApplyRequestAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            return Result.Success(request);
        }

        var script = _configuration.ActiveScript;
        if (script is null || !script.IsRequestPhaseEnabled)
        {
            return Result.Success(request);
        }

        var view = new ScriptableRequest(request);
        var sharedState = GetOrCreateSharedState(flowId, script);
        try
        {
            await script.OnRequestAsync(view, sharedState, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _sharedStatesByFlow.TryRemove(flowId, out _);
            throw;
        }
        catch (Exception ex)
        {
            _sharedStatesByFlow.TryRemove(flowId, out _);
            var error = new ScriptError(RequestScriptErrorCode, ex.Message);
            return Result.Failure<HypertextTransferProtocolRequestData>(error);
        }

        var projection = ScriptableProjector.Project(view, request);
        if (!projection.IsSuccess)
        {
            _sharedStatesByFlow.TryRemove(flowId, out _);
            throw new InvalidOperationException(
                $"Script request projection failed ({projection.Error!.Code}): {projection.Error!.Message}");
        }

        return Result.Success(projection.Value);
    }

    /// <inheritdoc />
    public async Task<Result<HypertextTransferProtocolResponseData>> ApplyResponseAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            return Result.Success(response);
        }

        var script = _configuration.ActiveScript;
        if (script is null || !script.IsResponsePhaseEnabled)
        {
            return Result.Success(response);
        }

        var requestView = new ScriptableRequest(request);
        var responseView = new ScriptableResponse(response);
        var sharedState = GetOrCreateSharedState(flowId, script);
        try
        {
            await script.OnResponseAsync(requestView, responseView, sharedState, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            RemoveSharedState(flowId, script);
            throw;
        }
        catch (Exception ex)
        {
            RemoveSharedState(flowId, script);
            var error = new ScriptError(ResponseScriptErrorCode, ex.Message);
            return Result.Failure<HypertextTransferProtocolResponseData>(error);
        }

        RemoveSharedState(flowId, script);
        var projection = ScriptableProjector.Project(responseView, response);
        if (!projection.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Script response projection failed ({projection.Error!.Code}): {projection.Error!.Message}");
        }

        return Result.Success(projection.Value);
    }

    private IDictionary<string, object?> GetOrCreateSharedState(string flowId, IUserScript script)
    {
        var key = new ScriptSharedStateKey
        {
            FlowId = flowId,
            Script = script,
        };
        var sharedState = _sharedStatesByFlow.GetOrAdd(key, UserScriptingHandlerHelpers.CreateSharedState);
        return sharedState;
    }

    private void OnConfigurationChanged()
    {
        _sharedStatesByFlow.Clear();
    }

    private void RemoveSharedState(string flowId, IUserScript script)
    {
        var key = new ScriptSharedStateKey
        {
            FlowId = flowId,
            Script = script,
        };
        _sharedStatesByFlow.TryRemove(key, out _);
    }
}
