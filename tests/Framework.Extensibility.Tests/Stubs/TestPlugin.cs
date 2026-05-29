using System.Threading;
using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility.Tests.Stubs;

/// <summary>
///     Public no-argument <see cref="IProxyfanPlugin" /> implementation used by the
///     <see cref="IsolatedPluginInstanceFactory" /> and <see cref="PluginInstanceCreator" />
///     success-path tests. Public so that <see cref="System.Reflection.Assembly.GetType" />
///     can locate it inside the test assembly when the assembly is loaded into a
///     <see cref="PluginLoadContext" />.
/// </summary>
public sealed class TestPlugin : IProxyfanPlugin
{
    private static readonly PluginMetadata DefaultMetadata = new(
        "test.plugin",
        "Test Plugin",
        "1.0.0",
        "Tests",
        "Stub plugin used by the extensibility tests.",
        "1.0");

    /// <summary>
    ///     Gets a count of how many times <see cref="Initialize" /> has been invoked across all
    ///     instances. Used by tests to assert lifecycle wiring.
    /// </summary>
    public static int InitializeCallCount => Volatile.Read(ref _initializeCallCount);

    private static int _initializeCallCount;

    /// <inheritdoc />
    public PluginMetadata Metadata => DefaultMetadata;

    /// <inheritdoc />
    public void Initialize(IPluginHost host)
    {
        _ = host;
        Interlocked.Increment(ref _initializeCallCount);
    }
}
