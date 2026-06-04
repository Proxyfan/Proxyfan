using System.Collections.Generic;

namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     Persists the list of plugin identifiers that the user has explicitly disabled.
///     The host consults the store during plugin loading and skips any plugin whose id
///     appears in the disabled set.
/// </summary>
public interface IPluginEnabledStateStore
{
    /// <summary>
    ///     Returns the current set of disabled plugin identifiers.
    /// </summary>
    /// <returns>The disabled identifiers (case-insensitive).</returns>
    IReadOnlySet<string> GetDisabledIdentifiers();

    /// <summary>
    ///     Sets the enabled state for the supplied plugin identifier.
    /// </summary>
    /// <param name="identifier">The plugin identifier.</param>
    /// <param name="isEnabled">True to enable, false to disable.</param>
    void SetEnabled(string identifier, bool isEnabled);
}
