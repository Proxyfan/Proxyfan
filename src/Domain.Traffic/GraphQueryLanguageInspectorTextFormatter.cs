using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Formats a captured HTTP request as a human-readable Graph Query Language (GraphQL)
///     inspector view. Returns an empty string when the request is not detected as GraphQL,
///     or a helpful diagnostic when GraphQL is detected but the payload cannot be parsed.
/// </summary>
public static class GraphQueryLanguageInspectorTextFormatter
{
    private static readonly string[] KnownPaths;

    static GraphQueryLanguageInspectorTextFormatter()
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
    ///     Renders the GraphQL inspector text for the supplied request, or an empty string
    ///     when the request is not GraphQL.
    /// </summary>
    /// <param name="request">The captured HTTP request data (may be <see langword="null" />).</param>
    /// <returns>The GraphQL inspector text or <see cref="string.Empty" />.</returns>
    public static string Format(HypertextTransferProtocolRequestData? request)
    {
        if (request is null)
        {
            return string.Empty;
        }

        var contentTypeHeader = request.Headers.Get("Content-Type");
        var urlPath = request.RequestUri.AbsolutePath;
        if (!HasGraphQueryLanguageIndicator(urlPath, contentTypeHeader))
        {
            return string.Empty;
        }

        var parsed = TryParse(request, contentTypeHeader);
        if (parsed is null)
        {
            return "GraphQL request detected, but the payload could not be parsed.";
        }

        return RenderRequest(parsed);
    }

    private static string FormatVariables(string rawJson)
    {
        if (string.IsNullOrEmpty(rawJson))
        {
            return rawJson;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            using var stream = new MemoryStream();
            var options = new JsonWriterOptions
            {
                Indented = true,
            };

            using (var writer = new Utf8JsonWriter(stream, options))
            {
                document.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return rawJson;
        }
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

    private static bool HasFragmentKeywordMatch(ReadOnlySpan<char> word)
    {
        return word.SequenceEqual("fragment".AsSpan());
    }

    private static bool HasGraphQueryLanguageIndicator(string? urlPath, string? contentType)
    {
        if (HasGraphQueryLanguageMediaType(contentType))
        {
            return true;
        }

        return HasKnownGraphQueryLanguagePath(urlPath);
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

    private static bool HasKnownGraphQueryLanguagePath(string? urlPath)
    {
        if (string.IsNullOrWhiteSpace(urlPath))
        {
            return false;
        }

        var queryIndex = urlPath.IndexOf('?', StringComparison.Ordinal);
        var pathOnly = queryIndex < 0 ? urlPath : urlPath[..queryIndex];

        foreach (var hint in KnownPaths)
        {
            if (pathOnly.EndsWith(hint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOperationKeywordMatch(ReadOnlySpan<char> word)
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

    private static GraphQueryLanguageRequestData? ParseFromJson(string json)
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

            string? operationName = null;
            if (root.TryGetProperty("operationName", out var operationElement) && operationElement.ValueKind == JsonValueKind.String)
            {
                operationName = operationElement.GetString();
            }

            if (string.IsNullOrEmpty(operationName))
            {
                operationName = TryResolveOperationName(query);
            }

            string? variables = null;
            if (root.TryGetProperty("variables", out var variablesElement) && variablesElement.ValueKind != JsonValueKind.Null && variablesElement.ValueKind != JsonValueKind.Undefined)
            {
                variables = variablesElement.GetRawText();
            }

            return new GraphQueryLanguageRequestData(query, operationName, variables);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("GraphQL request body is not valid JSON.", ex);
        }
    }

    private static GraphQueryLanguageRequestData ParseFromRawQuery(string rawQuery)
    {
        var operationName = TryResolveOperationName(rawQuery);
        return new GraphQueryLanguageRequestData(rawQuery, operationName, null);
    }

    private static GraphQueryLanguageRequestData? ParseFromUrlQuery(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        string? query = null;
        string? operationName = null;
        string? variables = null;

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
            else if (string.Equals(key, "variables", StringComparison.OrdinalIgnoreCase))
            {
                variables = value;
            }
        }

        if (query is null)
        {
            return null;
        }

        var resolvedOperationName = operationName ?? TryResolveOperationName(query);
        return new GraphQueryLanguageRequestData(query, resolvedOperationName, variables);
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

    private static string RenderRequest(GraphQueryLanguageRequestData parsed)
    {
        var builder = new StringBuilder();
        var operationName = string.IsNullOrEmpty(parsed.OperationName) ? "(anonymous)" : parsed.OperationName;
        builder.Append(CultureInfo.InvariantCulture, $"Operation: {operationName}");
        builder.Append('\n');
        builder.Append('\n');
        builder.Append("Query:");
        builder.Append('\n');
        builder.Append(parsed.Query);

        if (!string.IsNullOrEmpty(parsed.Variables))
        {
            builder.Append('\n');
            builder.Append('\n');
            builder.Append("Variables:");
            builder.Append('\n');
            builder.Append(FormatVariables(parsed.Variables));
        }

        return builder.ToString();
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

    private static GraphQueryLanguageRequestData? TryParse(HypertextTransferProtocolRequestData request, string? contentTypeHeader)
    {
        try
        {
            if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var query = request.RequestUri.Query;
                if (query.StartsWith('?'))
                {
                    query = query[1..];
                }

                return ParseFromUrlQuery(query);
            }

            if (request.Body.IsEmpty)
            {
                return null;
            }

            var bodyText = Encoding.UTF8.GetString(request.Body.Span);
            if (string.IsNullOrWhiteSpace(contentTypeHeader))
            {
                return ParseFromJson(bodyText);
            }

            if (contentTypeHeader.Contains("application/graphql", StringComparison.OrdinalIgnoreCase) &&
                !contentTypeHeader.Contains("+json", StringComparison.OrdinalIgnoreCase) &&
                !contentTypeHeader.Contains("-response+json", StringComparison.OrdinalIgnoreCase))
            {
                return ParseFromRawQuery(bodyText);
            }

            return ParseFromJson(bodyText);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static string? TryResolveOperationName(string? query)
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

            if (HasOperationKeywordMatch(word))
            {
                return ReadOperationName(span, offset);
            }

            if (!HasFragmentKeywordMatch(word))
            {
                return null;
            }

            offset = SkipFragmentDefinition(span, offset);
        }

        return null;
    }

    private sealed class GraphQueryLanguageRequestData
    {
        public string? OperationName { get; }

        public string Query { get; }

        public string? Variables { get; }

        public GraphQueryLanguageRequestData(string query, string? operationName, string? variables)
        {
            Query = query;
            OperationName = operationName;
            Variables = variables;
        }
    }
}
