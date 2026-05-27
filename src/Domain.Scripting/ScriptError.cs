namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Categorizes scripting failures (compilation, runtime, timeout, sandbox violation).
/// </summary>
public sealed record ScriptError : DomainError
{
    /// <summary>
    ///     Initializes a new <see cref="ScriptError" />.
    /// </summary>
    /// <param name="code">The stable, machine-readable error code.</param>
    /// <param name="message">The human-readable error description.</param>
    public ScriptError(string code, string message)
        : base(code, message)
    {
    }
}
