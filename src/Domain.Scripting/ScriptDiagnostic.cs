namespace Proxyfan.Domain.Scripting;

/// <summary>
///     A single diagnostic emitted while compiling a user script — typically a syntax or
///     semantic error from the Roslyn compiler, surfaced for display in the script editor UI.
/// </summary>
public sealed record ScriptDiagnostic
{
    /// <summary>
    ///     Gets the one-based column number on <see cref="Line" /> where the issue starts.
    /// </summary>
    public int Column { get; }

    /// <summary>
    ///     Gets the Roslyn diagnostic identifier (e.g. <c>"CS1002"</c>) or an empty string if
    ///     none was supplied.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Gets the one-based line number where the issue starts.
    /// </summary>
    public int Line { get; }

    /// <summary>
    ///     Gets the human-readable diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets the diagnostic severity.
    /// </summary>
    public ScriptDiagnosticSeverity Severity { get; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptDiagnostic" />.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="id">The Roslyn diagnostic identifier.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="line">The one-based line number.</param>
    /// <param name="column">The one-based column number.</param>
    public ScriptDiagnostic(ScriptDiagnosticSeverity severity, string id, string message, int line, int column)
    {
        Severity = severity;
        Id = id;
        Message = message;
        Line = line;
        Column = column;
    }
}
