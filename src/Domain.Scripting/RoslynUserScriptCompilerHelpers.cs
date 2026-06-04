using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Static helpers used by <see cref="RoslynUserScriptCompiler" /> to construct script
///     options and translate Roslyn diagnostics into <see cref="ScriptDiagnostic" /> entries.
/// </summary>
public static class RoslynUserScriptCompilerHelpers
{
    /// <summary>
    ///     Appends translated entries for each non-hidden Roslyn diagnostic in
    ///     <paramref name="source" /> to <paramref name="destination" />, prefixing each
    ///     message with a <c>[request]</c> or <c>[response]</c> tag.
    /// </summary>
    /// <param name="destination">The list to append to.</param>
    /// <param name="source">The Roslyn diagnostics.</param>
    /// <param name="isRequestPhase">When true the messages are tagged <c>[request]</c>; otherwise <c>[response]</c>.</param>
    public static void AppendDiagnostics(
        List<ScriptDiagnostic> destination,
        IEnumerable<Diagnostic> source,
        bool isRequestPhase)
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

        foreach (var diagnostic in source)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Hidden)
            {
                continue;
            }

            var severity = MapSeverity(diagnostic.Severity);
            var location = diagnostic.Location.GetLineSpan();
            var line = location.StartLinePosition.Line + 1;
            var column = location.StartLinePosition.Character + 1;
            var rawMessage = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            var message = $"[{phaseLabel}] {rawMessage}";
            var translated = new ScriptDiagnostic(severity, diagnostic.Id, message, line, column);
            destination.Add(translated);
        }
    }

    /// <summary>
    ///     Builds the default <see cref="ScriptOptions" /> used to compile every user script,
    ///     including the references that expose the scripting API and the implicit usings.
    /// </summary>
    /// <returns>The default <see cref="ScriptOptions" /> instance.</returns>
    public static ScriptOptions BuildDefaultOptions()
    {
        var references = new[]
        {
            typeof(object).Assembly,
            typeof(List<>).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(ScriptableRequest).Assembly,
        };
        var imports = new[]
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "Proxyfan.Domain.Scripting",
        };
        var options = ScriptOptions.Default
            .WithReferences(references)
            .WithImports(imports)
            .WithEmitDebugInformation(false);
        return options;
    }

    /// <summary>
    ///     Maps a Roslyn <see cref="DiagnosticSeverity" /> to a Proxyfan
    ///     <see cref="ScriptDiagnosticSeverity" />.
    /// </summary>
    /// <param name="severity">The Roslyn severity.</param>
    /// <returns>The mapped Proxyfan severity.</returns>
    public static ScriptDiagnosticSeverity MapSeverity(DiagnosticSeverity severity)
    {
        if (severity == DiagnosticSeverity.Error)
        {
            return ScriptDiagnosticSeverity.Error;
        }

        if (severity == DiagnosticSeverity.Warning)
        {
            return ScriptDiagnosticSeverity.Warning;
        }

        return ScriptDiagnosticSeverity.Information;
    }
}
