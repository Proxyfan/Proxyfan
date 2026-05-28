using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Builds the human-readable label used in the Diff Tool pool listbox.
/// </summary>
public static class DiffPoolItemDisplayFormatter
{
    /// <summary>
    ///     Formats the given flow as "{METHOD} {URL} -> {STATUS}" (status omitted when
    ///     the flow has no response yet).
    /// </summary>
    /// <param name="flow">The flow to format.</param>
    /// <returns>A short label suitable for a listbox row.</returns>
    public static string Format(TrafficFlow flow)
    {
        var method = flow.Request?.Method ?? "(no request)";
        var url = flow.Request?.RequestUri.ToString() ?? "(no url)";
        var status = flow.Response is null ? string.Empty : $" -> {flow.Response.StatusCode}";
        return $"{method} {url}{status}";
    }
}
