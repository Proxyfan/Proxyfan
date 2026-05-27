namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parsed User-Agent header, decomposing the raw text into product name and version
///     (best-effort; user agents are notoriously inconsistent).
/// </summary>
public sealed class UserAgentInfo
{
    /// <summary>
    ///     Gets the parsed product name (e.g. <c>"Mozilla"</c>, <c>"curl"</c>).
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    ///     Gets the parsed product version (e.g. <c>"5.0"</c>, <c>"7.85.0"</c>), or empty when absent.
    /// </summary>
    public string ProductVersion { get; }

    /// <summary>
    ///     Gets the raw User-Agent header value.
    /// </summary>
    public string RawValue { get; }

    /// <summary>
    ///     Initializes a new <see cref="UserAgentInfo" />.
    /// </summary>
    /// <param name="productName">The product name.</param>
    /// <param name="productVersion">The product version.</param>
    /// <param name="rawValue">The raw header value.</param>
    public UserAgentInfo(string productName, string productVersion, string rawValue)
    {
        ProductName = productName;
        ProductVersion = productVersion;
        RawValue = rawValue;
    }
}
