using Proxyfan.Plugin.Abstractions;
using System.Threading;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Idempotent startup hook that triggers <see cref="PluginLoader.LoadAll" /> against
///     the configured plugins root the first time it is called. Once activated, subsequent
///     <see cref="EnsureLoaded" /> calls are no-ops; UI-triggered "Reload" flows clear the
///     registry first via <see cref="Reload" />.
/// </summary>
public sealed class PluginActivationService
{
    private readonly IPluginHost _host;
    private readonly PluginLoader _loader;
    private readonly Lock _loadLock;
    private readonly PluginRegistry _registry;
    private readonly PluginRootDirectoryProvider _rootProvider;

    /// <summary>
    ///     Gets a value indicating whether the loader has been invoked at least once.
    /// </summary>
    public bool IsActivated { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="PluginActivationService" />.
    /// </summary>
    /// <param name="loader">The plugin loader.</param>
    /// <param name="host">The plugin host.</param>
    /// <param name="rootProvider">The plugins root directory provider.</param>
    /// <param name="registry">The plugin registry.</param>
    public PluginActivationService(
        PluginLoader loader,
        IPluginHost host,
        PluginRootDirectoryProvider rootProvider,
        PluginRegistry registry)
    {
        var newLock = new Lock();
        _loader = loader;
        _host = host;
        _rootProvider = rootProvider;
        _registry = registry;
        _loadLock = newLock;
    }

    /// <summary>
    ///     Runs <see cref="PluginLoader.LoadAll" /> exactly once. Subsequent calls are no-ops.
    /// </summary>
    public void EnsureLoaded()
    {
        lock (_loadLock)
        {
            if (IsActivated)
            {
                return;
            }

            _ = _loader.LoadAll(_rootProvider.GetRootDirectory(), _host);
            IsActivated = true;
        }
    }

    /// <summary>
    ///     Clears the registry and re-runs <see cref="PluginLoader.LoadAll" />. Use this for
    ///     UI-triggered "Reload" flows. Note that previously-loaded plugin assemblies remain
    ///     in their original <see cref="System.Runtime.Loader.AssemblyLoadContext" /> until
    ///     the process exits; reload primarily re-scans the manifest list and reports any
    ///     newly-added entries.
    /// </summary>
    public void Reload()
    {
        lock (_loadLock)
        {
            _registry.Reset();
            _ = _loader.LoadAll(_rootProvider.GetRootDirectory(), _host);
            IsActivated = true;
        }
    }
}
