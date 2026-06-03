using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Diff;

namespace Proxyfan.Domain.Traffic.Tests.Diff;

/// <summary>
///     Tests for <see cref="LineDiffer" />.
/// </summary>
public sealed class LineDifferTests
{
    /// <summary>
    ///     Verifies that diffing identical text yields all-equal segments.
    /// </summary>
    [Test]
    public async Task Diff_IdenticalText_ReturnsAllEqual()
    {
        var segments = LineDiffer.Diff("alpha\nbeta\ngamma", "alpha\nbeta\ngamma");

        await Assert.That(segments.Count).IsEqualTo(3);
        foreach (var segment in segments)
        {
            await Assert.That(segment.Operation).IsEqualTo(LineDiffOperation.Equal);
        }
    }

    /// <summary>
    ///     Verifies that diffing an empty old text against a non-empty new text yields only inserts.
    /// </summary>
    [Test]
    public async Task Diff_EmptyOld_ReturnsOnlyInserts()
    {
        var segments = LineDiffer.Diff(string.Empty, "alpha\nbeta");

        await Assert.That(segments.Count).IsEqualTo(2);
        await Assert.That(segments[0].Operation).IsEqualTo(LineDiffOperation.Insert);
        await Assert.That(segments[1].Operation).IsEqualTo(LineDiffOperation.Insert);
        await Assert.That(segments[0].Text).IsEqualTo("alpha");
        await Assert.That(segments[1].Text).IsEqualTo("beta");
    }

    /// <summary>
    ///     Verifies that diffing a non-empty old text against an empty new text yields only deletes.
    /// </summary>
    [Test]
    public async Task Diff_EmptyNew_ReturnsOnlyDeletes()
    {
        var segments = LineDiffer.Diff("alpha\nbeta", string.Empty);

        await Assert.That(segments.Count).IsEqualTo(2);
        await Assert.That(segments[0].Operation).IsEqualTo(LineDiffOperation.Delete);
        await Assert.That(segments[1].Operation).IsEqualTo(LineDiffOperation.Delete);
    }

    /// <summary>
    ///     Verifies that mixed insert/delete/equal segments are produced for a substitution.
    /// </summary>
    [Test]
    public async Task Diff_LineReplaced_ReturnsDeleteAndInsert()
    {
        var segments = LineDiffer.Diff("a\nb\nc", "a\nB\nc");

        await Assert.That(segments.Count).IsEqualTo(4);
        await Assert.That(segments[0].Operation).IsEqualTo(LineDiffOperation.Equal);
        await Assert.That(segments[3].Operation).IsEqualTo(LineDiffOperation.Equal);
    }

    /// <summary>
    ///     Verifies that null input is treated as empty text.
    /// </summary>
    [Test]
    public async Task Diff_NullInputs_TreatedAsEmpty()
    {
        var segments = LineDiffer.Diff(null, null);

        await Assert.That(segments.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that CRLF, LF, and CR line endings are all recognized.
    /// </summary>
    [Test]
    [Arguments("a\r\nb\r\nc")]
    [Arguments("a\nb\nc")]
    [Arguments("a\rb\rc")]
    public async Task Diff_DifferentLineEndings_SplitsIntoThreeLines(string text)
    {
        var segments = LineDiffer.Diff(text, text);

        await Assert.That(segments.Count).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that equal segments carry both old and new line numbers.
    /// </summary>
    [Test]
    public async Task Diff_EqualSegment_HasBothLineNumbers()
    {
        var segments = LineDiffer.Diff("a", "a");

        await Assert.That(segments[0].OldLineNumber).IsEqualTo(1);
        await Assert.That(segments[0].NewLineNumber).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that insert segments have only a new line number.
    /// </summary>
    [Test]
    public async Task Diff_InsertSegment_HasOnlyNewLineNumber()
    {
        var segments = LineDiffer.Diff(string.Empty, "a");

        await Assert.That(segments[0].OldLineNumber).IsNull();
        await Assert.That(segments[0].NewLineNumber).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that delete segments have only an old line number.
    /// </summary>
    [Test]
    public async Task Diff_DeleteSegment_HasOnlyOldLineNumber()
    {
        var segments = LineDiffer.Diff("a", string.Empty);

        await Assert.That(segments[0].OldLineNumber).IsEqualTo(1);
        await Assert.That(segments[0].NewLineNumber).IsNull();
    }

    /// <summary>
    ///     Verifies that inputs whose line-count product exceeds
    ///     <see cref="LineDiffer.MaximumLineCountProduct" /> bypass the quadratic
    ///     longest-common-subsequence matrix and fall back to a coarse delete-all /
    ///     insert-all representation, preventing unbounded matrix allocation.
    /// </summary>
    [Test]
    public async Task Diff_OversizedInputs_ReturnsCoarseDeleteAllInsertAllFallback()
    {
        var oldLines = BuildLines(prefix: "old", count: 2_001);
        var newLines = BuildLines(prefix: "new", count: 2_001);

        var segments = LineDiffer.Diff(oldLines, newLines);

        await Assert.That(segments.Count).IsEqualTo(4_002);
        for (var index = 0; index < 2_001; index++)
        {
            await Assert.That(segments[index].Operation).IsEqualTo(LineDiffOperation.Delete);
            await Assert.That(segments[index].OldLineNumber).IsEqualTo(index + 1);
            await Assert.That(segments[index].NewLineNumber).IsNull();
        }

        for (var index = 0; index < 2_001; index++)
        {
            var segment = segments[2_001 + index];
            await Assert.That(segment.Operation).IsEqualTo(LineDiffOperation.Insert);
            await Assert.That(segment.NewLineNumber).IsEqualTo(index + 1);
            await Assert.That(segment.OldLineNumber).IsNull();
        }
    }

    /// <summary>
    ///     Verifies that identical inputs at the oversized threshold still produce a
    ///     coarse fallback rather than an all-equal LCS result, confirming the guard
    ///     activates strictly on input size rather than on textual similarity.
    /// </summary>
    [Test]
    public async Task Diff_OversizedIdenticalInputs_ReturnsCoarseFallback()
    {
        var text = BuildLines(prefix: "line", count: 2_001);

        var segments = LineDiffer.Diff(text, text);

        await Assert.That(segments.Count).IsEqualTo(4_002);
        await Assert.That(segments[0].Operation).IsEqualTo(LineDiffOperation.Delete);
        await Assert.That(segments[segments.Count - 1].Operation).IsEqualTo(LineDiffOperation.Insert);
    }

    private static string BuildLines(string prefix, int count)
    {
        var builder = new System.Text.StringBuilder(prefix.Length * count + count);
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder.Append(prefix);
            builder.Append(index);
        }

        return builder.ToString();
    }
}
