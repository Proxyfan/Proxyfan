using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Projects the Graph Query Language (GraphQL) operation name from a captured HTTP
///     request for surfacing in the traffic list column. Returns <see langword="null" />
///     when the request is not a GraphQL request or the operation name cannot be determined.
///     All detection and parsing relies solely on standard .NET types.
/// </summary>
public static class GraphQueryLanguageRequestClassifier
{
    private static readonly HashSet<string> KnownContentTypes;
    private static readonly string[] PathHints;

    static GraphQueryLanguageRequestClassifier()
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
    ///     Returns the operation name from the captured request, or <see langword="null" />.
    /// </summary>
    /// <param name="request">The captured HTTP request (may be <see langword="null" />).</param>
    /// <returns>The operation name or <see langword="null" />.</returns>
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
                var query = request.RequestUri.Query;
                if (query.StartsWith('?'))
                {
                    query = query[1..];
                }

                return ExtractFromUrlQuery(query);
            }

            if (request.Body.IsEmpty)
            {
                return null;
            }

            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            if (!string.IsNullOrWhiteSpace(contentTypeHeader)
                && contentTypeHeader.Contains("application/graphql", StringComparison.OrdinalIgnoreCase)
                && !contentTypeHeader.Contains("+json", StringComparison.OrdinalIgnoreCase)
                && !contentTypeHeader.Contains("-response+json", StringComparison.OrdinalIgnoreCase))
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

            return ExtractOperationNameFromSource(query);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GraphQL request body is not valid JSON.", ex);
        }
    }

    private static string? ExtractFromUrlQuery(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        string? query = null;
        string? operationName = null;

        var pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = pair[..separatorIndex];
            var value = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);
            if (string.Equals(key, "query", StringComparison.OrdinalIgnoreCase))
            {
                query = value;
            }
            else if (string.Equals(key, "operationName", StringComparison.OrdinalIgnoreCase))
            {
                operationName = value;
            }
        }

        if (query is null)
        {
            return null;
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

        var span = query.AsSpan();
        var offset = 0;
        while (offset < span.Length)
        {
            offset = SkipWhitespaceAndComments(span, offset);
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
            offset += wordLength;

            if (HasOperationKeyword(word))
            {
                return ReadOperationName(span, offset);
            }

            if (!HasFragmentKeyword(word))
            {
                return null;
            }

            offset = SkipFragmentDefinition(span, offset);
        }

        return null;
    }

    private static bool HasBlockStringStart(ReadOnlySpan<char> span, int offset)
    {
        var hasRoom = offset + 2 < span.Length;
        var matches = hasRoom
            && span[offset] == '"'
            && span[offset + 1] == '"'
            && span[offset + 2] == '"';
        return matches;
    }

    private static bool HasFragmentKeyword(ReadOnlySpan<char> word)
    {
        return word.SequenceEqual("fragment".AsSpan());
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

    private static bool HasGraphQueryLanguageIndicator(string? urlPath, string? contentType)
    {
        if (HasGraphQueryLanguageContentType(contentType))
        {
            return true;
        }

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

    private static bool HasIdentifierPartChar(char value)
    {
        var allowed = HasIdentifierStartChar(value) || value is >= '0' and <= '9';
        return allowed;
    }

    private static bool HasIdentifierStartChar(char value)
    {
        var allowed = value is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_';
        return allowed;
    }

    private static bool HasOperationKeyword(ReadOnlySpan<char> word)
    {
        return word.SequenceEqual("query".AsSpan())
            || word.SequenceEqual("mutation".AsSpan())
            || word.SequenceEqual("subscription".AsSpan());
    }

    private static int MeasureIdentifier(ReadOnlySpan<char> span, int offset)
    {
        if (offset >= span.Length || !HasIdentifierStartChar(span[offset]))
        {
            return 0;
        }

        var index = offset + 1;
        while (index < span.Length && HasIdentifierPartChar(span[index]))
        {
            index++;
        }

        return index - offset;
    }

    private static string? ReadOperationName(ReadOnlySpan<char> span, int offset)
    {
        offset = SkipWhitespaceAndComments(span, offset);
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

    private static int SkipBalancedBraces(ReadOnlySpan<char> span, int offset)
    {
        var index = offset;
        var depth = 0;
        while (index < span.Length)
        {
            var current = span[index];
            if (current == '#')
            {
                index = SkipLineComment(span, index);
                continue;
            }

            if (current == '"')
            {
                index = SkipString(span, index);
                continue;
            }

            if (current == '{')
            {
                depth++;
                index++;
                continue;
            }

            if (current == '}')
            {
                depth--;
                index++;
                if (depth <= 0)
                {
                    return index;
                }

                continue;
            }

            index++;
        }

        return index;
    }

    private static int SkipBlockString(ReadOnlySpan<char> span, int offset)
    {
        var index = offset + 3;
        while (index < span.Length)
        {
            if (span[index] == '\\' && index + 1 < span.Length)
            {
                index += 2;
                continue;
            }

            if (HasBlockStringStart(span, index))
            {
                return index + 3;
            }

            index++;
        }

        return span.Length;
    }

    private static int SkipFragmentDefinition(ReadOnlySpan<char> span, int offset)
    {
        var index = offset;
        while (index < span.Length)
        {
            index = SkipWhitespaceAndComments(span, index);
            if (index >= span.Length)
            {
                return index;
            }

            var current = span[index];
            if (current == '{')
            {
                return SkipBalancedBraces(span, index);
            }

            if (current == '"')
            {
                index = SkipString(span, index);
                continue;
            }

            index++;
        }

        return index;
    }

    private static int SkipLineComment(ReadOnlySpan<char> span, int offset)
    {
        var index = offset;
        while (index < span.Length && span[index] != '\n')
        {
            index++;
        }

        return index;
    }

    private static int SkipRegularString(ReadOnlySpan<char> span, int offset)
    {
        var index = offset + 1;
        while (index < span.Length)
        {
            var current = span[index];
            if (current == '\\' && index + 1 < span.Length)
            {
                index += 2;
                continue;
            }

            if (current is '"' or '\n')
            {
                return current == '"' ? index + 1 : index;
            }

            index++;
        }

        return span.Length;
    }

    private static int SkipString(ReadOnlySpan<char> span, int offset)
    {
        if (HasBlockStringStart(span, offset))
        {
            return SkipBlockString(span, offset);
        }

        return SkipRegularString(span, offset);
    }

    private static int SkipWhitespaceAndComments(ReadOnlySpan<char> span, int offset)
    {
        var index = offset;
        while (index < span.Length)
        {
            var current = span[index];
            if (char.IsWhiteSpace(current) || current == ',')
            {
                index++;
                continue;
            }

            if (current == '#')
            {
                index = SkipLineComment(span, index);
                continue;
            }

            break;
        }

        return index;
    }
}
