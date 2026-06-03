namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Describes a malformed non-empty, non-comment configuration line.
/// </summary>
public readonly record struct KeyValueConfigurationParseDiagnostic
{
    /// <summary>
    ///     Gets the original line text.
    /// </summary>
    public string Line { get; }

    /// <summary>
    ///     Gets the 1-based line number in the source text.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    ///     Gets a human-readable description of the parse issue.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KeyValueConfigurationParseDiagnostic" /> struct.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number in the source text.</param>
    /// <param name="line">The original line text.</param>
    /// <param name="message">A human-readable description of the parse issue.</param>
    public KeyValueConfigurationParseDiagnostic(int lineNumber, string line, string message)
    {
        LineNumber = lineNumber;
        Line = line;
        Message = message;
    }
}
