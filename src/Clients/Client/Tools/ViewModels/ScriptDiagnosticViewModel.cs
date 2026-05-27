using Proxyfan.Domain.Scripting;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only view-model around a single <see cref="ScriptDiagnostic" /> for
///     display in the scripting tool window's diagnostics list.
/// </summary>
public sealed class ScriptDiagnosticViewModel
{
    /// <summary>
    ///     Gets the underlying domain diagnostic.
    /// </summary>
    public ScriptDiagnostic Diagnostic { get; }

    /// <summary>
    ///     Gets a formatted "Line:Column" location string for display.
    /// </summary>
    public string Location { get; }

    /// <summary>
    ///     Gets the diagnostic message text.
    /// </summary>
    public string Message => Diagnostic.Message;

    /// <summary>
    ///     Gets the formatted severity text (e.g. <c>"Error"</c>, <c>"Warning"</c>).
    /// </summary>
    public string Severity { get; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptDiagnosticViewModel" />.
    /// </summary>
    /// <param name="diagnostic">The underlying domain diagnostic.</param>
    public ScriptDiagnosticViewModel(ScriptDiagnostic diagnostic)
    {
        Diagnostic = diagnostic;
        Location = $"L{diagnostic.Line}:{diagnostic.Column}";
        Severity = diagnostic.Severity.ToString();
    }
}
