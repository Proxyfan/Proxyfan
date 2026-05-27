namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Extracts the value of a custom column for a given <see cref="TrafficFlow" />.
/// </summary>
public static class CustomColumnValueExtractor
{
    /// <summary>
    ///     Returns the value of <paramref name="column" /> for <paramref name="flow" />, or
    ///     <see cref="string.Empty" /> when the source side (request or response) is
    ///     missing or the header is not present.
    /// </summary>
    /// <param name="column">The column definition that selects header key and side.</param>
    /// <param name="flow">The flow to read from.</param>
    /// <returns>
    ///     The first value of the named header, joined by commas if multiple values exist,
    ///     or an empty string when no value is available.
    /// </returns>
    public static string Extract(CustomColumnDefinition column, TrafficFlow flow)
    {
        var headers = SelectHeaders(column.Source, flow);
        if (headers is null)
        {
            return string.Empty;
        }

        var values = headers.GetAll(column.HeaderKey);
        if (values.Length == 0)
        {
            return string.Empty;
        }

        if (values.Length == 1)
        {
            return values[0];
        }

        return string.Join(", ", values);
    }

    private static HeaderCollection? SelectHeaders(CustomColumnSource source, TrafficFlow flow)
    {
        if (source == CustomColumnSource.Request)
        {
            return flow.Request?.Headers;
        }

        return flow.Response?.Headers;
    }
}
