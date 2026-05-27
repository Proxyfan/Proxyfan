using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic.Diff;

/// <summary>
///     Structured comparison between two traffic flows, broken down by URL, method,
///     status code, request and response headers, and request and response bodies.
///     Suitable for rendering as side-by-side or unified diff.
/// </summary>
public sealed record TrafficFlowDiff
{
    /// <summary>
    ///     Gets a value indicating whether every section of the diff is empty or
    ///     fully equal (no insertions or deletions in any section).
    /// </summary>
    public required bool IsIdentical { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the HTTP method (single line per side).
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> Method { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the request body when both bodies are text,
    ///     or an empty list when one or both bodies are binary.
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> RequestBody { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the request headers (one header per line, in
    ///     canonical alphabetical order).
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> RequestHeaders { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the response body when both bodies are text,
    ///     or an empty list when one or both bodies are binary.
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> ResponseBody { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the response headers (one header per line, in
    ///     canonical alphabetical order).
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> ResponseHeaders { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the response status code and reason phrase
    ///     (single line per side).
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> Status { get; init; }

    /// <summary>
    ///     Gets the line-by-line diff of the request URL (single line per side).
    /// </summary>
    public required IReadOnlyList<LineDiffSegment> Url { get; init; }
}
