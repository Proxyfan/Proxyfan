namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a single name/value pair extracted from a URL query string or
///     <c>application/x-www-form-urlencoded</c> body. Both <see cref="Name" /> and
///     <see cref="Value" /> have already been percent-decoded.
/// </summary>
public sealed class QueryParameter
{
    /// <summary>
    ///     Gets the percent-decoded parameter name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the percent-decoded parameter value. Returns an empty string when the
    ///     original parameter had no <c>=</c> separator or an empty value segment.
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new <see cref="QueryParameter" />.
    /// </summary>
    /// <param name="name">The decoded parameter name.</param>
    /// <param name="value">The decoded parameter value.</param>
    public QueryParameter(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
