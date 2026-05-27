using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using System.Globalization;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only view model representing a single (pattern, local response) entry
///     in the Map Local tool window.
/// </summary>
public sealed class MapLocalEntryViewModel
{
    /// <summary>
    ///     Gets the underlying domain entry.
    /// </summary>
    public MapLocalEntry Entry { get; }

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
    ///     Gets the configured HTTP status code as a human-readable string.
    /// </summary>
    public string Status { get; }

    /// <summary>
    ///     Initializes a new <see cref="MapLocalEntryViewModel" /> wrapping a domain entry.
    /// </summary>
    /// <param name="entry">The mapping entry to expose to the UI.</param>
    public MapLocalEntryViewModel(MapLocalEntry entry)
    {
        Entry = entry;
        Pattern = entry.MatchingRule.Pattern;
        Kind = entry.MatchingRule.Kind;
        IsEnabled = entry.IsEnabled;
        Status = $"{entry.StatusCode.ToString(CultureInfo.InvariantCulture)} {entry.ReasonPhrase}";
    }
}
