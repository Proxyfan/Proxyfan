using System;
using System.Globalization;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Thrown when a script invocation allocates more memory on the executing thread than the
///     configured <see cref="ScriptSandboxOptions.MemoryLimitBytes" /> ceiling.
///     After this exception is thrown the originating <see cref="RoslynUserScript" /> is
///     permanently unloaded and will not accept further invocations.
/// </summary>
public sealed class ScriptMemoryLimitExceededException : Exception
{
    /// <summary>
    ///     Gets the number of bytes allocated by the script invocation.
    /// </summary>
    public long AllocatedBytes { get; }

    /// <summary>
    ///     Gets the configured memory ceiling that was exceeded.
    /// </summary>
    public long LimitBytes { get; }

    /// <summary>
    ///     Initializes a new <see cref="ScriptMemoryLimitExceededException" />.
    /// </summary>
    /// <param name="allocatedBytes">Bytes allocated during the invocation.</param>
    /// <param name="limitBytes">Configured memory ceiling.</param>
    public ScriptMemoryLimitExceededException(long allocatedBytes, long limitBytes)
        : base(string.Format(
            CultureInfo.InvariantCulture,
            "Script exceeded memory limit: allocated {0} bytes, limit is {1} bytes.",
            allocatedBytes,
            limitBytes))
    {
        AllocatedBytes = allocatedBytes;
        LimitBytes = limitBytes;
    }
}
