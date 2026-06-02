using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Post-instantiation gate that verifies the plugin instance's own
///     <see cref="PluginMetadata.Id" /> matches the manifest and is not in the
///     disabled-state store. A mismatched manifest id (stale or tampered) would otherwise
///     bypass the pre-instantiation disabled check, allowing a user-disabled plugin to
///     execute its extension registrations. On rejection the helper unloads the owning
///     collectible <see cref="PluginLoadContext" /> (when supplied) so the load attempt
///     leaves no live assembly behind.
/// </summary>
public static class PluginRuntimeIdentityChecker
{
    /// <summary>
    ///     Validates <paramref name="instantiation" /> against <paramref name="manifestMetadata" />
    ///     and <paramref name="disabledIdentifiers" />. Returns null when accepted, or a
    ///     populated failed <see cref="LoadedPlugin" /> when rejected (after unloading the
    ///     load context).
    /// </summary>
    /// <param name="manifestMetadata">The manifest metadata as scanned from disk.</param>
    /// <param name="instantiation">The successful instantiation result to validate.</param>
    /// <param name="disabledIdentifiers">The set of disabled plugin identifiers.</param>
    /// <param name="sourceDirectory">The directory the plugin was discovered in.</param>
    /// <returns>A failed <see cref="LoadedPlugin" /> describing the rejection, or null on success.</returns>
    public static LoadedPlugin? Validate(PluginMetadata manifestMetadata, PluginInstantiationResult instantiation, IReadOnlySet<string> disabledIdentifiers, string sourceDirectory)
    {
        if (instantiation.Plugin is null)
        {
            return null;
        }

        var runtimeId = instantiation.Plugin.Metadata.Id;
        var manifestId = manifestMetadata.Id;
        var idsMatch = string.Equals(runtimeId, manifestId, StringComparison.OrdinalIgnoreCase);
        if (idsMatch && !disabledIdentifiers.Contains(runtimeId))
        {
            return null;
        }

        if (instantiation.LoadContext is PluginLoadContext rejectedContext)
        {
            PluginLoadContextUnloader.Unload(rejectedContext);
        }

        var message = idsMatch
            ? "Disabled by user."
            : $"Plugin metadata id '{runtimeId}' does not match manifest id '{manifestId}'.";
        return new LoadedPlugin(manifestMetadata, null, false, message, sourceDirectory);
    }
}
