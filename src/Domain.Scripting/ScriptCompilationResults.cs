using System.Collections.Generic;
using System.Collections.Immutable;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Static factory helpers that create <see cref="ScriptCompilationResult" /> instances
///     for both success and failure cases.
/// </summary>
public static class ScriptCompilationResults
{
    /// <summary>
    ///     Initializes a failed compilation result.
    /// </summary>
    /// <param name="diagnostics">The diagnostics (must contain at least one error).</param>
    /// <returns>A failed <see cref="ScriptCompilationResult" />.</returns>
    public static ScriptCompilationResult Failure(IEnumerable<ScriptDiagnostic> diagnostics)
    {
        var snapshot = ImmutableArray.CreateRange(diagnostics);
        var result = new ScriptCompilationResult(false, null, snapshot);
        return result;
    }

    /// <summary>
    ///     Initializes a successful compilation result.
    /// </summary>
    /// <param name="script">The compiled script.</param>
    /// <param name="diagnostics">The non-error diagnostics.</param>
    /// <returns>A successful <see cref="ScriptCompilationResult" />.</returns>
    public static ScriptCompilationResult Success(IUserScript script, IEnumerable<ScriptDiagnostic> diagnostics)
    {
        var snapshot = ImmutableArray.CreateRange(diagnostics);
        var result = new ScriptCompilationResult(true, script, snapshot);
        return result;
    }
}
