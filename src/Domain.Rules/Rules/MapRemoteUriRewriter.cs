using System;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Helpers for applying a <see cref="MapRemoteDestination" /> to an original URI to produce
///     the rewritten URI used downstream of a Map Remote rule.
/// </summary>
public static class MapRemoteUriRewriter
{
    /// <summary>
    ///     Applies the supplied destination components to the original URI.
    ///     Components whose value is <see langword="null" /> preserve the original component.
    /// </summary>
    /// <param name="originalUri">The original request URI.</param>
    /// <param name="destination">The destination components.</param>
    /// <returns>The rewritten URI.</returns>
    public static Uri Rewrite(Uri originalUri, MapRemoteDestination destination)
    {
        var builder = new UriBuilder(originalUri);

        if (destination.Scheme is not null)
        {
            builder.Scheme = destination.Scheme;
        }

        if (destination.Host is not null)
        {
            builder.Host = destination.Host;
        }

        if (destination.Port is not null)
        {
            builder.Port = destination.Port.Value;
        }

        if (destination.Path is not null)
        {
            builder.Path = destination.Path;
        }

        return builder.Uri;
    }
}
