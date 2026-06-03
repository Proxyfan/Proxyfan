using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Provides an in-memory least-recently-used cache for leaf certificates.
/// </summary>
public sealed class LeafCertificateCache : ICertificateCache
{
    private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, X509Certificate2>>> _entries;
    private readonly Dictionary<string, Lazy<X509Certificate2>> _pendingEntries;
    private readonly Lock _syncRoot;
    private readonly LinkedList<KeyValuePair<string, X509Certificate2>> _usageOrder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LeafCertificateCache" /> class.
    /// </summary>
    /// <param name="capacity">The maximum number of certificates to retain.</param>
    public LeafCertificateCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        var entries = new Dictionary<string, LinkedListNode<KeyValuePair<string, X509Certificate2>>>(StringComparer.OrdinalIgnoreCase);
        var pendingEntries = new Dictionary<string, Lazy<X509Certificate2>>(StringComparer.OrdinalIgnoreCase);
        var syncRoot = new Lock();
        var usageOrder = new LinkedList<KeyValuePair<string, X509Certificate2>>();
        _entries = entries;
        _pendingEntries = pendingEntries;
        _syncRoot = syncRoot;
        _usageOrder = usageOrder;
    }

    /// <inheritdoc />
    public int Capacity { get; }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_syncRoot)
        {
            _entries.Clear();
            _pendingEntries.Clear();
            _usageOrder.Clear();
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.Count;
            }
        }
    }

    /// <inheritdoc />
    public void Evict(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Host name must be provided.", nameof(hostname));
        }

        lock (_syncRoot)
        {
            if (_entries.TryGetValue(hostname, out LinkedListNode<KeyValuePair<string, X509Certificate2>>? entry))
            {
                _entries.Remove(hostname);
                _usageOrder.Remove(entry);
            }

            _pendingEntries.Remove(hostname);
        }
    }

    /// <summary>
    ///     Gets a cached certificate for the specified host name or creates and stores one when missing.
    /// </summary>
    /// <param name="hostname">The host name to retrieve.</param>
    /// <param name="factory">The certificate factory used when the host name is not cached.</param>
    /// <returns>The cached or newly created certificate.</returns>
    public X509Certificate2 GetOrAdd(string hostname, CertificateFactory factory)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Host name must be provided.", nameof(hostname));
        }

        var pendingEntry = GetOrCreatePendingEntry(hostname, factory, out X509Certificate2? existingCertificate);

        if (existingCertificate is not null)
        {
            return existingCertificate;
        }

        return ResolvePendingEntry(hostname, pendingEntry!);
    }

    private Lazy<X509Certificate2>? GetOrCreatePendingEntry(string hostname, CertificateFactory factory, out X509Certificate2? existingCertificate)
    {
        lock (_syncRoot)
        {
            if (_entries.TryGetValue(hostname, out LinkedListNode<KeyValuePair<string, X509Certificate2>>? existingEntry))
            {
                MoveToFront(existingEntry);
                existingCertificate = existingEntry.Value.Value;
                return null;
            }

            if (_pendingEntries.TryGetValue(hostname, out Lazy<X509Certificate2>? existingPendingEntry))
            {
                existingCertificate = null;
                return existingPendingEntry;
            }

            var pendingEntry = new Lazy<X509Certificate2>(() => factory(hostname), LazyThreadSafetyMode.ExecutionAndPublication);
            _pendingEntries[hostname] = pendingEntry;
            existingCertificate = null;
            return pendingEntry;
        }
    }

    private void MoveToFront(LinkedListNode<KeyValuePair<string, X509Certificate2>> node)
    {
        _usageOrder.Remove(node);
        _usageOrder.AddFirst(node);
    }

    private void RemoveLeastRecentlyUsedWhenRequired()
    {
        if (_entries.Count <= Capacity)
        {
            return;
        }

        LinkedListNode<KeyValuePair<string, X509Certificate2>>? oldestEntry = _usageOrder.Last;

        if (oldestEntry is null)
        {
            return;
        }

        _entries.Remove(oldestEntry.Value.Key);
        _usageOrder.RemoveLast();
    }

    private void RemovePendingEntry(string hostname, Lazy<X509Certificate2> pendingEntry)
    {
        lock (_syncRoot)
        {
            if (_pendingEntries.TryGetValue(hostname, out Lazy<X509Certificate2>? registeredPendingEntry)
                && ReferenceEquals(registeredPendingEntry, pendingEntry))
            {
                _pendingEntries.Remove(hostname);
            }
        }
    }

    private X509Certificate2 ResolvePendingEntry(string hostname, Lazy<X509Certificate2> pendingEntry)
    {
        try
        {
            var certificate = pendingEntry.Value;

            lock (_syncRoot)
            {
                if (_entries.TryGetValue(hostname, out LinkedListNode<KeyValuePair<string, X509Certificate2>>? existingEntry))
                {
                    _pendingEntries.Remove(hostname);
                    MoveToFront(existingEntry);
                    return existingEntry.Value.Value;
                }

                if (_pendingEntries.TryGetValue(hostname, out Lazy<X509Certificate2>? registeredPendingEntry)
                    && ReferenceEquals(registeredPendingEntry, pendingEntry))
                {
                    var cacheEntry = new KeyValuePair<string, X509Certificate2>(hostname, certificate);
                    var node = _usageOrder.AddFirst(cacheEntry);
                    _entries[hostname] = node;
                    _pendingEntries.Remove(hostname);
                    RemoveLeastRecentlyUsedWhenRequired();
                }

                return certificate;
            }
        }
        catch
        {
            RemovePendingEntry(hostname, pendingEntry);
            throw;
        }
    }

    /// <summary>
    ///     Represents a factory that creates a certificate for the specified host name.
    /// </summary>
    /// <param name="hostname">The host name for which to create a certificate.</param>
    /// <returns>The generated certificate.</returns>
    public delegate X509Certificate2 CertificateFactory(string hostname);
}