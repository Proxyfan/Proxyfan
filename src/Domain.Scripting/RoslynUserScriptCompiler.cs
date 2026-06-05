using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Default <see cref="IUserScriptCompiler" /> backed by Roslyn
///     (<c>Microsoft.CodeAnalysis.CSharp.Scripting</c>). Produces a
///     <see cref="RoslynUserScript" /> that holds the compiled request- and response-phase
///     scripts.
/// </summary>
public sealed class RoslynUserScriptCompiler : IUserScriptCompiler
{
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
    ///     Compiles a single script phase and appends Roslyn diagnostics plus
    ///     sandbox scanner findings.
    /// </summary>
    /// <param name="source">The source text for this phase.</param>
    /// <param name="globalsType">The globals type exposed to the script.</param>
    /// <param name="diagnostics">The destination diagnostic list.</param>
    /// <param name="isRequestPhase">When true this is request-phase compilation; otherwise response-phase.</param>
    /// <returns>The compiled Roslyn script handle.</returns>
    private Script<object> CompilePhase(
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
        var phaseDiagnostics = compiled.Compile();
        RoslynUserScriptCompilerHelpers.AppendDiagnostics(diagnostics, phaseDiagnostics, isRequestPhase);
        var compilation = compiled.GetCompilation();
        foreach (var tree in compilation.SyntaxTrees)
        {
            ScriptForbiddenNamespaceScanner.Append(diagnostics, tree, isRequestPhase);
            ScriptForbiddenDirectiveScanner.Append(diagnostics, tree, isRequestPhase);
        }

        return compiled;
    }
}
