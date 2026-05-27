using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Defines a compiled user script that can mutate requests and responses as they pass
///     through the proxy pipeline.
/// </summary>
public interface IUserScript
{
    /// <summary>
    ///     Gets the script's friendly display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     Executes the script's request-phase logic against the supplied request view.
    /// </summary>
    /// <param name="request">The request view to mutate.</param>
    /// <param name="cancellationToken">A token that cancels execution.</param>
    /// <returns>A task that completes when the script has run.</returns>
    Task OnRequestAsync(ScriptableRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///     Executes the script's response-phase logic against the supplied response view.
    /// </summary>
    /// <param name="request">The request that triggered the response.</param>
    /// <param name="response">The response view to mutate.</param>
    /// <param name="cancellationToken">A token that cancels execution.</param>
    /// <returns>A task that completes when the script has run.</returns>
    Task OnResponseAsync(ScriptableRequest request, ScriptableResponse response, CancellationToken cancellationToken);
}
