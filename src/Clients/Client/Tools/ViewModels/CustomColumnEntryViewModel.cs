using Proxyfan.Domain.Traffic.Columns;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Read-only view model that exposes a single
///     <see cref="CustomColumnDefinition" /> as a row in the Custom Columns tool window.
/// </summary>
public sealed class CustomColumnEntryViewModel
{
    /// <summary>
    ///     Gets the underlying column definition. Used by the parent view model's
    ///     remove/update commands to identify the row.
    /// </summary>
    public CustomColumnDefinition Definition { get; }

    /// <summary>
    ///     Gets the human-readable column header text.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Gets the case-insensitive header key the column extracts.
    /// </summary>
    public string HeaderKey { get; }

    /// <summary>
    ///     Gets the side of the exchange the column reads from (request or response).
    /// </summary>
    public CustomColumnSource Source { get; }

    /// <summary>
    ///     Initializes a new <see cref="CustomColumnEntryViewModel" /> wrapping a column definition.
    /// </summary>
    /// <param name="definition">The column definition exposed to the UI.</param>
    public CustomColumnEntryViewModel(CustomColumnDefinition definition)
    {
        Definition = definition;
        DisplayName = definition.DisplayName;
        HeaderKey = definition.HeaderKey;
        Source = definition.Source;
    }
}
