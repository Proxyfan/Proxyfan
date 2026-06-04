using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Projects the Graph Query Language (GraphQL) operation name from a captured HTTP
///     request for surfacing in the traffic list column. Returns <see langword="null" />
///     when the request is not a GraphQL request or the operation name cannot be determined.
/// </summary>
public static partial class GraphQueryLanguageRequestClassifier
{
    private static readonly HashSet<string> KnownContentTypes;
    private static readonly Regex OperationNamePattern;
    private static readonly string[] PathHints;

    static GraphQueryLanguageRequestClassifier()
    {
        var knownContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/graphql",
            "application/graphql+json",
            "application/graphql-response+json",
        };
        KnownContentTypes = knownContentTypes;

        string[] pathHints =
        [
            "/graphql",
            "/api/graphql",
            "/v1/graphql",
            "/v2/graphql",
        ];
        PathHints = pathHints;

        var operationNamePattern = BuildOperationNameRegex();
        OperationNamePattern = operationNamePattern;
    }

    /// <summary>
    ///     Gets the GraphQL operation name for the supplied request.
    /// </summary>
    /// <param name="request">The request to classify.</param>
    /// <returns>The operation name, or <see langword="null" />.</returns>
    public static string? GetOperationName(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return null;
        }

        var contentTypeHeader = request.Headers.Get("Content-Type");
        if (!HasGraphQueryLanguageIndicator(request.RequestUri.AbsolutePath, contentTypeHeader))
        {
            return null;
        }

        try
        {
            if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractFromUrlQuery(request.RequestUri);
            }

            if (request.Body.IsEmpty)
            {
                return null;
            }

            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            if (HasRawGraphQueryLanguageContentType(contentTypeHeader))
            {
                return ExtractOperationNameFromSource(bodyText);
            }

            return ExtractFromJsonBody(bodyText);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"(?:^|[\s,{])(query|mutation|subscription)\s+([_A-Za-z][_0-9A-Za-z]*)\b", RegexOptions.CultureInvariant)]
    private static partial Regex BuildOperationNameRegex();

    private static string? ExtractFromJsonBody(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!root.TryGetProperty("query", out var queryElement) || queryElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var query = queryElement.GetString();
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            if (root.TryGetProperty("operationName", out var operationElement) && operationElement.ValueKind == JsonValueKind.String)
            {
                var explicitOperationName = operationElement.GetString();
                if (!string.IsNullOrEmpty(explicitOperationName))
                {
                    return explicitOperationName;
                }
            }

            return ExtractOperationNameFromSource(query);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GraphQL request body is not valid JSON.", ex);
        }
    }

    private static string? ExtractFromUrlQuery(Uri requestUri)
    {
        var query = string.Empty;
        string? operationName = null;

        var parameters = QueryStringParser.Parse(requestUri);
        foreach (var parameter in parameters)
        {
            if (string.Equals(parameter.Name, "query", StringComparison.OrdinalIgnoreCase))
            {
                query = parameter.Value;
                continue;
            }

            if (string.Equals(parameter.Name, "operationName", StringComparison.OrdinalIgnoreCase))
            {
                operationName = parameter.Value;
            }
        }

        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(operationName))
        {
            return operationName;
        }

        return ExtractOperationNameFromSource(query);
    }

    private static string? ExtractOperationNameFromSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var match = OperationNamePattern.Match(source);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups[2].Value;
    }

    private static bool HasGraphQueryLanguageContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separatorIndex < 0 ? contentType.Trim() : contentType[..separatorIndex].Trim();
        return KnownContentTypes.Contains(mediaType);
    }

    private static bool HasGraphQueryLanguageIndicator(string? path, string? contentType)
    {
        if (HasGraphQueryLanguageContentType(contentType))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var hint in PathHints)
        {
            if (path.EndsWith(hint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasRawGraphQueryLanguageContentType(string? contentTypeHeader)
    {
        if (string.IsNullOrWhiteSpace(contentTypeHeader))
        {
            return false;
        }

        return contentTypeHeader.Contains("application/graphql", StringComparison.OrdinalIgnoreCase)
            && !contentTypeHeader.Contains("+json", StringComparison.OrdinalIgnoreCase)
            && !contentTypeHeader.Contains("-response+json", StringComparison.OrdinalIgnoreCase);
    }
}
