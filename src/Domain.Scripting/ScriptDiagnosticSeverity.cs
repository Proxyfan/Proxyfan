namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Severity of a single <see cref="ScriptDiagnostic" /> entry reported by the compiler.
/// </summary>
public enum ScriptDiagnosticSeverity
{
    /// <summary>
    ///     Informational diagnostic — never blocks compilation.
    /// </summary>
    Information = 0,

    /// <summary>
    ///     Warning diagnostic — does not block compilation by itself.
    /// </summary>
    Warning = 1,

    /// <summary>
    ///     Error diagnostic — compilation fails when any error is present.
    /// </summary>
    Error = 2,
}
