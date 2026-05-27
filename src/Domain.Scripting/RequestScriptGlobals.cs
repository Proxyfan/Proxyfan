using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Globals object made available to a user's request-phase script. Script code can read
///     and mutate <see cref="Request" /> directly, and use <see cref="SharedState" /> to
///     persist values that the response-phase script will see for the same flow.
/// </summary>
public sealed class RequestScriptGlobals
{
    /// <summary>
    ///     Gets the mutable request view. Mutations are projected back onto the outgoing request.
    /// </summary>
    public required ScriptableRequest Request { get; init; }

    /// <summary>
    ///     Gets a flow-scoped dictionary that is visible to both the request- and response-phase
    ///     scripts for the same captured flow.
    /// </summary>
    public required IDictionary<string, object?> SharedState { get; init; }
}
