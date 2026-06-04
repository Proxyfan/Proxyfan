using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     In-memory registry of loaded plugins. Used by the host to track which plugins have
///     been initialized and to surface their state in the manager UI.
/// </summary>
public sealed class PluginRegistry : IPluginRegistry
{
    private readonly Lock _gate;
    private readonly List<LoadedPlugin> _plugins;

    /// <summary>
    ///     Gets a detached snapshot of currently-registered plugins. The returned list is a
    ///     defensive copy that is decoupled from the backing store, so callers can enumerate
    ///     it safely even while concurrent reloads mutate the registry, and mutations on the
    ///     registry after the snapshot is taken do not affect what callers see. The list
    ///     itself is exposed as <see cref="IReadOnlyList{T}" />; callers should not rely on
    ///     the concrete runtime type and should not down-cast.
    /// </summary>
    public IReadOnlyList<LoadedPlugin> Plugins
    {
        get
        {
            lock (_gate)
            {
                return [.. _plugins];
            }
        }
    }

    /// <summary>
    ///     Initializes a new empty <see cref="PluginRegistry" />.
    /// </summary>
    public PluginRegistry()
    {
        var plugins = new List<LoadedPlugin>();
        var gate = new Lock();
        _plugins = plugins;
        _gate = gate;
    }

    IReadOnlyList<IPluginLoadState> IPluginRegistry.Plugins => Plugins;

    /// <summary>
    ///     Adds a pre-built failed <see cref="LoadedPlugin" /> entry (e.g. from a bad manifest
    ///     or a disabled identifier) directly to the registry without invoking
    ///     <see cref="IProxyfanPlugin.Initialize" />.
    /// </summary>
    /// <param name="failed">The failed entry to record.</param>
    public void AddFailed(LoadedPlugin failed)
    {
        lock (_gate)
        {
            _plugins.Add(failed);
        }
    }

    /// <summary>
    ///     Removes every plugin entry from the registry. Used by hot-reload flows that
    ///     re-scan the plugins root on disk and want to start from a clean slate.
    /// </summary>
    public void Reset()
    {
        lock (_gate)
        {
            _plugins.Clear();
        }
    }

    /// <summary>
    ///     Initializes a plugin against the supplied host and adds it to the registry,
    ///     capturing any initialization exception as a failed-load entry.
    /// </summary>
    /// <param name="plugin">The plugin to initialize.</param>
    /// <param name="host">The host API.</param>
    /// <param name="sourceDirectory">Optional directory the plugin was discovered in.</param>
    /// <returns>The resulting <see cref="LoadedPlugin" /> entry.</returns>
    public LoadedPlugin TryInitialize(IProxyfanPlugin plugin, IPluginHost host, string? sourceDirectory)
    {
        if (!PluginApiVersionChecker.HasCompatibility(host.ApiVersion, plugin.Metadata.ApiVersion))
        {
            var incompatible = new LoadedPlugin(plugin.Metadata, null, false, $"Required API {plugin.Metadata.ApiVersion} is incompatible with host API {host.ApiVersion}.", sourceDirectory);
            lock (_gate)
            {
                _plugins.Add(incompatible);
            }
            return incompatible;
        }

        try
        {
            plugin.Initialize(host);
            var loaded = new LoadedPlugin(plugin.Metadata, plugin, true, null, sourceDirectory);
            lock (_gate)
            {
                _plugins.Add(loaded);
            }
            return loaded;
        }
        catch (Exception ex)
        {
            var failed = new LoadedPlugin(plugin.Metadata, null, false, ex.Message, sourceDirectory);
            lock (_gate)
            {
                _plugins.Add(failed);
            }
            return failed;
        }
    }
}
