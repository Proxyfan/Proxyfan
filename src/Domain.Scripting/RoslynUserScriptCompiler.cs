using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Default <see cref="IUserScriptCompiler" /> backed by Roslyn
///     (<c>Microsoft.CodeAnalysis.CSharp.Scripting</c>). Produces a
///     <see cref="RoslynUserScript" /> that holds the compiled request- and response-phase
///     scripts.
/// </summary>
public sealed class RoslynUserScriptCompiler : IUserScriptCompiler
{
    /// <summary>
    ///     The diagnostic identifier surfaced when a script phase compilation exceeds the
    ///     configured <see cref="ScriptSandboxOptions.CompilationTimeoutSeconds" /> budget.
    /// </summary>
    public const string CompilationTimeoutDiagnosticId = "SCRIPT_COMPILATION_TIMED_OUT";
    private readonly ScriptSandboxOptions _sandboxOptions;

    /// <summary>
    ///     Initializes a new <see cref="RoslynUserScriptCompiler" /> with the default sandbox
    ///     limits (<see cref="ScriptSandboxOptions.Default" />).
    /// </summary>
    public RoslynUserScriptCompiler() : this(ScriptSandboxOptions.Default)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="RoslynUserScriptCompiler" /> with the supplied
    ///     <paramref name="sandboxOptions" />.
    /// </summary>
    /// <param name="sandboxOptions">Resource limits to bake into every compiled script.</param>
    public RoslynUserScriptCompiler(ScriptSandboxOptions sandboxOptions)
    {
        _sandboxOptions = sandboxOptions;
    }

    /// <inheritdoc />
    public ScriptCompilationResult Compile(string displayName, string requestScript, string responseScript)
    {
        var diagnostics = new List<ScriptDiagnostic>();
        Script<object>? compiledRequest = null;
        Script<object>? compiledResponse = null;

        if (!string.IsNullOrWhiteSpace(requestScript))
        {
            compiledRequest = CompilePhase(requestScript, typeof(RequestScriptGlobals), diagnostics, isRequestPhase: true);
        }

        if (!string.IsNullOrWhiteSpace(responseScript))
        {
            compiledResponse = CompilePhase(responseScript, typeof(ResponseScriptGlobals), diagnostics, isRequestPhase: false);
        }

        var hasErrors = false;
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == ScriptDiagnosticSeverity.Error)
            {
                hasErrors = true;
                break;
            }
        }

        if (hasErrors)
        {
            return ScriptCompilationResults.Failure(diagnostics);
        }

        var script = new RoslynUserScript(displayName, compiledRequest, compiledResponse, _sandboxOptions);
        return ScriptCompilationResults.Success(script, diagnostics);
    }

    /// <summary>
    ///     Appends a <see cref="CompilationTimeoutDiagnosticId" /> error diagnostic to
    ///     <paramref name="diagnostics" /> when a phase compilation is cancelled by the timeout.
    /// </summary>
    /// <param name="diagnostics">The destination diagnostic list.</param>
    /// <param name="isRequestPhase">When true the message is tagged <c>[request]</c>; otherwise <c>[response]</c>.</param>
    private void AppendTimeoutDiagnostic(List<ScriptDiagnostic> diagnostics, bool isRequestPhase)
    {
        string phaseLabel;
        if (isRequestPhase)
        {
            phaseLabel = "request";
        }
        else
        {
            phaseLabel = "response";
        }

        var message = string.Format(
            CultureInfo.InvariantCulture,
            "[{0}] Script compilation timed out after {1} second(s).",
            phaseLabel,
            _sandboxOptions.CompilationTimeoutSeconds);
        var timeoutDiagnostic = new ScriptDiagnostic(
            ScriptDiagnosticSeverity.Error,
            CompilationTimeoutDiagnosticId,
            message,
            line: 0,
            column: 0);
        diagnostics.Add(timeoutDiagnostic);
    }

    /// <summary>
    ///     Compiles a single script phase and appends Roslyn diagnostics plus
    ///     sandbox scanner findings.  Returns <see langword="null" /> when
    ///     compilation is cancelled due to the timeout budget, after appending a
    ///     <see cref="CompilationTimeoutDiagnosticId" /> error to
    ///     <paramref name="diagnostics" />.
    /// </summary>
    /// <param name="source">The source text for this phase.</param>
    /// <param name="globalsType">The globals type exposed to the script.</param>
    /// <param name="diagnostics">The destination diagnostic list.</param>
    /// <param name="isRequestPhase">When true this is request-phase compilation; otherwise response-phase.</param>
    /// <returns>The compiled Roslyn script handle, or <see langword="null" /> on timeout.</returns>
    private Script<object>? CompilePhase(
        string source,
        Type globalsType,
        List<ScriptDiagnostic> diagnostics,
        bool isRequestPhase)
    {
        var options = RoslynUserScriptCompilerHelpers.BuildDefaultOptions();
        var compiled = CSharpScript.Create<object>(
            source,
            options,
            globalsType);

        var compilationTimeout = TimeSpan.FromSeconds(_sandboxOptions.CompilationTimeoutSeconds);
        using var cancellationSource = new CancellationTokenSource(compilationTimeout);

        try
        {
            var phaseDiagnostics = compiled.Compile(cancellationSource.Token);
            RoslynUserScriptCompilerHelpers.AppendDiagnostics(diagnostics, phaseDiagnostics, isRequestPhase);
            var compilation = compiled.GetCompilation();
            foreach (var tree in compilation.SyntaxTrees)
            {
                ScriptForbiddenNamespaceScanner.Append(diagnostics, tree, isRequestPhase);
                ScriptForbiddenDirectiveScanner.Append(diagnostics, tree, isRequestPhase);
            }

            return compiled;
        }
        catch (OperationCanceledException)
        {
            AppendTimeoutDiagnostic(diagnostics, isRequestPhase);
            return null;
        }
    }
}
