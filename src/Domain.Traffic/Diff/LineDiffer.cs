using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     Produces line-based diff segments between two texts using a longest-common-subsequence
///     algorithm. Suitable for comparing HTTP headers and short-to-medium text bodies.
/// </summary>
public static class LineDiffer
{
    /// <summary>
    ///     Maximum allowed product of <c>(oldLines + 1) * (newLines + 1)</c> before the
    ///     full longest-common-subsequence matrix is replaced with a coarse
    ///     delete-all / insert-all fallback. The cap bounds matrix memory to roughly
    ///     16&#8239;MB (4&#8239;million <see cref="int" /> cells) and prevents pathological
    ///     allocations on very large captured text bodies.
    /// </summary>
    public const long MaximumLineCountProduct = 4_000_000L;
    private static readonly string[] LineSeparators;

    static LineDiffer()
    {
        var separators = new string[]
        {
            "\r\n",
            "\n",
            "\r",
        };
        LineSeparators = separators;
    }

    /// <summary>
    ///     Computes a line-based diff between <paramref name="oldText" /> and
    ///     <paramref name="newText" />. Both inputs are split on CR/LF boundaries before
    ///     comparison. The returned segments preserve original line order and are suitable
    ///     for unified-style or side-by-side rendering.
    /// </summary>
    /// <param name="oldText">The original text. <c>null</c> is treated as empty.</param>
    /// <param name="newText">The modified text. <c>null</c> is treated as empty.</param>
    /// <returns>
    ///     An ordered sequence of diff segments describing the transformation from
    ///     <paramref name="oldText" /> to <paramref name="newText" />. When the product
    ///     of the two line counts exceeds <see cref="MaximumLineCountProduct" />, the
    ///     longest-common-subsequence matrix is skipped and a coarse fallback is returned
    ///     that deletes every old line and inserts every new line, preserving correctness
    ///     while bounding memory usage.
    /// </returns>
    public static IReadOnlyList<LineDiffSegment> Diff(string? oldText, string? newText)
    {
        var oldLines = SplitLines(oldText ?? string.Empty);
        var newLines = SplitLines(newText ?? string.Empty);
        if (HasExceededLineCountProduct(oldLines.Length, newLines.Length))
        {
            return BuildCoarseFallback(oldLines, newLines);
        }

        var matrix = BuildLongestCommonSubsequenceMatrix(oldLines, newLines);
        var segments = new List<LineDiffSegment>(oldLines.Length + newLines.Length);
        Backtrack(oldLines, newLines, matrix, segments);
        return segments;
    }

    private static void Backtrack(
        string[] oldLines,
        string[] newLines,
        int[,] matrix,
        List<LineDiffSegment> segments)
    {
        var oldIndex = oldLines.Length;
        var newIndex = newLines.Length;
        var reversed = new List<LineDiffSegment>(oldLines.Length + newLines.Length);
        while (oldIndex > 0 || newIndex > 0)
        {
            if (oldIndex > 0 && newIndex > 0 && oldLines[oldIndex - 1] == newLines[newIndex - 1])
            {
                reversed.Add(BuildEqualSegment(oldLines, oldIndex, newIndex));
                oldIndex--;
                newIndex--;
            }
            else if (newIndex > 0 && (oldIndex == 0 || matrix[oldIndex, newIndex - 1] >= matrix[oldIndex - 1, newIndex]))
            {
                reversed.Add(BuildInsertSegment(newLines, newIndex));
                newIndex--;
            }
            else
            {
                reversed.Add(BuildDeleteSegment(oldLines, oldIndex));
                oldIndex--;
            }
        }

        for (var index = reversed.Count - 1; index >= 0; index--)
        {
            segments.Add(reversed[index]);
        }
    }

    private static List<LineDiffSegment> BuildCoarseFallback(string[] oldLines, string[] newLines)
    {
        var segments = new List<LineDiffSegment>(oldLines.Length + newLines.Length);
        for (var index = 0; index < oldLines.Length; index++)
        {
            segments.Add(BuildDeleteSegment(oldLines, index + 1));
        }

        for (var index = 0; index < newLines.Length; index++)
        {
            segments.Add(BuildInsertSegment(newLines, index + 1));
        }

        return segments;
    }

    private static LineDiffSegment BuildDeleteSegment(string[] oldLines, int oldIndex)
    {
        var segment = new LineDiffSegment
        {
            NewLineNumber = null,
            OldLineNumber = oldIndex,
            Operation = LineDiffOperation.Delete,
            Text = oldLines[oldIndex - 1],
        };
        return segment;
    }

    private static LineDiffSegment BuildEqualSegment(string[] oldLines, int oldIndex, int newIndex)
    {
        var segment = new LineDiffSegment
        {
            NewLineNumber = newIndex,
            OldLineNumber = oldIndex,
            Operation = LineDiffOperation.Equal,
            Text = oldLines[oldIndex - 1],
        };
        return segment;
    }

    private static LineDiffSegment BuildInsertSegment(string[] newLines, int newIndex)
    {
        var segment = new LineDiffSegment
        {
            NewLineNumber = newIndex,
            OldLineNumber = null,
            Operation = LineDiffOperation.Insert,
            Text = newLines[newIndex - 1],
        };
        return segment;
    }

    private static int[,] BuildLongestCommonSubsequenceMatrix(string[] oldLines, string[] newLines)
    {
        var matrix = new int[oldLines.Length + 1, newLines.Length + 1];
        for (var oldIndex = 1; oldIndex <= oldLines.Length; oldIndex++)
        {
            for (var newIndex = 1; newIndex <= newLines.Length; newIndex++)
            {
                if (oldLines[oldIndex - 1] == newLines[newIndex - 1])
                {
                    matrix[oldIndex, newIndex] = matrix[oldIndex - 1, newIndex - 1] + 1;
                }
                else
                {
                    matrix[oldIndex, newIndex] = Math.Max(matrix[oldIndex - 1, newIndex], matrix[oldIndex, newIndex - 1]);
                }
            }
        }

        return matrix;
    }

    private static bool HasExceededLineCountProduct(int oldLineCount, int newLineCount)
    {
        var product = (oldLineCount + 1L) * (newLineCount + 1L);
        return product > MaximumLineCountProduct;
    }

    private static string[] SplitLines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        return text.Split(LineSeparators, StringSplitOptions.None);
    }
}
