namespace Proxyfan.Domain.Composer;

/// <summary>
///     A single name/value header entry for a composer request. Headers are stored in
///     insertion order to preserve duplicates and ordering significant to some servers.
/// </summary>
public sealed class ComposerRequestHeader
{
    /// <summary>
    ///     Gets the header name (RFC 7230 token).
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the header value (whitespace-trimmed).
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new <see cref="ComposerRequestHeader" />.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    public ComposerRequestHeader(string name, string value)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Value = value;
    }
}
