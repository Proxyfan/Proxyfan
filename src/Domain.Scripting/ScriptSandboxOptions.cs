namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Resource limits enforced around each script invocation:
///     a wall-clock timeout and a per-invocation allocation ceiling.
/// </summary>
public sealed class ScriptSandboxOptions
{
    /// <summary>
    ///     Default sandbox limits: 5-second execution timeout, 50 MB allocation ceiling per
    ///     invocation, and a 2-second compilation timeout per phase.
    /// </summary>
    public static readonly ScriptSandboxOptions Default;

    /// <summary>
    ///     Gets the maximum wall-clock time (in seconds) allowed for compiling a single script
    ///     phase before it is cancelled.  A value of 0 causes an immediate timeout (useful for
    ///     testing); values greater than 0 cap the compiler at that many seconds.
    /// </summary>
    public int CompilationTimeoutSeconds { get; }

    /// <summary>
    ///     Gets the maximum number of bytes that a single script invocation may allocate on the
    ///     executing thread before the script context is unloaded.
    /// </summary>
    public long MemoryLimitBytes { get; }

    /// <summary>
    ///     Gets the maximum wall-clock time (in seconds) allowed for a single script invocation
    ///     before it is cancelled.
    /// </summary>
    public int TimeoutSeconds { get; }

    static ScriptSandboxOptions()
    {
        var defaultOptions = new ScriptSandboxOptions(timeoutSeconds: 5, memoryLimitBytes: 50L * 1024L * 1024L, compilationTimeoutSeconds: 2);
        Default = defaultOptions;
    }

    /// <summary>
    ///     Initializes a new <see cref="ScriptSandboxOptions" />.
    /// </summary>
    /// <param name="timeoutSeconds">Wall-clock execution timeout in seconds (must be &gt; 0).</param>
    /// <param name="memoryLimitBytes">Allocation ceiling in bytes (must be &gt; 0).</param>
    /// <param name="compilationTimeoutSeconds">
    ///     Maximum seconds allowed for each compilation phase (must be &gt;= 0;
    ///     0 causes an immediate timeout, primarily useful for testing).
    /// </param>
    public ScriptSandboxOptions(int timeoutSeconds, long memoryLimitBytes, int compilationTimeoutSeconds)
    {
        TimeoutSeconds = timeoutSeconds;
        MemoryLimitBytes = memoryLimitBytes;
        CompilationTimeoutSeconds = compilationTimeoutSeconds;
    }
}
