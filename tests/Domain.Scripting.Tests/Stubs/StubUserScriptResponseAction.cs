using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting.Tests.Stubs;

/// <summary>
///     Delegate shape for the response-phase action of <see cref="StubUserScript" />.
/// </summary>
/// <param name="request">The scriptable request view associated with the response.</param>
/// <param name="response">The scriptable response view to mutate.</param>
/// <param name="sharedState">The flow-scoped shared state dictionary.</param>
public delegate void StubUserScriptResponseAction(
    ScriptableRequest request,
    ScriptableResponse response,
    IDictionary<string, object?> sharedState);
