using Proxyfan.Domain.Rules.Rules;
using System.Globalization;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Formats <see cref="MapRemoteDestination" /> instances as a single human-readable string
///     suitable for display in tool windows. Null components are rendered as <c>*</c>
///     ("preserve original").
/// </summary>
public static class MapRemoteDestinationFormatter
{
    /// <summary>
    ///     Renders the supplied destination as a single line.
    /// </summary>
    /// <param name="destination">The destination components.</param>
    /// <returns>A human-readable summary of the destination.</returns>
    public static string Format(MapRemoteDestination destination)
    {
        var scheme = destination.Scheme ?? "*";
        var host = destination.Host ?? "*";
        var port = destination.Port?.ToString(CultureInfo.InvariantCulture) ?? "*";
        var path = destination.Path ?? "*";
        return $"{scheme}://{host}:{port}{path}";
    }
}
