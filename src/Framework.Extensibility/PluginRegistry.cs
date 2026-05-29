using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     In-memory registry of loaded plugins. Used by the host to track which plugins have
///     been initialized and to surface their state in the manager UI.
/// </summary>
public sealed class PluginRegistry
{
    private readonly List<LoadedPlugin> _plugins;

    /// <summary>
    ///     Gets the snapshot of currently-registered plugins.
    /// </summary>
    public IReadOnlyList<LoadedPlugin> Plugins => _plugins;

    /// <summary>
    ///     Initializes a new empty <see cref="PluginRegistry" />.
    /// </summary>
    public PluginRegistry()
    {
        var plugins = new List<LoadedPlugin>();
        _plugins = plugins;
    }

    /// <summary>
    ///     Adds a pre-built failed <see cref="LoadedPlugin" /> entry (e.g. from a bad manifest
    ///     or a disabled identifier) directly to the registry without invoking
    ///     <see cref="IProxyfanPlugin.Initialize" />.
    /// </summary>
    /// <param name="failed">The failed entry to record.</param>
    public void AddFailed(LoadedPlugin failed)
    {
        _plugins.Add(failed);
    }

    /// <summary>
    ///     Removes every plugin entry from the registry. Used by hot-reload flows that
    ///     re-scan the plugins root on disk and want to start from a clean slate.
    /// </summary>
    public void Reset()
    {
        _plugins.Clear();
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
            _plugins.Add(incompatible);
            return incompatible;
        }

        try
        {
            plugin.Initialize(host);
            var loaded = new LoadedPlugin(plugin.Metadata, plugin, true, null, sourceDirectory);
            _plugins.Add(loaded);
            return loaded;
        }
        catch (Exception ex)
        {
            var failed = new LoadedPlugin(plugin.Metadata, null, false, ex.Message, sourceDirectory);
            _plugins.Add(failed);
            return failed;
        }
    }
}
