using System;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Extracts the first named operation from a Graph Query Language (GraphQL) query
///     source. GraphQL documents may contain anonymous (<c>{ ... }</c>) or named
///     (<c>query Foo { ... }</c>, <c>mutation Bar { ... }</c>,
///     <c>subscription Baz { ... }</c>) operations. When the request payload does not specify
///     an explicit <c>operationName</c>, this helper recovers it from the source so the
///     traffic list can display something meaningful.
/// </summary>
public static class GraphQueryLanguageOperationNameExtractor
{
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
        var offset = SkipWhitespaceAndComments(span, 0);
        if (offset >= span.Length)
        {
            return null;
        }

        var keyword = TryReadKeyword(span, offset);
        if (keyword is null)
        {
            return null;
        }

        offset += keyword.Length;
        offset = SkipWhitespaceAndComments(span, offset);
        if (offset >= span.Length)
        {
            return null;
        }

        var name = TryReadIdentifier(span, offset);
        return name;
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
                while (index < span.Length && span[index] != '\n')
                {
                    index++;
                }

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

    private static string? TryReadKeyword(ReadOnlySpan<char> span, int offset)
    {
        foreach (var keyword in OperationKeywords)
        {
            var keywordSpan = keyword.AsSpan();
            if (offset + keywordSpan.Length > span.Length)
            {
                continue;
            }

            var slice = span.Slice(offset, keywordSpan.Length);
            if (!slice.SequenceEqual(keywordSpan))
            {
                continue;
            }

            var nextIndex = offset + keywordSpan.Length;
            if (nextIndex < span.Length && HasIdentifierPartChar(span[nextIndex]))
            {
                continue;
            }

            return keyword;
        }

        return null;
    }
}
