using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Classifies Graph Query Language (GraphQL) requests and projects their operation name.
/// </summary>
public static class GraphQueryLanguageRequestClassifier
{
    /// <summary>
    ///     Returns the GraphQL operation name for <paramref name="request" />, or
    ///     <see langword="null" /> when the request is not GraphQL or has no named operation.
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
        var path = request.RequestUri.AbsolutePath;
        if (!HasGraphQueryLanguageIndicator(path, contentTypeHeader))
        {
            return null;
        }

        try
        {
            if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractFromUrlQuery(request.RequestUri.Query);
            }

            if (request.Body.IsEmpty)
            {
                return null;
            }

            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            if (HasGraphQueryLanguageContentType(contentTypeHeader))
            {
                return ExtractOperationName(bodyText);
            }

            return ExtractFromJsonBody(bodyText);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

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
                var operationName = operationElement.GetString();
                if (!string.IsNullOrEmpty(operationName))
                {
                    return operationName;
                }
            }

            return ExtractOperationName(query);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GraphQL request body is not valid JSON.", ex);
        }
    }

    private static string? ExtractFromUrlQuery(string queryString)
    {
        var parameters = QueryStringParser.ParseQueryString(queryString);

        string? query = null;
        string? operationName = null;

        foreach (var parameter in parameters)
        {
            if (string.Equals(parameter.Name, "query", StringComparison.OrdinalIgnoreCase))
            {
                query = parameter.Value;
            }
            else if (string.Equals(parameter.Name, "operationName", StringComparison.OrdinalIgnoreCase))
            {
                operationName = parameter.Value;
            }
        }

        if (!string.IsNullOrEmpty(operationName))
        {
            return operationName;
        }

        return ExtractOperationName(query);
    }

    private static string? ExtractOperationName(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var span = query.AsSpan().Trim();
        if (span.Length == 0 || span[0] == '{')
        {
            return null;
        }

        var operationIndex = span.IndexOf("query ".AsSpan(), StringComparison.Ordinal);
        if (operationIndex < 0)
        {
            operationIndex = span.IndexOf("mutation ".AsSpan(), StringComparison.Ordinal);
        }

        if (operationIndex < 0)
        {
            operationIndex = span.IndexOf("subscription ".AsSpan(), StringComparison.Ordinal);
        }

        if (operationIndex < 0)
        {
            return null;
        }

        var operationSpan = span[operationIndex..];
        var firstSpace = operationSpan.IndexOf(' ');
        if (firstSpace < 0 || firstSpace + 1 >= operationSpan.Length)
        {
            return null;
        }

        var remainder = operationSpan[(firstSpace + 1)..].TrimStart();
        var end = 0;
        while (end < remainder.Length)
        {
            var current = remainder[end];
            var isLetter = current is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
            var isDigit = current is >= '0' and <= '9';
            if (!isLetter && !isDigit)
            {
                break;
            }

            end++;
        }

        if (end == 0)
        {
            return null;
        }

        return new string(remainder[..end]);
    }

    private static bool HasGraphQueryLanguageContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("application/graphql", StringComparison.OrdinalIgnoreCase)
            && !contentType.Contains("+json", StringComparison.OrdinalIgnoreCase)
            && !contentType.Contains("-response+json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasGraphQueryLanguageIndicator(string? path, string? contentType)
    {
        if (HasGraphQueryLanguageContentType(contentType))
        {
            return true;
        }

        return HasPathHint(path);
    }

    private static bool HasPathHint(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.EndsWith("/graphql", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/api/graphql", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/v1/graphql", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/v2/graphql", StringComparison.OrdinalIgnoreCase);
    }
}
