using Proxyfan.Domain.Scripting;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Hand-written <see cref="IUserScriptCompiler" /> stub used to drive
///     <see cref="Proxyfan.Client.Tools.ViewModels.ScriptingViewModel" /> tests
///     deterministically without invoking Roslyn.
/// </summary>
public sealed class StubUserScriptCompiler : IUserScriptCompiler
{
    /// <summary>
    ///     Gets the list of compiler invocations recorded in arrival order.
    /// </summary>
    public List<StubUserScriptCompilerInvocation> Invocations { get; } = [];

    /// <summary>
    ///     Gets or sets the result that the next compile call will return. Defaults to a
    ///     successful compilation of an empty <see cref="StubCompiledScript" />.
    /// </summary>
    public ScriptCompilationResult NextResult { get; set; } = CreateDefaultSuccessResult();

    /// <inheritdoc />
    public ScriptCompilationResult Compile(string displayName, string requestScript, string responseScript)
    {
        var invocation = new StubUserScriptCompilerInvocation(displayName, requestScript, responseScript);
        Invocations.Add(invocation);
        return NextResult;
    }

    private static ScriptCompilationResult CreateDefaultSuccessResult()
    {
        var script = new StubCompiledScript("Stub");
        var diagnostics = ImmutableArray<ScriptDiagnostic>.Empty;
        return new ScriptCompilationResult(true, script, diagnostics);
    }
}
