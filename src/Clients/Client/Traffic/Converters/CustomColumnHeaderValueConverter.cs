using Avalonia.Data.Converters;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Columns;
using System;
using System.Globalization;

namespace Proxyfan.Client.Traffic.Converters;

/// <summary>
///     Converts a request or response payload into the header value selected by a
///     <see cref="CustomColumnDefinition" />.
/// </summary>
public sealed class CustomColumnHeaderValueConverter : IValueConverter
{
    private const string HeaderValueSeparator = ", ";

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not CustomColumnDefinition definition)
        {
            return string.Empty;
        }

        var headers = GetHeaders(value);
        if (headers is null)
        {
            return string.Empty;
        }

        var values = headers.GetAll(definition.HeaderKey);
        if (values.Length == 0)
        {
            return string.Empty;
        }

        if (values.Length == 1)
        {
            return values[0];
        }

        return string.Join(HeaderValueSeparator, values);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("CustomColumnHeaderValueConverter does not support conversion back.");
    }

    private HeaderCollection? GetHeaders(object? value)
    {
        if (value is HypertextTransferProtocolRequestData request)
        {
            return request.Headers;
        }

        if (value is HypertextTransferProtocolResponseData response)
        {
            return response.Headers;
        }

        return null;
    }
}
