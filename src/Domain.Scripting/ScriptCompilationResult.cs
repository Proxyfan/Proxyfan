using System.Collections.Immutable;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Outcome of compiling a user script — either a runnable <see cref="IUserScript" />
///     together with any non-error diagnostics, or a list of compilation errors.
/// </summary>
public sealed class ScriptCompilationResult
{
    /// <summary>
    ///     Gets the diagnostics emitted by the compiler (informational, warnings, errors).
    /// </summary>
    public ImmutableArray<ScriptDiagnostic> Diagnostics { get; }

    /// <summary>
    ///     Gets a value indicating whether compilation succeeded (no errors were reported).
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the compiled script when <see cref="IsSuccess" /> is true; <see langword="null" /> otherwise.
    /// </summary>
    public IUserScript? Script { get; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptCompilationResult" />.
    /// </summary>
    /// <param name="isSuccess">Whether compilation succeeded.</param>
    /// <param name="script">The compiled script (non-null only when <paramref name="isSuccess" /> is <see langword="true" />).</param>
    /// <param name="diagnostics">The diagnostics emitted during compilation.</param>
    public ScriptCompilationResult(bool isSuccess, IUserScript? script, ImmutableArray<ScriptDiagnostic> diagnostics)
    {
        IsSuccess = isSuccess;
        Script = script;
        Diagnostics = diagnostics;
    }
}
