namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Composite dictionary key that associates a per-flow shared state dictionary with both
///     the flow identifier and the <see cref="IUserScript" /> instance that owns it.
///     Keying on the script reference ensures that a new compilation always receives a fresh
///     shared state, preventing state written by a previous script version from leaking into a
///     subsequent one.
/// </summary>
public readonly record struct ScriptSharedStateKey
{
    /// <summary>
    ///     Gets the proxy flow identifier.
    /// </summary>
    public string FlowId { get; init; }

    /// <summary>
    ///     Gets the compiled script instance that the shared state belongs to.
    /// </summary>
    public IUserScript Script { get; init; }
}
