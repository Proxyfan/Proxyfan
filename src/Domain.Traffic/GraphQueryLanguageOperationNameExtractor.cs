using System;

namespace Proxyfan.Domain.Traffic;

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
