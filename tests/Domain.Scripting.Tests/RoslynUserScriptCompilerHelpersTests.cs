using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for the pure helper methods on <see cref="RoslynUserScriptCompilerHelpers" />
///     that translate Roslyn diagnostics and severities into the Proxyfan scripting model.
/// </summary>
public sealed class RoslynUserScriptCompilerHelpersTests
{
    /// <summary>
    ///     Verifies that <see cref="DiagnosticSeverity.Error" /> maps to the Error Proxyfan severity.
    /// </summary>
    [Test]
    public async Task MapSeverity_Error_ReturnsError()
    {
        var result = RoslynUserScriptCompilerHelpers.MapSeverity(DiagnosticSeverity.Error);

        await Assert.That(result).IsEqualTo(ScriptDiagnosticSeverity.Error);
    }

    /// <summary>
    ///     Verifies that <see cref="DiagnosticSeverity.Warning" /> maps to the Warning Proxyfan severity.
    /// </summary>
    [Test]
    public async Task MapSeverity_Warning_ReturnsWarning()
    {
        var result = RoslynUserScriptCompilerHelpers.MapSeverity(DiagnosticSeverity.Warning);

        await Assert.That(result).IsEqualTo(ScriptDiagnosticSeverity.Warning);
    }

    /// <summary>
    ///     Verifies that <see cref="DiagnosticSeverity.Info" /> maps to the Information Proxyfan severity.
    /// </summary>
    [Test]
    public async Task MapSeverity_Info_ReturnsInformation()
    {
        var result = RoslynUserScriptCompilerHelpers.MapSeverity(DiagnosticSeverity.Info);

        await Assert.That(result).IsEqualTo(ScriptDiagnosticSeverity.Information);
    }

    /// <summary>
    ///     Verifies that a Hidden Roslyn diagnostic is skipped by <c>AppendDiagnostics</c>,
    ///     exercising the early-continue branch.
    /// </summary>
    [Test]
    public async Task AppendDiagnostics_HiddenSeverity_IsSkipped()
    {
        var destination = new List<ScriptDiagnostic>();
        var hidden = CreateDiagnostic(DiagnosticSeverity.Hidden);
        var warning = CreateDiagnostic(DiagnosticSeverity.Warning);

        RoslynUserScriptCompilerHelpers.AppendDiagnostics(destination, new[] { hidden, warning }, isRequestPhase: false);

        await Assert.That(destination.Count).IsEqualTo(1);
        await Assert.That(destination[0].Severity).IsEqualTo(ScriptDiagnosticSeverity.Warning);
        await Assert.That(destination[0].Message).StartsWith("[response]");
    }

    /// <summary>
    ///     Verifies that the request-phase label is applied when <c>isRequestPhase</c> is true.
    /// </summary>
    [Test]
    public async Task AppendDiagnostics_RequestPhase_TagsMessageWithRequestLabel()
    {
        var destination = new List<ScriptDiagnostic>();
        var warning = CreateDiagnostic(DiagnosticSeverity.Warning);

        RoslynUserScriptCompilerHelpers.AppendDiagnostics(destination, new[] { warning }, isRequestPhase: true);

        await Assert.That(destination.Count).IsEqualTo(1);
        await Assert.That(destination[0].Message).StartsWith("[request]");
    }

    /// <summary>
    ///     Verifies that the Roslyn diagnostic identifier is preserved on the projected
    ///     <see cref="ScriptDiagnostic.Id" /> for downstream tooling.
    /// </summary>
    [Test]
    public async Task AppendDiagnostics_DiagnosticId_IsPreservedOnProjection()
    {
        var destination = new List<ScriptDiagnostic>();
        var warning = CreateDiagnostic(DiagnosticSeverity.Warning);

        RoslynUserScriptCompilerHelpers.AppendDiagnostics(destination, new[] { warning }, isRequestPhase: false);

        await Assert.That(destination[0].Id).IsEqualTo(warning.Id);
    }

    /// <summary>
    ///     Verifies that <see cref="RoslynUserScriptCompilerHelpers.BuildDefaultOptions" /> returns
    ///     a non-null configuration including the Proxyfan.Domain.Scripting import.
    /// </summary>
    [Test]
    public async Task BuildDefaultOptions_Always_IncludesScriptingImport()
    {
        var options = RoslynUserScriptCompilerHelpers.BuildDefaultOptions();

        await Assert.That(options).IsNotNull();
        await Assert.That(options.Imports.Contains("Proxyfan.Domain.Scripting")).IsTrue();
    }

    private static Diagnostic CreateDiagnostic(DiagnosticSeverity severity)
    {
        var defaultSeverity = severity == DiagnosticSeverity.Hidden ? DiagnosticSeverity.Hidden : severity;
        var descriptor = new DiagnosticDescriptor(
            "TEST001",
            "Test",
            "Test message",
            "Tests",
            defaultSeverity,
            isEnabledByDefault: true);
        return Diagnostic.Create(descriptor, Location.None);
    }
}
