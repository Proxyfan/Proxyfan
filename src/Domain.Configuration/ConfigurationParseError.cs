using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Domain error raised when a configuration file contains one or more malformed lines
///     that cannot be interpreted as valid <c>key=value</c> pairs.
/// </summary>
public sealed record ConfigurationParseError : DomainError
{
    /// <summary>
    ///     Gets the raw text of each line that was rejected during parsing because it was
    ///     neither a comment, an empty line, nor a valid <c>key=value</c> pair.
    /// </summary>
    public IReadOnlyList<string> MalformedLines { get; }

    /// <summary>
    ///     Initializes a new <see cref="ConfigurationParseError" /> carrying the raw text of
    ///     every malformed line that was encountered during parsing.
    /// </summary>
    /// <param name="malformedLines">The raw text of each malformed line.</param>
    public ConfigurationParseError(IReadOnlyList<string> malformedLines)
        : base("CONFIG_MALFORMED", BuildMessage(malformedLines))
    {
        MalformedLines = malformedLines;
    }

    private static string BuildMessage(IReadOnlyList<string> malformedLines)
    {
        return $"Configuration file contains {malformedLines.Count} malformed line(s) that cannot be parsed as key=value pairs.";
    }
}
