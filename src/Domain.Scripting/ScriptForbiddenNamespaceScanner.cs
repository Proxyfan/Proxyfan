using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Defence-in-depth scanner that walks a user script's parsed
///     <see cref="SyntaxTree" /> and emits <see cref="ScriptDiagnostic" /> errors when the
///     script references any of the namespaces or types that the sandbox forbids
///     (see <c>.github/instructions/scripting-sandbox.instructions.md</c>). The scan
///     covers fully-qualified type references in declarations / object creations,
///     qualified member-access expressions of the form <c>System.Foo.Bar.Member</c>, and
///     <c>using</c> directives. It is purely lexical, so it is robust to assemblies that
///     the Roslyn interactive loader exposes transitively from the host process and that
///     a curated <see cref="Microsoft.CodeAnalysis.Scripting.ScriptOptions" /> reference
///     list cannot block. It does not attempt to defeat deliberate evasion such as type
///     aliases or reflection — those are mitigated by the per-invocation timeout and
///     allocation ceiling.
/// </summary>
public static class ScriptForbiddenNamespaceScanner
{
    /// <summary>
    ///     The diagnostic identifier surfaced for every forbidden-namespace finding.
    /// </summary>
    public const string DiagnosticId = "PROXYFAN_SANDBOX_FORBIDDEN";
    private static readonly string[] ForbiddenPrefixes;

    static ScriptForbiddenNamespaceScanner()
    {
        ForbiddenPrefixes =
        [
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Net.WebSockets",
            "System.IO.File",
            "System.IO.Directory",
            "System.IO.FileStream",
            "System.IO.FileSystemWatcher",
            "System.IO.StreamReader",
            "System.IO.StreamWriter",
            "System.Diagnostics.Process",
            "System.Reflection.Emit",
            "System.Reflection.Assembly",
            "System.Runtime.Loader",
            "System.Runtime.InteropServices",
            "System.Activator",
            "System.Environment",
        ];
    }

    /// <summary>
    ///     Appends a <see cref="ScriptDiagnostic" /> with severity
    ///     <see cref="ScriptDiagnosticSeverity.Error" /> to <paramref name="destination" />
    ///     for every occurrence in <paramref name="tree" /> of a namespace, type, or
    ///     member-access chain whose dotted path matches one of the sandbox-forbidden
    ///     prefixes. Each diagnostic is tagged <c>[request]</c> or <c>[response]</c>
    ///     depending on <paramref name="isRequestPhase" />.
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

        var root = tree.GetRoot();
        foreach (var descendant in root.DescendantNodes())
        {
            var candidatePath = ExtractCandidatePath(descendant);
            if (candidatePath is null)
            {
                continue;
            }

            var matchedPrefix = MatchForbiddenPrefix(candidatePath);
            if (matchedPrefix is null)
            {
                continue;
            }

            destination.Add(BuildDiagnostic(descendant, matchedPrefix, phaseLabel));
        }
    }

    private static ScriptDiagnostic BuildDiagnostic(SyntaxNode node, string matchedPrefix, string phaseLabel)
    {
        var location = node.GetLocation().GetLineSpan();
        var line = location.StartLinePosition.Line + 1;
        var column = location.StartLinePosition.Character + 1;
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "[{0}] Sandbox-forbidden namespace or type referenced: '{1}'.",
            phaseLabel,
            matchedPrefix);
        return new ScriptDiagnostic(
            ScriptDiagnosticSeverity.Error,
            DiagnosticId,
            message,
            line,
            column);
    }

    private static string? ExtractCandidatePath(SyntaxNode node)
    {
        if (node is QualifiedNameSyntax qualifiedName)
        {
            return qualifiedName.ToString();
        }

        if (node is UsingDirectiveSyntax usingDirective && usingDirective.Name is not null)
        {
            return usingDirective.Name.ToString();
        }

        if (node is MemberAccessExpressionSyntax memberAccess
            && memberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression
            && HasOnlyIdentifierChain(memberAccess))
        {
            return memberAccess.ToString();
        }

        return null;
    }

    private static bool HasOnlyIdentifierChain(MemberAccessExpressionSyntax memberAccess)
    {
        var expression = memberAccess.Expression;
        while (expression is MemberAccessExpressionSyntax inner
            && inner.Kind() == SyntaxKind.SimpleMemberAccessExpression)
        {
            expression = inner.Expression;
        }

        return expression is IdentifierNameSyntax;
    }

    private static string? MatchForbiddenPrefix(string candidatePath)
    {
        foreach (var prefix in ForbiddenPrefixes)
        {
            if (string.Equals(candidatePath, prefix, StringComparison.Ordinal))
            {
                return prefix;
            }

            if (candidatePath.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                return prefix;
            }
        }

        return null;
    }
}
