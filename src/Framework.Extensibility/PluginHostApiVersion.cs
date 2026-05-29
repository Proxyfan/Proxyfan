namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Constants describing the host's published plugin API contract. Plugins compare
///     <see cref="Current" /> against their declared required version via
///     <see cref="PluginApiVersionChecker.HasCompatibility" />.
/// </summary>
public static class PluginHostApiVersion
{
    /// <summary>
    ///     The current host plugin API version (SemVer "major.minor"). Bump the major
    ///     component when introducing breaking changes to <see cref="Proxyfan.Plugin.Abstractions.IPluginHost" />
    ///     or <see cref="Proxyfan.Plugin.Abstractions.IProxyfanPlugin" />.
    /// </summary>
    public const string Current = "1.0";
}
