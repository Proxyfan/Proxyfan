namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Mutable parameter bag for parsing a Set-Cookie header's attributes; populated by
///     <see cref="HypertextTransferProtocolCookieParser" /> as it walks the attribute list.
/// </summary>
public sealed class HypertextTransferProtocolCookieAttributeState
{
    /// <summary>
    ///     Gets or sets the Domain attribute.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    ///     Gets or sets the Expires attribute.
    /// </summary>
    public string? Expires { get; set; }

    /// <summary>
    ///     Gets or sets the HttpOnly flag.
    /// </summary>
    public bool IsHypertextTransferProtocolOnlyFlag { get; set; }

    /// <summary>
    ///     Gets or sets the Secure flag.
    /// </summary>
    public bool IsSecureFlag { get; set; }

    /// <summary>
    ///     Gets or sets the Path attribute.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     Gets or sets the SameSite attribute.
    /// </summary>
    public string? SameSite { get; set; }
}
