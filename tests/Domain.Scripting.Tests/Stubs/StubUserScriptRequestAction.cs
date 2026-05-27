using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting.Tests.Stubs;

/// <summary>
///     Delegate shape for the request-phase action of <see cref="StubUserScript" />.
/// </summary>
/// <param name="request">The scriptable request view to mutate.</param>
/// <param name="sharedState">The flow-scoped shared state dictionary.</param>
public delegate void StubUserScriptRequestAction(
    ScriptableRequest request,
    IDictionary<string, object?> sharedState);
