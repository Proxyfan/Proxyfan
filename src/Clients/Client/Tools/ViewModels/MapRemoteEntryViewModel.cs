using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only view model representing a single (pattern, destination) entry
///     in the Map Remote tool window.
/// </summary>
public sealed class MapRemoteEntryViewModel
{
    /// <summary>
    ///     Gets a human-readable single-line description of the destination components.
    /// </summary>
    public string Destination { get; }

    /// <summary>
    ///     Gets the underlying domain entry.
    /// </summary>
    public MapRemoteEntry Entry { get; }

    /// <summary>
    ///     Gets a value indicating whether this entry is currently enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    ///     Gets the strategy used to compare URLs against <see cref="Pattern" />.
    /// </summary>
    public MatchingRuleKind Kind { get; }

    /// <summary>
    ///     Gets the URL pattern as a human-readable string.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    ///     Initializes a new <see cref="MapRemoteEntryViewModel" /> wrapping a domain entry.
    /// </summary>
    /// <param name="entry">The mapping entry to expose to the UI.</param>
    public MapRemoteEntryViewModel(MapRemoteEntry entry)
    {
        Entry = entry;
        Pattern = entry.MatchingRule.Pattern;
        Kind = entry.MatchingRule.Kind;
        IsEnabled = entry.IsEnabled;
        Destination = MapRemoteDestinationFormatter.Format(entry.Destination);
    }
}
