namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Column widths for rendering the cookie table in
///     <see cref="HypertextTransferProtocolCookieTextFormatter" />.
/// </summary>
public sealed class CookieColumnWidths
{
    /// <summary>
    ///     Gets the width of the Domain column.
    /// </summary>
    public required int Domain { get; init; }

    /// <summary>
    ///     Gets the width of the Name column.
    /// </summary>
    public required int Name { get; init; }

    /// <summary>
    ///     Gets the width of the Path column.
    /// </summary>
    public required int Path { get; init; }

    /// <summary>
    ///     Gets the width of the Value column.
    /// </summary>
    public required int Value { get; init; }
}
