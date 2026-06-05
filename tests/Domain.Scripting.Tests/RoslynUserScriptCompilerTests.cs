using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests for <see cref="RoslynUserScriptCompiler" />.
/// </summary>
public sealed class RoslynUserScriptCompilerTests
{
    /// <summary>
    ///     Verifies that compiling a script with an empty source body yields a script that
    ///     has neither a request nor a response phase enabled.
    /// </summary>
    [Test]
    public async Task Compile_BothEmpty_ProducesEmptyScript()
    {
        var compiler = new RoslynUserScriptCompiler();

        var result = compiler.Compile("empty", string.Empty, string.Empty);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Script).IsNotNull();
        await Assert.That(result.Script!.IsRequestPhaseEnabled).IsFalse();
        await Assert.That(result.Script!.IsResponsePhaseEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that compiling a script with valid request and response code succeeds
    ///     and the resulting script has both phases enabled.
    /// </summary>
    [Test]
    public async Task Compile_BothPhasesValid_ProducesScriptWithBothPhases()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string requestSource = "Request.Headers.Set(\"X-Trace\", \"yes\");";
        const string responseSource = "Response.Headers.Set(\"X-Trace-Out\", \"yes\");";

        var result = compiler.Compile("both", requestSource, responseSource);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Script).IsNotNull();
        await Assert.That(result.Script!.IsRequestPhaseEnabled).IsTrue();
        await Assert.That(result.Script!.IsResponsePhaseEnabled).IsTrue();
        await Assert.That(result.Script.DisplayName).IsEqualTo("both");
    }

    /// <summary>
    ///     Verifies that compiling a script with a syntax error in the request body fails
    ///     and produces error diagnostics.
    /// </summary>
    [Test]
    public async Task Compile_InvalidRequestSyntax_FailsWithErrors()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string brokenSource = "this is not C#!";

        var result = compiler.Compile("broken", brokenSource, string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Script).IsNull();
        await Assert.That(result.Diagnostics.Length > 0).IsTrue();
    }

    /// <summary>
    ///     Verifies that compiling a script with only request source produces a request-only script.
    /// </summary>
    [Test]
    public async Task Compile_RequestOnly_ProducesRequestOnlyScript()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string requestSource = "Request.Headers.Set(\"X-Trace\", \"yes\");";

        var result = compiler.Compile("request-only", requestSource, string.Empty);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Script!.IsRequestPhaseEnabled).IsTrue();
        await Assert.That(result.Script!.IsResponsePhaseEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that compiling a script with only response source produces a response-only script.
    /// </summary>
    [Test]
    public async Task Compile_ResponseOnly_ProducesResponseOnlyScript()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string responseSource = "Response.Headers.Set(\"X-Trace-Out\", \"yes\");";

        var result = compiler.Compile("response-only", string.Empty, responseSource);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Script!.IsRequestPhaseEnabled).IsFalse();
        await Assert.That(result.Script!.IsResponsePhaseEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that a script referencing <c>System.Net.Http.HttpClient</c> fails to compile
    ///     because the <c>System.Net.Http</c> assembly is not included in the curated reference
    ///     set exposed to user scripts.
    /// </summary>
    [Test]
    public async Task Compile_ScriptReferencesForbiddenAssembly_ReturnsErrors()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string source = "_ = new System.Net.Http.HttpClient();";

        var result = compiler.Compile("forbidden", source, string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Diagnostics.Any(d => d.Severity == ScriptDiagnosticSeverity.Error)).IsTrue();
    }

    /// <summary>
    ///     Verifies that scripts using a <c>#load</c> directive are rejected so users cannot
    ///     import additional source files.
    /// </summary>
    [Test]
    public async Task Compile_ScriptUsesLoadDirective_ReturnsErrors()
    {
        var compiler = new RoslynUserScriptCompiler();
        var tempScriptPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempScriptPath, "int Loaded() => 1;");
        var escapedPath = tempScriptPath.Replace("\\", "\\\\");
        var source = $"#load \"{escapedPath}\"\n_ = Loaded();";

        try
        {
            var result = compiler.Compile("load-directive", source, string.Empty);

            await Assert.That(result.IsSuccess).IsFalse();
            await Assert.That(result.Diagnostics.Any(d => d.Id == ScriptForbiddenDirectiveScanner.DiagnosticId)).IsTrue();
        }
        finally
        {
            if (File.Exists(tempScriptPath))
            {
                File.Delete(tempScriptPath);
            }
        }
    }

    /// <summary>
    ///     Verifies that a script calling <c>System.Diagnostics.Process.Start</c> fails to compile
    ///     because <c>System.Diagnostics</c> process-related types are not exposed in the curated
    ///     reference set.
    /// </summary>
    [Test]
    public async Task Compile_ScriptUsesProcessStart_ReturnsErrors()
    {
        var compiler = new RoslynUserScriptCompiler();
        const string source = "System.Diagnostics.Process.Start(\"cmd\");";

        var result = compiler.Compile("process-start", source, string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Diagnostics.Any(d => d.Severity == ScriptDiagnosticSeverity.Error)).IsTrue();
    }

    /// <summary>
    ///     Verifies that scripts using a <c>#r</c> directive are rejected so users cannot
    ///     extend the assembly allow-list.
    /// </summary>
    [Test]
    public async Task Compile_ScriptUsesReferenceDirective_ReturnsErrors()
    {
        var compiler = new RoslynUserScriptCompiler();
        var systemNetHttpPath = typeof(System.Net.Http.HttpClient).Assembly.Location.Replace("\\", "\\\\");
        var source = $"#r \"{systemNetHttpPath}\"\n_ = new System.Net.Http.HttpClient();";

        var result = compiler.Compile("reference-directive", source, string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Diagnostics.Any(d => d.Id == ScriptForbiddenDirectiveScanner.DiagnosticId)).IsTrue();
    }
}
