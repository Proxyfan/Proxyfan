using Proxyfan.Domain.Traffic;
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
    private readonly MutableScriptingConfiguration _configuration;
    private readonly ConcurrentDictionary<string, IDictionary<string, object?>> _sharedStatesByFlow;

    /// <summary>
    ///     Initializes a new <see cref="UserScriptingHandler" />.
    /// </summary>
    /// <param name="configuration">The scripting configuration to consult.</param>
    public UserScriptingHandler(MutableScriptingConfiguration configuration)
    {
        _configuration = configuration;
        var sharedStatesByFlow = new ConcurrentDictionary<string, IDictionary<string, object?>>();
        _sharedStatesByFlow = sharedStatesByFlow;
    }

    /// <inheritdoc />
    public async Task<HypertextTransferProtocolRequestData> ApplyRequestAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            return request;
        }

        var script = _configuration.ActiveScript;
        if (script is null || !script.IsRequestPhaseEnabled)
        {
            return request;
        }

        var view = new ScriptableRequest(request);
        var sharedState = GetOrCreateSharedState(flowId);
        await script.OnRequestAsync(view, sharedState, cancellationToken).ConfigureAwait(false);
        var projection = ScriptableProjector.Project(view, request);
        if (!projection.IsSuccess)
        {
            return request;
        }

        return projection.Value;
    }

    /// <inheritdoc />
    public async Task<HypertextTransferProtocolResponseData> ApplyResponseAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            return response;
        }

        var script = _configuration.ActiveScript;
        if (script is null || !script.IsResponsePhaseEnabled)
        {
            return response;
        }

        var requestView = new ScriptableRequest(request);
        var responseView = new ScriptableResponse(response);
        var sharedState = GetOrCreateSharedState(flowId);
        try
        {
            await script.OnResponseAsync(requestView, responseView, sharedState, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sharedStatesByFlow.TryRemove(flowId, out _);
        }

        var projection = ScriptableProjector.Project(responseView, response);
        if (!projection.IsSuccess)
        {
            return response;
        }

        return projection.Value;
    }

    private IDictionary<string, object?> GetOrCreateSharedState(string flowId)
    {
        var sharedState = _sharedStatesByFlow.GetOrAdd(flowId, UserScriptingHandlerHelpers.CreateSharedState);
        return sharedState;
    }
}
