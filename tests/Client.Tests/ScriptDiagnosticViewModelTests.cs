using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Scripting;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ScriptDiagnosticViewModel" />.
/// </summary>
public sealed class ScriptDiagnosticViewModelTests
{
    [Test]
    public async Task Constructor_FromDiagnostic_FormatsLocationAndSeverity()
    {
        var diagnostic = new ScriptDiagnostic(ScriptDiagnosticSeverity.Error, "CS0103", "unknown", 12, 4);

        var viewModel = new ScriptDiagnosticViewModel(diagnostic);

        await Assert.That(viewModel.Diagnostic).IsSameReferenceAs(diagnostic);
        await Assert.That(viewModel.Location).IsEqualTo("L12:4");
        await Assert.That(viewModel.Severity).IsEqualTo("Error");
        await Assert.That(viewModel.Message).IsEqualTo("unknown");
    }

    [Test]
    public async Task Constructor_WarningSeverity_FormatsAsWarning()
    {
        var diagnostic = new ScriptDiagnostic(ScriptDiagnosticSeverity.Warning, "CS0168", "unused", 1, 1);

        var viewModel = new ScriptDiagnosticViewModel(diagnostic);

        await Assert.That(viewModel.Severity).IsEqualTo("Warning");
    }
}
