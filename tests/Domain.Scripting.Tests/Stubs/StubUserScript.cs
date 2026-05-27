using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests.Stubs;

/// <summary>
///     Hand-written <see cref="IUserScript" /> stub used to drive
///     <see cref="UserScriptingHandler" /> tests without invoking Roslyn.
/// </summary>
public sealed class StubUserScript : IUserScript
{
    private readonly StubUserScriptRequestAction? _onRequest;
    private readonly StubUserScriptResponseAction? _onResponse;

    /// <summary>
    ///     Initializes a new <see cref="StubUserScript" />.
    /// </summary>
    /// <param name="displayName">The script's display name.</param>
    /// <param name="onRequest">Optional request-phase action. When <see langword="null" />, request phase is disabled.</param>
    /// <param name="onResponse">Optional response-phase action. When <see langword="null" />, response phase is disabled.</param>
    public StubUserScript(
        string displayName,
        StubUserScriptRequestAction? onRequest = null,
        StubUserScriptResponseAction? onResponse = null)
    {
        DisplayName = displayName;
        _onRequest = onRequest;
        _onResponse = onResponse;
        IsRequestPhaseEnabled = onRequest is not null;
        IsResponsePhaseEnabled = onResponse is not null;
    }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsRequestPhaseEnabled { get; }

    /// <inheritdoc />
    public bool IsResponsePhaseEnabled { get; }

    /// <inheritdoc />
    public Task OnRequestAsync(
        ScriptableRequest request,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        _onRequest?.Invoke(request, sharedState);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnResponseAsync(
        ScriptableRequest request,
        ScriptableResponse response,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        _onResponse?.Invoke(request, response, sharedState);
        return Task.CompletedTask;
    }
}
