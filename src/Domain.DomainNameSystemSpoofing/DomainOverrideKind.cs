namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     Discriminator for the matching strategy of a
///     <see cref="DomainNameSystemOverrideEntry" />.
/// </summary>
public enum DomainOverrideKind
{
    /// <summary>
    ///     The entry matches only when the hostname is exactly equal (case-insensitive)
    ///     to the configured pattern.
    /// </summary>
    Exact = 0,

    /// <summary>
    ///     The entry matches when the hostname ends with the configured suffix (the
    ///     portion after the leading <c>*.</c> wildcard token, including the dot). Bare
    ///     parent-domain matches are excluded — <c>*.example.com</c> does not match
    ///     <c>example.com</c>.
    /// </summary>
    WildcardSuffix = 1,
}
