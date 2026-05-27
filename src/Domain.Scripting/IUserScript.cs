using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     A compiled user script that can mutate requests and responses as they pass through the
///     proxy pipeline.
/// </summary>
public interface IUserScript
{
    /// <summary>
    ///     Gets the script's friendly display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     Gets a value indicating whether the script has compiled request-phase logic.
    /// </summary>
    bool IsRequestPhaseEnabled { get; }

    /// <summary>
    ///     Gets a value indicating whether the script has compiled response-phase logic.
    /// </summary>
    bool IsResponsePhaseEnabled { get; }

    /// <summary>
    ///     Executes the script's request-phase logic against the supplied request view.
    /// </summary>
    /// <param name="request">The request view to mutate.</param>
    /// <param name="sharedState">A flow-scoped dictionary shared with the response phase.</param>
    /// <param name="cancellationToken">A token that cancels execution.</param>
    /// <returns>A task that completes when the script has run.</returns>
    Task OnRequestAsync(
        ScriptableRequest request,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Executes the script's response-phase logic against the supplied response view.
    /// </summary>
    /// <param name="request">The request that triggered the response.</param>
    /// <param name="response">The response view to mutate.</param>
    /// <param name="sharedState">A flow-scoped dictionary shared with the request phase.</param>
    /// <param name="cancellationToken">A token that cancels execution.</param>
    /// <returns>A task that completes when the script has run.</returns>
    Task OnResponseAsync(
        ScriptableRequest request,
        ScriptableResponse response,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken);
}
