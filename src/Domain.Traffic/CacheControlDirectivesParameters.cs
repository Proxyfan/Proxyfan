namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Mutable parameter bag for constructing a <see cref="CacheControlDirectives" /> via
///     <see cref="CacheControlParser" />.
/// </summary>
public sealed class CacheControlDirectivesParameters
{
    /// <summary>
    ///     Gets or sets a value indicating whether the must-revalidate directive is present.
    /// </summary>
    public bool IsMustRevalidate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the no-cache directive is present.
    /// </summary>
    public bool IsNoCache { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the no-store directive is present.
    /// </summary>
    public bool IsNoStore { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the private directive is present.
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the public directive is present.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    ///     Gets or sets the max-age value in seconds.
    /// </summary>
    public long? MaxAgeSeconds { get; set; }

    /// <summary>
    ///     Gets or sets the s-maxage value in seconds.
    /// </summary>
    public long? SharedMaxAgeSeconds { get; set; }
}
