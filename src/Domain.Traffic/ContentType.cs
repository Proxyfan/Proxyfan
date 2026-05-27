namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parsed Content-Type header value: media type plus optional charset and boundary.
/// </summary>
public sealed class ContentType
{
    /// <summary>
    ///     Gets the boundary parameter (used by multipart content types), or null when absent.
    /// </summary>
    public string? Boundary { get; }

    /// <summary>
    ///     Gets the charset parameter, or null when absent.
    /// </summary>
    public string? Charset { get; }

    /// <summary>
    ///     Gets the media type (e.g. <c>"application/json"</c>).
    /// </summary>
    public string MediaType { get; }

    /// <summary>
    ///     Gets the raw header value.
    /// </summary>
    public string RawValue { get; }

    /// <summary>
    ///     Initializes a new <see cref="ContentType" />.
    /// </summary>
    /// <param name="mediaType">The media type.</param>
    /// <param name="charset">The optional charset parameter.</param>
    /// <param name="boundary">The optional boundary parameter.</param>
    /// <param name="rawValue">The raw header value.</param>
    public ContentType(string mediaType, string? charset, string? boundary, string rawValue)
    {
        MediaType = mediaType;
        Charset = charset;
        Boundary = boundary;
        RawValue = rawValue;
    }
}
