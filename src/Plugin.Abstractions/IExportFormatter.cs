namespace Proxyfan.Plugin.Abstractions;

/// <summary>
///     An export format that converts captured traffic to a particular output format.
///     Plugins register implementations via <see cref="IPluginHost.RegisterExportFormatter" />.
/// </summary>
public interface IExportFormatter
{
    /// <summary>
    ///     Gets the display name shown in the export format picker (e.g. <c>"HAR 1.2"</c>,
    ///     <c>"cURL"</c>).
    /// </summary>
    string DisplayName { get; }
}
