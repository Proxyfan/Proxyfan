using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Formats a captured HTTP request as a human-readable Graph Query Language (GraphQL)
///     inspector view. Returns an empty string when the request is not detected as GraphQL,
///     or a helpful diagnostic when GraphQL is detected but the payload cannot be parsed.
/// </summary>
public static class GraphQueryLanguageInspectorFormatter
{
    /// <summary>
    ///     Renders the GraphQL inspector text for the supplied request, or an empty string
    ///     when the request is not GraphQL.
    /// </summary>
    /// <param name="request">The captured HTTP request data (may be <see langword="null" />).</param>
    /// <returns>The GraphQL inspector text or <see cref="string.Empty" />.</returns>
    public static string Format(HypertextTransferProtocolRequestData? request)
    {
        return global::Proxyfan.Domain.Traffic.GraphQueryLanguageInspectorTextFormatter.Format(request);
    }
}
