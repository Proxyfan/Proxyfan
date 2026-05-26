using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Parameters that configure a <see cref="MapLocalRule" />.
/// </summary>
public sealed class MapLocalRuleParameters
{
    /// <summary>
    ///     Gets the body bytes returned by the local response.
    /// </summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    ///     Gets the response headers (zero or more name/value pairs).
    /// </summary>
    public required IEnumerable<KeyValuePair<string, string>> Headers { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the rule is enabled.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    ///     Gets the rule's priority within request-phase rules.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    ///     Gets the HTTP reason phrase returned with the local response.
    /// </summary>
    public required string ReasonPhrase { get; init; }

    /// <summary>
    ///     Gets the HTTP status code returned by the local response (100-599).
    /// </summary>
    public required int StatusCode { get; init; }
}
