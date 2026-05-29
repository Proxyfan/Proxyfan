namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Records a single inspector tab registration contributed by a plugin via
///     <see cref="Proxyfan.Plugin.Abstractions.IPluginHost.RegisterInspectorTab" />.
/// </summary>
public sealed class PluginInspectorTabRegistration
{
    /// <summary>
    ///     Gets the display name of the inspector tab.
    /// </summary>
    public string TabName { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginInspectorTabRegistration" />.
    /// </summary>
    /// <param name="tabName">The display name of the tab.</param>
    public PluginInspectorTabRegistration(string tabName)
    {
        TabName = tabName;
    }
}
