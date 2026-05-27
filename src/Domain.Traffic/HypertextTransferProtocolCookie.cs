namespace Proxyfan.Domain.Traffic;

/// <summary>
///     A single Set-Cookie directive parsed from a response header.
/// </summary>
public sealed class HypertextTransferProtocolCookie
{
    /// <summary>
    ///     Gets the domain attribute, or <see langword="null" /> when absent.
    /// </summary>
    public string? Domain { get; }

    /// <summary>
    ///     Gets the expires attribute, or <see langword="null" /> when absent.
    /// </summary>
    public string? Expires { get; }

    /// <summary>
    ///     Gets a value indicating whether the HttpOnly flag is set.
    /// </summary>
    public bool IsHypertextTransferProtocolOnly { get; }

    /// <summary>
    ///     Gets a value indicating whether the Secure flag is set.
    /// </summary>
    public bool IsSecure { get; }

    /// <summary>
    ///     Gets the cookie name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the path attribute, or <see langword="null" /> when absent.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    ///     Gets the SameSite attribute (Strict/Lax/None), or <see langword="null" /> when absent.
    /// </summary>
    public string? SameSite { get; }

    /// <summary>
    ///     Gets the cookie value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolCookie" /> instance.
    /// </summary>
    /// <param name="parameters">The cookie parameters.</param>
    public HypertextTransferProtocolCookie(HypertextTransferProtocolCookieParameters parameters)
    {
        Name = parameters.Name;
        Value = parameters.Value;
        Domain = parameters.Domain;
        Path = parameters.Path;
        Expires = parameters.Expires;
        SameSite = parameters.SameSite;
        IsSecure = parameters.IsSecure;
        IsHypertextTransferProtocolOnly = parameters.IsHypertextTransferProtocolOnly;
    }
}
