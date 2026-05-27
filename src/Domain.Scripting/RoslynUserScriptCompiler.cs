using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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
    /// <inheritdoc />
    public ScriptCompilationResult Compile(string displayName, string requestScript, string responseScript)
    {
        var options = RoslynUserScriptCompilerHelpers.BuildDefaultOptions();
        var diagnostics = new List<ScriptDiagnostic>();
        Script<object>? compiledRequest = null;
        Script<object>? compiledResponse = null;

        if (!string.IsNullOrWhiteSpace(requestScript))
        {
            compiledRequest = CSharpScript.Create<object>(
                requestScript,
                options,
                globalsType: typeof(RequestScriptGlobals));
            var requestDiagnostics = compiledRequest.Compile();
            RoslynUserScriptCompilerHelpers.AppendDiagnostics(diagnostics, requestDiagnostics, isRequestPhase: true);
        }

        if (!string.IsNullOrWhiteSpace(responseScript))
        {
            compiledResponse = CSharpScript.Create<object>(
                responseScript,
                options,
                globalsType: typeof(ResponseScriptGlobals));
            var responseDiagnostics = compiledResponse.Compile();
            RoslynUserScriptCompilerHelpers.AppendDiagnostics(diagnostics, responseDiagnostics, isRequestPhase: false);
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

        var script = new RoslynUserScript(displayName, compiledRequest, compiledResponse);
        return ScriptCompilationResults.Success(script, diagnostics);
    }
}
