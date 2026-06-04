using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting.Tests;

/// <summary>
///     Tests that exercise the sandbox resource limits (timeout and allocation ceiling)
///     baked into <see cref="RoslynUserScript" /> via <see cref="ScriptSandboxOptions" />.
/// </summary>
public sealed class RoslynUserScriptSandboxTests
{
    /// <summary>
    ///     Verifies that a script containing an infinite loop is cancelled when the configured
    ///     timeout expires, and that the resulting exception propagates as
    ///     <see cref="OperationCanceledException" />.
    /// </summary>
    [Test]
    public async Task RunAsync_ScriptExceedsTimeout_IsCancelled()
    {
        var sandboxOptions = new ScriptSandboxOptions(timeoutSeconds: 1, memoryLimitBytes: 50L * 1024L * 1024L);
        var compiler = new RoslynUserScriptCompiler(sandboxOptions);
        const string source = "await System.Threading.Tasks.Task.Delay(60_000);";
        var compilation = compiler.Compile("timeout-test", source, string.Empty);
        var script = compilation.Script!;
        var request = BuildScriptableRequest();
        var sharedState = new Dictionary<string, object?>();

        await Assert.That(async () => await script.OnRequestAsync(request, sharedState, CancellationToken.None))
            .Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that a script whose managed allocation exceeds the configured per-invocation
    ///     ceiling throws <see cref="ScriptMemoryLimitExceededException" /> and sets
    ///     <see cref="RoslynUserScript.IsUnloaded" /> to <see langword="true" />, preventing
    ///     any subsequent invocations.
    /// </summary>
    [Test]
    public async Task RunAsync_ScriptAllocatesAboveBudget_UnloadsContext()
    {
        var sandboxOptions = new ScriptSandboxOptions(timeoutSeconds: 5, memoryLimitBytes: 1024L);
        var compiler = new RoslynUserScriptCompiler(sandboxOptions);
        const string source = "var data = new byte[10 * 1024 * 1024];";
        var compilation = compiler.Compile("oom-test", source, string.Empty);
        var script = (RoslynUserScript)compilation.Script!;
        var request = BuildScriptableRequest();
        var sharedState = new Dictionary<string, object?>();

        await Assert.That(async () => await script.OnRequestAsync(request, sharedState, CancellationToken.None))
            .Throws<ScriptMemoryLimitExceededException>();

        await Assert.That(script.IsUnloaded).IsTrue();
    }

    private static ScriptableRequest BuildScriptableRequest()
    {
        var requestData = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        });
        return new ScriptableRequest(requestData);
    }
}
