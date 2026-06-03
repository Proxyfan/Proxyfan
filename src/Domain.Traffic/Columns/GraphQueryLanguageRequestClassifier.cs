using System;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Projects the Graph Query Language (GraphQL) operation name from a captured HTTP
///     request for surfacing in the traffic list column. Returns <see langword="null" />
///     when the request is not a GraphQL request or the operation name cannot be determined.
/// </summary>
public static class GraphQueryLanguageRequestClassifier
{
    private static readonly string[] PathHints;

    static GraphQueryLanguageRequestClassifier()
    {
        PathHints =
        [
            "/graphql",
            "/api/graphql",
            "/v1/graphql",
            "/v2/graphql",
        ];
    }

    /// <summary>
    ///     Returns the operation name from a captured request, or <see langword="null" />.
    /// </summary>
    /// <param name="request">The captured request.</param>
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
                return ExtractOperationNameFromSource(bodyText);
            }

            return ExtractFromJsonBody(bodyText);
        }
        catch (JsonException)
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

        if (root.TryGetProperty("operationName", out var operationNameElement)
            && operationNameElement.ValueKind == JsonValueKind.String)
        {
            var operationName = operationNameElement.GetString();
            if (!string.IsNullOrEmpty(operationName))
            {
                return operationName;
            }
        }

        return ExtractOperationNameFromSource(query);
    }

    private static string? ExtractFromUrlQuery(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        string? query = null;
        string? operationName = null;
        var parameters = QueryStringParser.ParseQueryString(queryString);
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

        if (!string.IsNullOrEmpty(operationName))
        {
            return operationName;
        }

        return ExtractOperationNameFromSource(query);
    }

    private static string? ExtractOperationNameFromSource(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var span = query.AsSpan().TrimStart();
        if (span.Length == 0 || span[0] == '{')
        {
            return null;
        }

        var firstWordLength = MeasureIdentifier(span, 0);
        if (firstWordLength == 0)
        {
            return null;
        }

        var firstWord = span[..firstWordLength];
        if (!HasOperationKeyword(firstWord))
        {
            return null;
        }

        var offset = firstWordLength;
        while (offset < span.Length && char.IsWhiteSpace(span[offset]))
        {
            offset++;
        }

        var operationNameLength = MeasureIdentifier(span, offset);
        if (operationNameLength == 0)
        {
            return null;
        }

        return new string(span.Slice(offset, operationNameLength));
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

    private static bool HasGraphQueryLanguageIndicator(string? urlPath, string? contentType)
    {
        if (HasKnownContentType(contentType))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(urlPath))
        {
            return false;
        }

        foreach (var hint in PathHints)
        {
            if (urlPath.EndsWith(hint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasIdentifierPartCharacter(char value)
    {
        return HasIdentifierStartCharacter(value) || value is >= '0' and <= '9';
    }

    private static bool HasIdentifierStartCharacter(char value)
    {
        return value is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_';
    }

    private static bool HasKnownContentType(string? contentType)
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

    private static bool HasOperationKeyword(ReadOnlySpan<char> word)
    {
        return word.SequenceEqual("query".AsSpan())
            || word.SequenceEqual("mutation".AsSpan())
            || word.SequenceEqual("subscription".AsSpan());
    }

    private static int MeasureIdentifier(ReadOnlySpan<char> span, int offset)
    {
        if (offset >= span.Length || !HasIdentifierStartCharacter(span[offset]))
        {
            return 0;
        }

        var index = offset + 1;
        while (index < span.Length && HasIdentifierPartCharacter(span[index]))
        {
            index++;
        }

        return index - offset;
    }
}
