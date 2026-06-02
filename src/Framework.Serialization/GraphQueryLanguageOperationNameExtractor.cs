using System;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Extracts the first named operation from a Graph Query Language (GraphQL) query
///     source. GraphQL documents may contain anonymous (<c>{ ... }</c>) or named
///     (<c>query Foo { ... }</c>, <c>mutation Bar { ... }</c>,
///     <c>subscription Baz { ... }</c>) operations and may also define fragments
///     before the first operation. When the request payload does not specify an
///     explicit <c>operationName</c>, this helper recovers it from the source so the
///     traffic list can display something meaningful.
/// </summary>
public static class GraphQueryLanguageOperationNameExtractor
{
    private const string FragmentKeyword = "fragment";
    private static readonly string[] OperationKeywords;

    static GraphQueryLanguageOperationNameExtractor()
    {
        var keywords = new[]
        {
            "query",
            "mutation",
            "subscription",
        };
        OperationKeywords = keywords;
    }

    /// <summary>
    ///     Extracts the first named operation from the supplied query source text.
    /// </summary>
    /// <param name="query">The query, mutation, or subscription source.</param>
    /// <returns>The first named operation, or <see langword="null" /> if none is present.</returns>
    public static string? Extract(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var span = query.AsSpan();
        var offset = 0;
        while (true)
        {
            offset = SkipWhitespaceAndComments(span, offset);
            if (offset >= span.Length)
            {
                return null;
            }

            if (span[offset] == '{')
            {
                return null;
            }

            var word = TryReadIdentifier(span, offset);
            if (word is null)
            {
                return null;
            }

            offset += word.Length;

            if (Array.IndexOf(OperationKeywords, word) >= 0)
            {
                offset = SkipWhitespaceAndComments(span, offset);
                if (offset >= span.Length)
                {
                    return null;
                }

                var name = TryReadIdentifier(span, offset);
                return name;
            }

            if (word == FragmentKeyword)
            {
                offset = SkipFragmentDefinition(span, offset);
                continue;
            }

            return null;
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

    private static string? TryReadIdentifier(ReadOnlySpan<char> span, int offset)
    {
        var start = offset;
        var index = offset;
        if (index >= span.Length)
        {
            return null;
        }

        var first = span[index];
        if (!HasIdentifierStartChar(first))
        {
            return null;
        }

        index++;
        while (index < span.Length && HasIdentifierPartChar(span[index]))
        {
            index++;
        }

        var length = index - start;
        if (length == 0)
        {
            return null;
        }

        var identifier = new string(span.Slice(start, length));
        return identifier;
    }
}
