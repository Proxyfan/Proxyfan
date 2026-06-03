namespace Proxyfan.Domain.Configuration;

/// <summary>
///     A malformed line discovered while parsing a key-value configuration file.
/// </summary>
public sealed record KeyValueConfigurationParseDiagnostic
{
    /// <summary>
    ///     Gets the one-based line number that could not be parsed.
    /// </summary>
    public int Line { get; }

    /// <summary>
    ///     Gets the human-readable diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Initializes a new <see cref="KeyValueConfigurationParseDiagnostic" />.
    /// </summary>
    /// <param name="line">The one-based line number that could not be parsed.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    public KeyValueConfigurationParseDiagnostic(int line, string message)
    {
        Line = line;
        Message = message;
    }
}
