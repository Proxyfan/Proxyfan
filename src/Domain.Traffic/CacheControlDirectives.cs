namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parsed Cache-Control header directives per RFC 9111 § 5.2.
/// </summary>
public sealed class CacheControlDirectives
{
    /// <summary>
    ///     Gets a value indicating whether the must-revalidate directive is present.
    /// </summary>
    public bool IsMustRevalidate { get; }

    /// <summary>
    ///     Gets a value indicating whether the no-cache directive is present.
    /// </summary>
    public bool IsNoCache { get; }

    /// <summary>
    ///     Gets a value indicating whether the no-store directive is present.
    /// </summary>
    public bool IsNoStore { get; }

    /// <summary>
    ///     Gets a value indicating whether the private directive is present.
    /// </summary>
    public bool IsPrivate { get; }

    /// <summary>
    ///     Gets a value indicating whether the public directive is present.
    /// </summary>
    public bool IsPublic { get; }

    /// <summary>
    ///     Gets the max-age value in seconds, or null when absent.
    /// </summary>
    public long? MaxAgeSeconds { get; }

    /// <summary>
    ///     Gets the s-maxage value in seconds, or null when absent.
    /// </summary>
    public long? SharedMaxAgeSeconds { get; }

    /// <summary>
    ///     Initializes a new <see cref="CacheControlDirectives" />.
    /// </summary>
    /// <param name="parameters">The parameter bag.</param>
    public CacheControlDirectives(CacheControlDirectivesParameters parameters)
    {
        IsNoCache = parameters.IsNoCache;
        IsNoStore = parameters.IsNoStore;
        IsPrivate = parameters.IsPrivate;
        IsPublic = parameters.IsPublic;
        IsMustRevalidate = parameters.IsMustRevalidate;
        MaxAgeSeconds = parameters.MaxAgeSeconds;
        SharedMaxAgeSeconds = parameters.SharedMaxAgeSeconds;
    }
}
