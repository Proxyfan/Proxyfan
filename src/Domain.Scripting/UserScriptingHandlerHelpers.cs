using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Static helpers used by <see cref="UserScriptingHandler" />.
/// </summary>
public static class UserScriptingHandlerHelpers
{
    /// <summary>
    ///     Factory used to allocate a fresh per-flow shared state dictionary on first access.
    /// </summary>
    /// <param name="flowId">The flow identifier (unused; required by the delegate shape).</param>
    /// <returns>An empty mutable dictionary.</returns>
    public static IDictionary<string, object?> CreateSharedState(string flowId)
    {
        _ = flowId;
        var sharedState = new Dictionary<string, object?>();
        return sharedState;
    }
}
