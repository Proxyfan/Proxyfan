using Proxyfan.Domain.Rules.Matching;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     A single map-remote mapping consisting of a URL pattern and its rewritten destination.
/// </summary>
public sealed class MapRemoteEntry
{
    /// <summary>
    ///     Gets the destination components that replace the matching request's URL.
    /// </summary>
    public required MapRemoteDestination Destination { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this entry is active.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    ///     Gets the URL matching rule used to select requests for rewriting.
    /// </summary>
    public required MatchingRule MatchingRule { get; init; }
}
