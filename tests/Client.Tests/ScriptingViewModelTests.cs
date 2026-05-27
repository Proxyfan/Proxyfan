using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Scripting;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ScriptingViewModel" /> covering enable/disable toggling, the
///     compile command's success and failure paths, the clear command, and external-change
///     propagation back into the bound source properties.
/// </summary>
public sealed class ScriptingViewModelTests
{
    /// <summary>
    ///     Verifies that the constructor seeds enabled state and source text from the
    ///     supplied configuration.
    /// </summary>
    [Test]
    public async Task Constructor_EnabledConfiguration_SeedsFromConfiguration()
    {
        var configuration = new MutableScriptingConfiguration(true);
        configuration.SetRequestSource("// req");
        configuration.SetResponseSource("// resp");
        var compiler = new StubUserScriptCompiler();

        var viewModel = CreateViewModel(configuration, compiler);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.RequestSource).IsEqualTo("// req");
        await Assert.That(viewModel.ResponseSource).IsEqualTo("// resp");
        await Assert.That(viewModel.IsCompilationSuccessful).IsFalse();
    }

    /// <summary>
    ///     Verifies that toggling <see cref="ScriptingViewModel.IsEnabled" /> from the UI
    ///     propagates to the underlying configuration.
    /// </summary>
    [Test]
    public async Task IsEnabled_ToggleOff_PropagatesToConfiguration()
    {
        var configuration = new MutableScriptingConfiguration(true);
        var compiler = new StubUserScriptCompiler();
        var viewModel = CreateViewModel(configuration, compiler);

        viewModel.IsEnabled = false;

        await Assert.That(configuration.IsEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that the Compile command updates the configuration's active script when
    ///     compilation succeeds and reports success.
    /// </summary>
    [Test]
    public async Task CompileCommand_Success_SetsActiveScriptAndReportsSuccess()
    {
        var configuration = new MutableScriptingConfiguration(true);
        var compiler = new StubUserScriptCompiler();
        var viewModel = CreateViewModel(configuration, compiler);
        viewModel.RequestSource = "// new request";
        viewModel.ResponseSource = "// new response";

        viewModel.CompileCommand.Execute(null);

        await Assert.That(compiler.Invocations.Count).IsEqualTo(1);
        await Assert.That(compiler.Invocations[0].RequestScript).IsEqualTo("// new request");
        await Assert.That(compiler.Invocations[0].ResponseScript).IsEqualTo("// new response");
        await Assert.That(viewModel.IsCompilationSuccessful).IsTrue();
        await Assert.That(viewModel.CompilationStatus).IsEqualTo("OK");
        await Assert.That(configuration.ActiveScript).IsNotNull();
        await Assert.That(viewModel.Diagnostics.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the Compile command surfaces diagnostics and clears the active
    ///     script when compilation fails.
    /// </summary>
    [Test]
    public async Task CompileCommand_Failure_PublishesDiagnosticsAndClearsActiveScript()
    {
        var configuration = new MutableScriptingConfiguration(true);
        configuration.SetActiveScript(new StubCompiledScript("Prior"));
        var compiler = new StubUserScriptCompiler();
        var diagnostic = new ScriptDiagnostic(ScriptDiagnosticSeverity.Error, "CS1002", "; expected", 2, 5);
        var diagnostics = ImmutableArray.Create(diagnostic);
        compiler.NextResult = new ScriptCompilationResult(false, null, diagnostics);
        var viewModel = CreateViewModel(configuration, compiler);

        viewModel.CompileCommand.Execute(null);

        await Assert.That(viewModel.IsCompilationSuccessful).IsFalse();
        await Assert.That(viewModel.CompilationStatus).IsEqualTo("Failed");
        await Assert.That(viewModel.Diagnostics.Count).IsEqualTo(1);
        await Assert.That(viewModel.Diagnostics[0].Severity).IsEqualTo("Error");
        await Assert.That(viewModel.Diagnostics[0].Location).IsEqualTo("L2:5");
        await Assert.That(viewModel.Diagnostics[0].Message).IsEqualTo("; expected");
        await Assert.That(configuration.ActiveScript).IsNull();
    }

    /// <summary>
    ///     Verifies that the ClearScript command removes the active script and resets the
    ///     local success indicators.
    /// </summary>
    [Test]
    public async Task ClearScriptCommand_ActiveScript_ClearsConfigurationAndState()
    {
        var configuration = new MutableScriptingConfiguration(true);
        var compiler = new StubUserScriptCompiler();
        var viewModel = CreateViewModel(configuration, compiler);
        viewModel.CompileCommand.Execute(null);

        viewModel.ClearScriptCommand.Execute(null);

        await Assert.That(configuration.ActiveScript).IsNull();
        await Assert.That(viewModel.IsCompilationSuccessful).IsFalse();
        await Assert.That(viewModel.CompilationStatus).IsEqualTo(string.Empty);
        await Assert.That(viewModel.Diagnostics.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that external configuration changes propagate back into the bound
    ///     properties via the inline UI scheduler.
    /// </summary>
    [Test]
    public async Task ExternalChange_SetEnabled_UpdatesIsEnabled()
    {
        var configuration = new MutableScriptingConfiguration(false);
        var compiler = new StubUserScriptCompiler();
        var viewModel = CreateViewModel(configuration, compiler);

        configuration.SetEnabled(true);

        await Assert.That(viewModel.IsEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that external request/response source mutations propagate back into the
    ///     bound properties.
    /// </summary>
    [Test]
    public async Task ExternalChange_SetSource_UpdatesBoundProperties()
    {
        var configuration = new MutableScriptingConfiguration(true);
        var compiler = new StubUserScriptCompiler();
        var viewModel = CreateViewModel(configuration, compiler);

        configuration.SetRequestSource("// external request");
        configuration.SetResponseSource("// external response");

        await Assert.That(viewModel.RequestSource).IsEqualTo("// external request");
        await Assert.That(viewModel.ResponseSource).IsEqualTo("// external response");
    }

    /// <summary>
    ///     Verifies that disposing the view model unsubscribes from the configuration's
    ///     Changed event so post-dispose mutations no longer propagate.
    /// </summary>
    [Test]
    public async Task Dispose_AfterChange_StopsReceivingUpdates()
    {
        var configuration = new MutableScriptingConfiguration(false);
        var compiler = new StubUserScriptCompiler();
        var viewModel = CreateViewModel(configuration, compiler);
        viewModel.Dispose();

        configuration.SetEnabled(true);

        await Assert.That(viewModel.IsEnabled).IsFalse();
    }

    private static ScriptingViewModel CreateViewModel(
        MutableScriptingConfiguration configuration,
        StubUserScriptCompiler compiler)
    {
        return new ScriptingViewModel(configuration, compiler, InlineUserInterfaceScheduler.Instance);
    }
}
