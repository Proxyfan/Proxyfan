using Proxyfan.Plugin.Abstractions;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Records a single export formatter registration contributed by a plugin via
///     <see cref="Proxyfan.Plugin.Abstractions.IPluginHost.RegisterExportFormatter" />.
/// </summary>
public sealed class PluginExportFormatterRegistration
{
    /// <summary>
    ///     Gets the export formatter implementation contributed by the plugin.
    /// </summary>
    public IExportFormatter Formatter { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginExportFormatterRegistration" />.
    /// </summary>
    /// <param name="formatter">The export formatter implementation.</param>
    public PluginExportFormatterRegistration(IExportFormatter formatter)
    {
        Formatter = formatter;
    }
}
