using System;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Classifies captured HTTP requests as Graph Query Language (GraphQL) and extracts the
///     operation name when available.
/// </summary>
public static class GraphQueryLanguageRequestClassifier
{
    private static readonly string[] PathHints;

    static GraphQueryLanguageRequestClassifier()
    {
        var pathHints = new[]
        {
            "/graphql",
            "/api/graphql",
            "/v1/graphql",
            "/v2/graphql",
        };
        PathHints = pathHints;
    }

    /// <summary>
    ///     Returns the GraphQL operation name from a request, or <see langword="null" />.
    /// </summary>
    /// <param name="request">The request to classify.</param>
    /// <returns>The operation name when available; otherwise <see langword="null" />.</returns>
    public static string? GetOperationName(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return null;
        }

        var contentTypeHeader = request.Headers.Get("Content-Type");
        if (!HasIndicator(request.RequestUri.AbsolutePath, contentTypeHeader))
        {
            return null;
        }

        if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return GetOperationNameFromUrlQuery(request.RequestUri.Query);
        }

        if (request.Body.IsEmpty)
        {
            return null;
        }

        var bodyText = Encoding.UTF8.GetString(request.Body.Span);
        if (HasRawGraphQueryLanguageContentType(contentTypeHeader))
        {
            return ExtractOperationName(bodyText);
        }

        return GetOperationNameFromJsonBody(bodyText);
    }

    private static string? ExtractOperationName(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var span = query.AsSpan();
        var offset = SkipWhitespace(span, 0);

        if (offset >= span.Length || span[offset] == '{')
        {
            return null;
        }

        var operationLength = MeasureIdentifier(span, offset);
        if (operationLength == 0)
        {
            return null;
        }

        var operation = span.Slice(offset, operationLength);
        offset += operationLength;
        if (!operation.SequenceEqual("query".AsSpan())
            && !operation.SequenceEqual("mutation".AsSpan())
            && !operation.SequenceEqual("subscription".AsSpan()))
        {
            return null;
        }

        offset = SkipWhitespace(span, offset);
        var nameLength = MeasureIdentifier(span, offset);
        if (nameLength == 0)
        {
            return null;
        }

        return new string(span.Slice(offset, nameLength));
    }

    private static string? GetOperationNameFromJsonBody(string bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(bodyText);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty("query", out var queryElement)
                || queryElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var query = queryElement.GetString();
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            if (document.RootElement.TryGetProperty("operationName", out var operationNameElement)
                && operationNameElement.ValueKind == JsonValueKind.String)
            {
                var operationName = operationNameElement.GetString();
                if (!string.IsNullOrEmpty(operationName))
                {
                    return operationName;
                }
            }

            return ExtractOperationName(query);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetOperationNameFromUrlQuery(string query)
    {
        string? graphQuery = null;
        string? operationName = null;

        var parameters = QueryStringParser.ParseQueryString(query);
        foreach (var parameter in parameters)
        {
            if (string.Equals(parameter.Name, "query", StringComparison.OrdinalIgnoreCase))
            {
                graphQuery = parameter.Value;
            }
            else if (string.Equals(parameter.Name, "operationName", StringComparison.OrdinalIgnoreCase))
            {
                operationName = parameter.Value;
            }
        }

        if (string.IsNullOrEmpty(graphQuery))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(operationName))
        {
            return operationName;
        }

        return ExtractOperationName(graphQuery);
    }

    private static bool HasIndicator(string? urlPath, string? contentType)
    {
        if (HasMatchingContentType(contentType))
        {
            return true;
        }

        return HasMatchingPath(urlPath);
    }

    private static bool HasMatchingContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separatorIndex < 0 ? contentType.Trim() : contentType[..separatorIndex].Trim();
        return string.Equals(mediaType, "application/graphql", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mediaType, "application/graphql+json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mediaType, "application/graphql-response+json", StringComparison.OrdinalIgnoreCase);
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

    private static int MeasureIdentifier(ReadOnlySpan<char> span, int offset)
    {
        if (offset >= span.Length || (!char.IsLetter(span[offset]) && span[offset] != '_'))
        {
            return 0;
        }

        var index = offset + 1;
        while (index < span.Length)
        {
            var current = span[index];
            if (!(char.IsLetterOrDigit(current) || current == '_'))
            {
                break;
            }

            index++;
        }

        return index - offset;
    }

    private static int SkipWhitespace(ReadOnlySpan<char> span, int offset)
    {
        var index = offset;
        while (index < span.Length && char.IsWhiteSpace(span[index]))
        {
            index++;
        }

        return index;
    }
}
