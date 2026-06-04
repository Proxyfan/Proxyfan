namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Resource limits enforced around each script invocation:
///     a wall-clock timeout and a per-invocation allocation ceiling.
/// </summary>
public sealed class ScriptSandboxOptions
{
    /// <summary>
    ///     Default sandbox limits: 5-second timeout and 50 MB allocation ceiling per invocation.
    /// </summary>
    public static readonly ScriptSandboxOptions Default;

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
        var defaultOptions = new ScriptSandboxOptions(timeoutSeconds: 5, memoryLimitBytes: 50L * 1024L * 1024L);
        Default = defaultOptions;
    }

    /// <summary>
    ///     Initializes a new <see cref="ScriptSandboxOptions" />.
    /// </summary>
    /// <param name="timeoutSeconds">Wall-clock timeout in seconds (must be &gt; 0).</param>
    /// <param name="memoryLimitBytes">Allocation ceiling in bytes (must be &gt; 0).</param>
    public ScriptSandboxOptions(int timeoutSeconds, long memoryLimitBytes)
    {
        TimeoutSeconds = timeoutSeconds;
        MemoryLimitBytes = memoryLimitBytes;
    }
}
