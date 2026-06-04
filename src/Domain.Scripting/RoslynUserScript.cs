using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     <see cref="IUserScript" /> implementation that wraps Roslyn-compiled
///     <see cref="Script{T}" /> instances for the request- and response-phase script bodies.
///     Each invocation is protected by a per-call timeout (via a linked
///     <see cref="CancellationTokenSource" />) and a managed-allocation ceiling tracked
///     through <see cref="GC.GetAllocatedBytesForCurrentThread" />.
///     When the allocation ceiling is exceeded the script references are cleared so that
///     subsequent calls are no-ops.
/// </summary>
public sealed class RoslynUserScript : IUserScript
{
    private readonly ScriptSandboxOptions _sandboxOptions;
    private volatile bool _isUnloaded;
    private volatile Script<object>? _requestScript;
    private volatile Script<object>? _responseScript;

    /// <summary>
    ///     Gets a value indicating whether the script context has been unloaded due to an
    ///     exceeded memory limit.  An unloaded script is permanently a no-op.
    /// </summary>
    public bool IsUnloaded => _isUnloaded;

    /// <summary>
    ///     Initializes a new <see cref="RoslynUserScript" />.
    /// </summary>
    /// <param name="displayName">The script's friendly display name.</param>
    /// <param name="requestScript">The compiled request-phase script (may be <see langword="null" />).</param>
    /// <param name="responseScript">The compiled response-phase script (may be <see langword="null" />).</param>
    /// <param name="sandboxOptions">Resource limits to enforce on every invocation.</param>
    public RoslynUserScript(
        string displayName,
        Script<object>? requestScript,
        Script<object>? responseScript,
        ScriptSandboxOptions sandboxOptions)
    {
        DisplayName = displayName;
        _requestScript = requestScript;
        _responseScript = responseScript;
        _sandboxOptions = sandboxOptions;
        IsRequestPhaseEnabled = requestScript is not null;
        IsResponsePhaseEnabled = responseScript is not null;
    }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsRequestPhaseEnabled { get; }

    /// <inheritdoc />
    public bool IsResponsePhaseEnabled { get; }

    /// <inheritdoc />
    public async Task OnRequestAsync(
        ScriptableRequest request,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        var script = _requestScript;
        if (script is null || _isUnloaded)
        {
            return;
        }

        var globals = new RequestScriptGlobals
        {
            Request = request,
            SharedState = sharedState,
        };
        await RunWithSandboxAsync(script, globals, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task OnResponseAsync(
        ScriptableRequest request,
        ScriptableResponse response,
        IDictionary<string, object?> sharedState,
        CancellationToken cancellationToken)
    {
        var script = _responseScript;
        if (script is null || _isUnloaded)
        {
            return;
        }

        var globals = new ResponseScriptGlobals
        {
            Request = request,
            Response = response,
            SharedState = sharedState,
        };
        await RunWithSandboxAsync(script, globals, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunWithSandboxAsync(Script<object> script, object globals, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_sandboxOptions.TimeoutSeconds);
        using var timeoutCancellationSource = new CancellationTokenSource(timeout);
        using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationSource.Token);

        long allocatedBytes = 0;
        var scriptTask = Task.Run(async () =>
        {
            var allocatedBytesBeforeScript = GC.GetAllocatedBytesForCurrentThread();
            try
            {
                await script.RunAsync(globals, linkedCancellationSource.Token).ConfigureAwait(false);
            }
            finally
            {
                allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBeforeScript;
            }
        }, linkedCancellationSource.Token);

        await scriptTask.WaitAsync(linkedCancellationSource.Token).ConfigureAwait(false);

        if (allocatedBytes > _sandboxOptions.MemoryLimitBytes)
        {
            Unload();
            throw new ScriptMemoryLimitExceededException(allocatedBytes, _sandboxOptions.MemoryLimitBytes);
        }
    }

    private void Unload()
    {
        _isUnloaded = true;
        _requestScript = null;
        _responseScript = null;
    }
}
