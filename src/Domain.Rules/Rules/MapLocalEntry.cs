using Proxyfan.Domain.Rules.Matching;
using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     A single map-local mapping consisting of a URL pattern and the local response served
///     when the pattern matches.
/// </summary>
public sealed class MapLocalEntry
{
    /// <summary>
    ///     Gets the response body bytes returned by the local response.
    /// </summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    ///     Gets the response headers (zero or more name/value pairs).
    /// </summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Headers { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this entry is active.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    ///     Gets the URL matching rule used to select requests for local response.
    /// </summary>
    public required MatchingRule MatchingRule { get; init; }

    /// <summary>
    ///     Gets the HTTP reason phrase returned with the local response.
    /// </summary>
    public required string ReasonPhrase { get; init; }

    /// <summary>
    ///     Gets the HTTP status code returned by the local response (100-599).
    /// </summary>
    public required int StatusCode { get; init; }
}
