using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Orchestrates plugin discovery, instantiation, and registration. Given a plugins
///     root directory, the loader:
///     <list type="number">
///         <item>Scans for candidates via <see cref="PluginDirectoryScanner" />.</item>
///         <item>Skips any candidate whose id is in the enabled-state store.</item>
///         <item>For each enabled valid candidate, calls the
///               <see cref="IPluginInstanceFactory" /> to instantiate.</item>
///         <item>Hands the resulting plugin to <see cref="PluginRegistry.TryInitialize" />
///               for compatibility check and lifecycle <c>Initialize</c>.</item>
///         <item>Surfaces invalid candidates (missing manifest, parse failure) as failed
///               <see cref="LoadedPlugin" /> entries so the UI can show diagnostics.</item>
///     </list>
/// </summary>
public sealed class PluginLoader
{
    private readonly IPluginEnabledStateStore _enabledStateStore;
    private readonly IPluginInstanceFactory _instanceFactory;
    private readonly PluginRegistry _registry;
    private readonly PluginDirectoryScanner _scanner;

    /// <summary>
    ///     Initializes a new <see cref="PluginLoader" />.
    /// </summary>
    /// <param name="scanner">The directory scanner.</param>
    /// <param name="instanceFactory">The instance factory (typically <see cref="IsolatedPluginInstanceFactory" />).</param>
    /// <param name="registry">The plugin registry to populate.</param>
    /// <param name="enabledStateStore">The enabled-state store.</param>
    public PluginLoader(
        PluginDirectoryScanner scanner,
        IPluginInstanceFactory instanceFactory,
        PluginRegistry registry,
        IPluginEnabledStateStore enabledStateStore)
    {
        _scanner = scanner;
        _instanceFactory = instanceFactory;
        _registry = registry;
        _enabledStateStore = enabledStateStore;
    }

    /// <summary>
    ///     Loads every enabled plugin found under <paramref name="rootDirectory" />, calling
    ///     <see cref="IProxyfanPlugin.Initialize" /> on each successfully-loaded plugin. The
    ///     returned list mirrors the registry order and includes any failed entries
    ///     (invalid manifest, missing assembly, incompatible API).
    /// </summary>
    /// <param name="rootDirectory">The plugins root.</param>
    /// <param name="host">The host API surface.</param>
    /// <returns>The collection of attempted plugin loads.</returns>
    public IReadOnlyList<LoadedPlugin> LoadAll(string rootDirectory, IPluginHost host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        var disabledIdentifiers = _enabledStateStore.GetDisabledIdentifiers();
        var candidates = _scanner.Scan(rootDirectory);
        var results = new List<LoadedPlugin>();

        foreach (var candidate in candidates)
        {
            if (!candidate.IsValid || candidate.Manifest is null)
            {
                var directoryName = Path.GetFileName(candidate.DirectoryPath);
                var metadata = new PluginMetadata(directoryName, directoryName, "0.0.0", "unknown", "Failed to read manifest.", "0.0");
                var invalid = new LoadedPlugin(metadata, null, false, candidate.ErrorMessage, candidate.DirectoryPath);
                results.Add(invalid);
                _registry.AddFailed(invalid);
                continue;
            }

            if (disabledIdentifiers.Contains(candidate.Manifest.Metadata.Id))
            {
                var disabled = new LoadedPlugin(candidate.Manifest.Metadata, null, false, "Disabled by user.", candidate.DirectoryPath);
                results.Add(disabled);
                _registry.AddFailed(disabled);
                continue;
            }

            var instantiation = _instanceFactory.Create(candidate);
            if (!instantiation.IsSuccess || instantiation.Plugin is null)
            {
                var failed = new LoadedPlugin(candidate.Manifest.Metadata, null, false, instantiation.ErrorMessage, candidate.DirectoryPath);
                results.Add(failed);
                _registry.AddFailed(failed);
                continue;
            }

            var loaded = _registry.TryInitialize(instantiation.Plugin, host, candidate.DirectoryPath);
            results.Add(loaded);
        }

        return results;
    }
}
