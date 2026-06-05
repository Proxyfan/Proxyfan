using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Defence-in-depth scanner that rejects Roslyn script directives capable of
///     loading external assemblies or source files.
/// </summary>
public static class ScriptForbiddenDirectiveScanner
{
    /// <summary>
    ///     The diagnostic identifier surfaced for every forbidden-directive finding.
    /// </summary>
    public const string DiagnosticId = "PROXYFAN_SANDBOX_FORBIDDEN_DIRECTIVE";

    /// <summary>
    ///     Appends a <see cref="ScriptDiagnostic" /> with severity
    ///     <see cref="ScriptDiagnosticSeverity.Error" /> to <paramref name="destination" />
    ///     for each <c>#r</c> or <c>#load</c> directive present in <paramref name="tree" />.
    /// </summary>
    /// <param name="destination">The list to append diagnostics to.</param>
    /// <param name="tree">The parsed script syntax tree.</param>
    /// <param name="isRequestPhase">When true the messages are tagged <c>[request]</c>; otherwise <c>[response]</c>.</param>
    public static void Append(
        List<ScriptDiagnostic> destination,
        SyntaxTree tree,
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

        foreach (var trivia in tree.GetRoot().DescendantTrivia(descendIntoTrivia: true))
        {
            var directiveName = GetForbiddenDirectiveName(trivia);
            if (directiveName is null)
            {
                continue;
            }

            var location = trivia.GetLocation().GetLineSpan();
            var line = location.StartLinePosition.Line + 1;
            var column = location.StartLinePosition.Character + 1;
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "[{0}] Sandbox-forbidden script directive used: '{1}'.",
                phaseLabel,
                directiveName);
            var diagnostic = new ScriptDiagnostic(
                ScriptDiagnosticSeverity.Error,
                DiagnosticId,
                message,
                line,
                column);
            destination.Add(diagnostic);
        }
    }

    private static string? GetForbiddenDirectiveName(SyntaxTrivia trivia)
    {
        if (trivia.IsKind(SyntaxKind.ReferenceDirectiveTrivia))
        {
            return "#r";
        }

        if (trivia.IsKind(SyntaxKind.LoadDirectiveTrivia))
        {
            return "#load";
        }

        return null;
    }
}
