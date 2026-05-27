using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View-model wrapper for a single <see cref="ComposerHistoryEntry" /> shown in the
///     Composer's history sidebar.
/// </summary>
public sealed class ComposerHistoryEntryViewModel
{
    /// <summary>
    ///     Gets a value indicating whether the entry is starred.
    /// </summary>
    public bool IsStarred => Source.IsStarred;

    /// <summary>
    ///     Gets the HTTP method to display.
    /// </summary>
    public string Method => Source.Method;

    /// <summary>
    ///     Gets the underlying history entry.
    /// </summary>
    public ComposerHistoryEntry Source { get; }

    /// <summary>
    ///     Gets the URL to display.
    /// </summary>
    public string Url => Source.Url;

    /// <summary>
    ///     Initializes a new wrapper around <paramref name="source" />.
    /// </summary>
    /// <param name="source">The underlying history entry.</param>
    public ComposerHistoryEntryViewModel(ComposerHistoryEntry source)
    {
        Source = source;
    }
}
