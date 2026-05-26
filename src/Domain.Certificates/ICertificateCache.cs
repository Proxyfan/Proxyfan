namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Defines the contract for a certificate cache.
/// </summary>
public interface ICertificateCache
{
    /// <summary>
    ///     Gets the configured cache capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    ///     Gets the current number of cached certificates.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Removes all cached certificates.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Removes the cached certificate for the specified host name.
    /// </summary>
    /// <param name="hostname">The host name whose cached certificate should be removed.</param>
    void Evict(string hostname);
}