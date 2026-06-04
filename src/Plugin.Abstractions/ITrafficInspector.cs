namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     An inspector tab shown in the request/response inspector panel. Plugins register
///     implementations via <see cref="IPluginHost.RegisterInspectorTab" />.
/// </summary>
public interface ITrafficInspector
{
    /// <summary>
    ///     Gets the display name shown on the inspector tab.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     Gets the sort order used to position this tab relative to built-in tabs.
    ///     Lower values appear first.
    /// </summary>
    int Order { get; }
}
