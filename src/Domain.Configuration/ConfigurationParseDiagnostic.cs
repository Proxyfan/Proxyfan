namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Describes a malformed line encountered while parsing a <c>key=value</c>
///     configuration text. A line is considered malformed when it is non-empty, not a
///     comment, yet contains no <c>=</c> separator (or has an empty key before the
///     separator).
/// </summary>
public sealed record ConfigurationParseDiagnostic
{
    /// <summary>
    ///     Gets the raw content of the malformed line as it appeared in the source text.
    /// </summary>
    public required string LineContent { get; init; }

    /// <summary>
    ///     Gets the 1-based line number of the malformed line in the source text.
    /// </summary>
    public required int LineNumber { get; init; }
}
