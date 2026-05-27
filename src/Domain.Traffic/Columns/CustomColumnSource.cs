namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Identifies whether a custom column reads its value from a request header or a
///     response header.
/// </summary>
public enum CustomColumnSource
{
    /// <summary>
    ///     The value is extracted from the flow's request headers.
    /// </summary>
    Request,

    /// <summary>
    ///     The value is extracted from the flow's response headers.
    /// </summary>
    Response,
}
