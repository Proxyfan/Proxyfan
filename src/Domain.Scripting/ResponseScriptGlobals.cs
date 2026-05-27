using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Globals object made available to a user's response-phase script. Script code can read
///     and mutate <see cref="Response" /> directly, read the (already-completed) <see cref="Request" />,
///     and use <see cref="SharedState" /> populated by the request-phase script for this flow.
/// </summary>
public sealed class ResponseScriptGlobals
{
    /// <summary>
    ///     Gets the read-side request view that triggered this response.
    /// </summary>
    public required ScriptableRequest Request { get; init; }

    /// <summary>
    ///     Gets the mutable response view. Mutations are projected back onto the outgoing response.
    /// </summary>
    public required ScriptableResponse Response { get; init; }

    /// <summary>
    ///     Gets a flow-scoped dictionary shared with the request-phase script for the same flow.
    /// </summary>
    public required IDictionary<string, object?> SharedState { get; init; }
}
