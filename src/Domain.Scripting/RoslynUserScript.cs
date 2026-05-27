using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     <see cref="IUserScript" /> implementation that wraps Roslyn-compiled
///     <see cref="Script{T}" /> instances for the request- and response-phase script bodies.
/// </summary>
public sealed class RoslynUserScript : IUserScript
{
    private readonly Script<object>? _requestScript;
    private readonly Script<object>? _responseScript;

    /// <summary>
    ///     Initializes a new <see cref="RoslynUserScript" />.
    /// </summary>
    /// <param name="displayName">The script's friendly display name.</param>
    /// <param name="requestScript">The compiled request-phase script (may be <see langword="null" />).</param>
    /// <param name="responseScript">The compiled response-phase script (may be <see langword="null" />).</param>
    public RoslynUserScript(string displayName, Script<object>? requestScript, Script<object>? responseScript)
    {
        DisplayName = displayName;
        _requestScript = requestScript;
        _responseScript = responseScript;
        IsRequestPhaseEnabled = requestScript is not null;
        IsResponsePhaseEnabled = responseScript is not null;
    }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsRequestPhaseEnabled { get; }

    /// <inheritdoc />
    public bool IsResponsePhaseEnabled { get; }

    /// <inheritdoc />
    public async Task OnRequestAsync(
        ScriptableRequest request,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        if (_requestScript is null)
        {
            return;
        }

        var globals = new RequestScriptGlobals
        {
            Request = request,
            SharedState = sharedState,
        };
        await _requestScript.RunAsync(globals, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task OnResponseAsync(
        ScriptableRequest request,
        ScriptableResponse response,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        if (_responseScript is null)
        {
            return;
        }

        var globals = new ResponseScriptGlobals
        {
            Request = request,
            Response = response,
            SharedState = sharedState,
        };
        await _responseScript.RunAsync(globals, cancellationToken).ConfigureAwait(false);
    }
}
