using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Helpers for extracting the source host from a captured request,
///     used by <see cref="SourceListViewModel" /> when grouping flows.
/// </summary>
public static class SourceHostExtractor
{
    /// <summary>
    ///     Returns the host header value when present and non-blank,
    ///     falling back to the request URI host otherwise.
    /// </summary>
    /// <param name="domainEvent">The request event to extract from.</param>
    /// <returns>The host string used to key the source list grouping.</returns>
    public static string Extract(RequestReceived domainEvent)
    {
        var header = domainEvent.Request.Headers.Get("Host");
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        return domainEvent.Request.RequestUri.Host;
    }

    /// <summary>
    ///     Returns the host header value when present and non-blank,
    ///     falling back to the request URI host and then to the supplied
    ///     fallback value.
    /// </summary>
    /// <param name="request">The request to extract from.</param>
    /// <param name="fallbackHost">The fallback host when request is unavailable.</param>
    /// <returns>The host string used to key the source list grouping.</returns>
    public static string Extract(HypertextTransferProtocolRequestData? request, string fallbackHost)
    {
        if (request is null)
        {
            return fallbackHost;
        }

        var header = request.Headers.Get("Host");
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        return request.RequestUri.Host;
    }
}
