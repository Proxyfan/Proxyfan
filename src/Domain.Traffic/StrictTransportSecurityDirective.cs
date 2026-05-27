namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parsed Strict-Transport-Security header (RFC 6797).
/// </summary>
public sealed class StrictTransportSecurityDirective
{
    /// <summary>
    ///     Gets the includeSubDomains directive flag.
    /// </summary>
    public bool AllowsSubDomains { get; }

    /// <summary>
    ///     Gets the preload directive flag.
    /// </summary>
    public bool IsPreloadable { get; }

    /// <summary>
    ///     Gets the max-age value in seconds.
    /// </summary>
    public long MaxAgeSeconds { get; }

    /// <summary>
    ///     Initializes a new <see cref="StrictTransportSecurityDirective" />.
    /// </summary>
    /// <param name="maxAgeSeconds">The max-age in seconds.</param>
    /// <param name="allowsSubDomains">Whether includeSubDomains is set.</param>
    /// <param name="isPreloadable">Whether preload is set.</param>
    public StrictTransportSecurityDirective(long maxAgeSeconds, bool allowsSubDomains, bool isPreloadable)
    {
        MaxAgeSeconds = maxAgeSeconds;
        AllowsSubDomains = allowsSubDomains;
        IsPreloadable = isPreloadable;
    }
}
