using System;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Projects the Graph Query Language (GraphQL) operation name from a captured HTTP
///     request for surfacing in traffic columns. Returns <see langword="null" /> when
///     the request is not GraphQL or the operation name cannot be determined.
/// </summary>
public static class GraphQueryLanguageRequestClassifier
{
    private static readonly string[] KnownPaths;

    static GraphQueryLanguageRequestClassifier()
    {
        KnownPaths =
        [
            "/graphql",
            "/api/graphql",
            "/v1/graphql",
            "/v2/graphql",
        ];
    }

    /// <summary>
    ///     Returns the operation name from <paramref name="request" />, or
    ///     <see langword="null" /> when the request is not GraphQL or no named operation exists.
    /// </summary>
    /// <param name="request">The captured HTTP request (may be <see langword="null" />).</param>
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

            if (root.TryGetProperty("operationName", out var operationNameElement) && operationNameElement.ValueKind == JsonValueKind.String)
            {
                var operationName = operationNameElement.GetString();
                if (!string.IsNullOrWhiteSpace(operationName))
                {
                    return operationName;
                }
            }

            return ExtractOperationNameFromSource(query);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractFromUrlQuery(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        var query = queryString.Length > 0 && queryString[0] == '?'
            ? queryString[1..]
            : queryString;

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        string? queryValue = null;
        string? operationNameValue = null;
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = pair[..separatorIndex];
            var value = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            if (string.Equals(key, "query", StringComparison.OrdinalIgnoreCase))
            {
                queryValue = value;
                continue;
            }

            if (string.Equals(key, "operationName", StringComparison.OrdinalIgnoreCase))
            {
                operationNameValue = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(operationNameValue))
        {
            return operationNameValue;
        }

        return ExtractOperationNameFromSource(queryValue);
    }

    private static string? ExtractOperationNameFromSource(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var span = query.AsSpan();
        var offset = SkipIgnorable(span, 0);
        if (offset >= span.Length || span[offset] == '{')
        {
            return null;
        }

        var wordLength = MeasureIdentifier(span, offset);
        if (wordLength == 0)
        {
            return null;
        }

        var word = span.Slice(offset, wordLength);
        if (!HasOperationKeyword(word))
        {
            return null;
        }

        offset += wordLength;
        offset = SkipIgnorable(span, offset);
        if (offset >= span.Length)
        {
            return null;
        }

        var nameLength = MeasureIdentifier(span, offset);
        if (nameLength == 0)
        {
            return null;
        }

        return new string(span.Slice(offset, nameLength));
    }

    private static bool HasGraphQueryLanguageContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separatorIndex < 0 ? contentType.Trim() : contentType[..separatorIndex].Trim();
        return string.Equals(mediaType, "application/graphql", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasGraphQueryLanguageIndicator(string? path, string? contentType)
    {
        if (HasGraphQueryLanguageMediaType(contentType))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var knownPath in KnownPaths)
        {
            if (path.EndsWith(knownPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasGraphQueryLanguageMediaType(string? contentType)
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

    private static bool HasIdentifierStartCharacter(char value)
    {
        return value is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_';
    }

    private static bool HasOperationKeyword(ReadOnlySpan<char> word)
    {
        return word.SequenceEqual("query".AsSpan())
            || word.SequenceEqual("mutation".AsSpan())
            || word.SequenceEqual("subscription".AsSpan());
    }

    private static int MeasureIdentifier(ReadOnlySpan<char> source, int offset)
    {
        if (offset >= source.Length || !HasIdentifierStartCharacter(source[offset]))
        {
            return 0;
        }

        var index = offset + 1;
        while (index < source.Length)
        {
            var character = source[index];
            if (!HasIdentifierStartCharacter(character) && (character < '0' || character > '9'))
            {
                break;
            }

            index++;
        }

        return index - offset;
    }

    private static int SkipComment(ReadOnlySpan<char> source, int offset)
    {
        var index = offset;
        while (index < source.Length && source[index] != '\n')
        {
            index++;
        }

        return index;
    }

    private static int SkipIgnorable(ReadOnlySpan<char> source, int offset)
    {
        var index = offset;
        while (index < source.Length)
        {
            var current = source[index];
            if (char.IsWhiteSpace(current) || current == ',')
            {
                index++;
                continue;
            }

            if (current == '#')
            {
                index = SkipComment(source, index);
                continue;
            }

            break;
        }

        return index;
    }
}
