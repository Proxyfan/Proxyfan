namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate used to extract a string column value from a cookie when rendering the
///     cookie table.
/// </summary>
/// <param name="cookie">The cookie to extract the column from.</param>
/// <returns>The column text.</returns>
public delegate string CookieColumnSelector(HypertextTransferProtocolCookie cookie);
