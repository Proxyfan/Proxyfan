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
    private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>>> _entries;
    private readonly Lock _syncRoot;
    private readonly LinkedList<KeyValuePair<string, Lazy<X509Certificate2>>> _usageOrder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LeafCertificateCache" /> class.
    /// </summary>
    /// <param name="capacity">The maximum number of certificates to retain.</param>
    public LeafCertificateCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        var entries = new Dictionary<string, LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>>>(StringComparer.OrdinalIgnoreCase);
        var syncRoot = new Lock();
        var usageOrder = new LinkedList<KeyValuePair<string, Lazy<X509Certificate2>>>();
        _entries = entries;
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
            if (_entries.TryGetValue(hostname, out LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>>? entry))
            {
                _entries.Remove(hostname);
                _usageOrder.Remove(entry);
            }
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

        Lazy<X509Certificate2> lazy;
        lock (_syncRoot)
        {
            if (_entries.TryGetValue(hostname, out LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>>? existingEntry))
            {
                MoveToFront(existingEntry);
                lazy = existingEntry.Value.Value;
            }
            else
            {
                var newLazy = new Lazy<X509Certificate2>(() => factory(hostname), LazyThreadSafetyMode.ExecutionAndPublication);
                lazy = newLazy;
                var cacheEntry = new KeyValuePair<string, Lazy<X509Certificate2>>(hostname, lazy);
                var node = _usageOrder.AddFirst(cacheEntry);
                _entries[hostname] = node;
                RemoveLeastRecentlyUsedWhenRequired();
            }
        }

        try
        {
            return lazy.Value;
        }
        catch
        {
            lock (_syncRoot)
            {
                if (_entries.TryGetValue(hostname, out LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>>? failedNode)
                    && ReferenceEquals(failedNode.Value.Value, lazy))
                {
                    _entries.Remove(hostname);
                    _usageOrder.Remove(failedNode);
                }
            }

            throw;
        }
    }

    private void MoveToFront(LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>> node)
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

        LinkedListNode<KeyValuePair<string, Lazy<X509Certificate2>>>? oldestEntry = _usageOrder.Last;

        if (oldestEntry is null)
        {
            return;
        }

        _entries.Remove(oldestEntry.Value.Key);
        _usageOrder.RemoveLast();
    }

    /// <summary>
    ///     Represents a factory that creates a certificate for the specified host name.
    /// </summary>
    /// <param name="hostname">The host name for which to create a certificate.</param>
    /// <returns>The generated certificate.</returns>
    public delegate X509Certificate2 CertificateFactory(string hostname);
}