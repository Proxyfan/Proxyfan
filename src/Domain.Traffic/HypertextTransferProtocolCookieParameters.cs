namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Mutable parameter bag for constructing a <see cref="HypertextTransferProtocolCookie" />.
/// </summary>
public sealed class HypertextTransferProtocolCookieParameters
{
    /// <summary>
    ///     Gets or sets the domain attribute.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    ///     Gets or sets the expires attribute.
    /// </summary>
    public string? Expires { get; init; }

    /// <summary>
    ///     Gets or sets the HttpOnly flag.
    /// </summary>
    public bool IsHypertextTransferProtocolOnly { get; init; }

    /// <summary>
    ///     Gets or sets the Secure flag.
    /// </summary>
    public bool IsSecure { get; init; }

    /// <summary>
    ///     Gets or sets the cookie name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets or sets the path attribute.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    ///     Gets or sets the SameSite attribute.
    /// </summary>
    public string? SameSite { get; init; }

    /// <summary>
    ///     Gets or sets the cookie value.
    /// </summary>
    public required string Value { get; init; }
}
