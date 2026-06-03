using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Heuristically detects whether an HTTP request is a Graph Query Language (GraphQL)
///     request based on URL path and content-type. Used by the proxy to surface GraphQL
///     traffic with operation-name columns and the GraphQL inspector tab in the UI.
/// </summary>
public static class GraphQueryLanguageRequestDetector
{
    private static readonly HashSet<string> KnownContentTypes;
    private static readonly string[] PathHints;

    static GraphQueryLanguageRequestDetector()
    {
        var paths = new[]
        {
            "/graphql",
            "/api/graphql",
            "/v1/graphql",
            "/v2/graphql",
        };
        PathHints = paths;

        var contentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/graphql",
            "application/graphql+json",
            "application/graphql-response+json",
        };
        KnownContentTypes = contentTypes;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the URL path or content-type indicates a
    ///     GraphQL request.
    /// </summary>
    /// <param name="urlPath">The request URL path (without the host).</param>
    /// <param name="contentType">The request Content-Type header value (may be <see langword="null" />).</param>
    /// <returns><see langword="true" /> when the request looks like GraphQL.</returns>
    public static bool HasIndicator(string? urlPath, string? contentType)
    {
        if (HasMatchingContentType(contentType))
        {
            return true;
        }

        var hasMatchingPath = HasMatchingPath(urlPath);
        return hasMatchingPath;
    }

    private static bool HasMatchingContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separatorIndex < 0 ? contentType.Trim() : contentType[..separatorIndex].Trim();
        var hasMatch = KnownContentTypes.Contains(mediaType);
        return hasMatch;
    }

    private static bool HasMatchingPath(string? urlPath)
    {
        if (string.IsNullOrWhiteSpace(urlPath))
        {
            return false;
        }

        var queryIndex = urlPath.IndexOf('?', StringComparison.Ordinal);
        var pathOnly = queryIndex < 0 ? urlPath : urlPath[..queryIndex];

        foreach (var hint in PathHints)
        {
            if (pathOnly.EndsWith(hint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
