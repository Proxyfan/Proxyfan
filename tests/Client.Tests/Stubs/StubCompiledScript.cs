using Proxyfan.Domain.Scripting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Minimal hand-written <see cref="IUserScript" /> stub used in
///     <see cref="Proxyfan.Client.Tools.ViewModels.ScriptingViewModel" /> tests.
/// </summary>
public sealed class StubCompiledScript : IUserScript
{
    /// <summary>
    ///     Initializes a new <see cref="StubCompiledScript" />.
    /// </summary>
    /// <param name="displayName">The display name assigned by the compiler.</param>
    public StubCompiledScript(string displayName)
    {
        DisplayName = displayName;
    }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsRequestPhaseEnabled => false;

    /// <inheritdoc />
    public bool IsResponsePhaseEnabled => false;

    /// <inheritdoc />
    public Task OnRequestAsync(
        ScriptableRequest request,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnResponseAsync(
        ScriptableRequest request,
        ScriptableResponse response,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
