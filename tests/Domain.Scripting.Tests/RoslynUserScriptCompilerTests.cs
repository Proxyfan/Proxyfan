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
}
