using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Formats a list of <see cref="QueryParameter" /> values as a fixed-width text table
///     suitable for the Query inspector tab.
/// </summary>
public static class QueryStringFormatter
{
    /// <summary>
    ///     Renders the parameters as a fixed-width table with Name and Value columns.
    ///     Returns an empty string when the list is null or empty.
    /// </summary>
    /// <param name="parameters">The query parameters to format.</param>
    /// <returns>The formatted table text.</returns>
    public static string Format(IReadOnlyList<QueryParameter>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return string.Empty;
        }

        var nameWidth = ComputeColumnWidth(parameters, selectName: true, minimum: "Name".Length);
        var valueWidth = ComputeColumnWidth(parameters, selectName: false, minimum: "Value".Length);

        var builder = new StringBuilder();
        builder.Append("Name".PadRight(nameWidth));
        builder.Append("  ");
        builder.AppendLine("Value".PadRight(valueWidth));
        var nameSeparator = new string('-', nameWidth);
        var valueSeparator = new string('-', valueWidth);
        builder.Append(nameSeparator);
        builder.Append("  ");
        builder.AppendLine(valueSeparator);

        foreach (var parameter in parameters)
        {
            builder.Append(parameter.Name.PadRight(nameWidth));
            builder.Append("  ");
            builder.AppendLine(parameter.Value.PadRight(valueWidth));
        }

        return builder.ToString();
    }

    private static int ComputeColumnWidth(IReadOnlyList<QueryParameter> parameters, bool selectName, int minimum)
    {
        var width = minimum;

        foreach (var parameter in parameters)
        {
            string candidate;

            if (selectName)
            {
                candidate = parameter.Name;
            }
            else
            {
                candidate = parameter.Value;
            }

            if (candidate.Length > width)
            {
                width = candidate.Length;
            }
        }

        return width;
    }
}
