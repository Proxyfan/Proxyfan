using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Diff;

namespace Proxyfan.Domain.Traffic.Tests.Diff;

/// <summary>
///     Tests for <see cref="UnifiedDiffFormatter" />.
/// </summary>
public sealed class UnifiedDiffFormatterTests
{
    /// <summary>
    ///     Verifies that an identical diff renders as "(no differences)".
    /// </summary>
    [Test]
    public async Task Format_IdenticalDiff_ReturnsNoDifferencesText()
    {
        var diff = BuildEmptyDiff(isIdentical: true);

        var formatted = UnifiedDiffFormatter.Format(diff);

        await Assert.That(formatted).IsEqualTo("(no differences)");
    }

    /// <summary>
    ///     Verifies that a diff with URL change renders a sectioned unified diff.
    /// </summary>
    [Test]
    public async Task Format_UrlChange_RendersSectionHeader()
    {
        var urlSegments = new List<LineDiffSegment>();
        var deleteSegment = new LineDiffSegment
        {
            NewLineNumber = null,
            OldLineNumber = 1,
            Operation = LineDiffOperation.Delete,
            Text = "https://example.com/one",
        };
        var insertSegment = new LineDiffSegment
        {
            NewLineNumber = 1,
            OldLineNumber = null,
            Operation = LineDiffOperation.Insert,
            Text = "https://example.com/two",
        };
        urlSegments.Add(deleteSegment);
        urlSegments.Add(insertSegment);

        var diff = new TrafficFlowDiff
        {
            IsIdentical = false,
            Method = new List<LineDiffSegment>(),
            RequestBody = new List<LineDiffSegment>(),
            RequestHeaders = new List<LineDiffSegment>(),
            ResponseBody = new List<LineDiffSegment>(),
            ResponseHeaders = new List<LineDiffSegment>(),
            Status = new List<LineDiffSegment>(),
            Url = urlSegments,
        };

        var formatted = UnifiedDiffFormatter.Format(diff);

        await Assert.That(formatted).Contains("--- URL (old)");
        await Assert.That(formatted).Contains("+++ URL (new)");
        await Assert.That(formatted).Contains("-https://example.com/one");
        await Assert.That(formatted).Contains("+https://example.com/two");
    }

    /// <summary>
    ///     Verifies that all-equal sections are skipped from the unified output.
    /// </summary>
    [Test]
    public async Task Format_AllEqualSection_IsSkipped()
    {
        var equalSegments = new List<LineDiffSegment>();
        var segment = new LineDiffSegment
        {
            NewLineNumber = 1,
            OldLineNumber = 1,
            Operation = LineDiffOperation.Equal,
            Text = "GET",
        };
        equalSegments.Add(segment);

        var diff = new TrafficFlowDiff
        {
            IsIdentical = false,
            Method = equalSegments,
            RequestBody = new List<LineDiffSegment>(),
            RequestHeaders = new List<LineDiffSegment>(),
            ResponseBody = new List<LineDiffSegment>(),
            ResponseHeaders = new List<LineDiffSegment>(),
            Status = new List<LineDiffSegment>(),
            Url = new List<LineDiffSegment>(),
        };

        var formatted = UnifiedDiffFormatter.Format(diff);

        await Assert.That(formatted).DoesNotContain("--- Method");
    }

    private static TrafficFlowDiff BuildEmptyDiff(bool isIdentical)
    {
        var diff = new TrafficFlowDiff
        {
            IsIdentical = isIdentical,
            Method = new List<LineDiffSegment>(),
            RequestBody = new List<LineDiffSegment>(),
            RequestHeaders = new List<LineDiffSegment>(),
            ResponseBody = new List<LineDiffSegment>(),
            ResponseHeaders = new List<LineDiffSegment>(),
            Status = new List<LineDiffSegment>(),
            Url = new List<LineDiffSegment>(),
        };
        return diff;
    }
}
