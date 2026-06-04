using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Default thread-safe in-memory <see cref="IRuleRegistry" /> implementation
///     backed by independent locks for request- and response-phase rule lists.
///     Sorted snapshots are cached on registration/unregistration so that the
///     hot path reads them without per-evaluation allocation or sorting.
/// </summary>
public sealed class RuleRegistry : IRuleRegistry
{
    /// <inheritdoc />
    public event RuleRegistryChanged? Changed;

    private readonly Lock _asyncRequestLock;
    private readonly List<IAsyncRequestPhaseRule> _asyncRequestRules;
    private readonly Lock _asyncResponseLock;
    private readonly List<IAsyncResponsePhaseRule> _asyncResponseRules;
    private readonly Lock _requestLock;
    private readonly List<IRequestPhaseRule> _requestRules;
    private readonly Lock _responseLock;
    private readonly List<IResponsePhaseRule> _responseRules;
    private IAsyncRequestPhaseRule[] _asyncRequestSnapshot;
    private IAsyncResponsePhaseRule[] _asyncResponseSnapshot;
    private IRequestPhaseRule[] _requestSnapshot;
    private IResponsePhaseRule[] _responseSnapshot;

    /// <summary>
    ///     Initializes a new empty <see cref="RuleRegistry" />.
    ///     Locks are constructed via temporary locals to satisfy ATXCS058.
    /// </summary>
    public RuleRegistry()
    {
        _requestRules = [];
        _responseRules = [];
        _asyncRequestRules = [];
        _asyncResponseRules = [];
        var requestLock = new Lock();
        var responseLock = new Lock();
        var asyncRequestLock = new Lock();
        var asyncResponseLock = new Lock();
        _requestLock = requestLock;
        _responseLock = responseLock;
        _asyncRequestLock = asyncRequestLock;
        _asyncResponseLock = asyncResponseLock;
        _requestSnapshot = [];
        _responseSnapshot = [];
        _asyncRequestSnapshot = [];
        _asyncResponseSnapshot = [];
    }

    /// <inheritdoc />
    public IReadOnlyList<IAsyncRequestPhaseRule> GetAsyncRequestPhaseRules()
    {
        return Volatile.Read(ref _asyncRequestSnapshot);
    }

    /// <inheritdoc />
    public IReadOnlyList<IAsyncResponsePhaseRule> GetAsyncResponsePhaseRules()
    {
        return Volatile.Read(ref _asyncResponseSnapshot);
    }

    /// <inheritdoc />
    public IReadOnlyList<IRequestPhaseRule> GetRequestPhaseRules()
    {
        return Volatile.Read(ref _requestSnapshot);
    }

    /// <inheritdoc />
    public IReadOnlyList<IResponsePhaseRule> GetResponsePhaseRules()
    {
        return Volatile.Read(ref _responseSnapshot);
    }

    /// <inheritdoc />
    public void RegisterAsyncRequestPhaseRule(IAsyncRequestPhaseRule rule)
    {
        lock (_asyncRequestLock)
        {
            _asyncRequestRules.Add(rule);
            RebuildAsyncRequestSnapshot();
        }

        RaiseChanged();
    }

    /// <inheritdoc />
    public void RegisterAsyncResponsePhaseRule(IAsyncResponsePhaseRule rule)
    {
        lock (_asyncResponseLock)
        {
            _asyncResponseRules.Add(rule);
            RebuildAsyncResponseSnapshot();
        }

        RaiseChanged();
    }

    /// <inheritdoc />
    public void RegisterRequestPhaseRule(IRequestPhaseRule rule)
    {
        lock (_requestLock)
        {
            _requestRules.Add(rule);
            RebuildRequestSnapshot();
        }

        RaiseChanged();
    }

    /// <inheritdoc />
    public void RegisterResponsePhaseRule(IResponsePhaseRule rule)
    {
        lock (_responseLock)
        {
            _responseRules.Add(rule);
            RebuildResponseSnapshot();
        }

        RaiseChanged();
    }

    /// <inheritdoc />
    public void UnregisterAsyncRequestPhaseRule(IAsyncRequestPhaseRule rule)
    {
        bool removed;
        lock (_asyncRequestLock)
        {
            removed = _asyncRequestRules.Remove(rule);
            if (removed)
            {
                RebuildAsyncRequestSnapshot();
            }
        }

        if (removed)
        {
            RaiseChanged();
        }
    }

    /// <inheritdoc />
    public void UnregisterAsyncResponsePhaseRule(IAsyncResponsePhaseRule rule)
    {
        bool removed;
        lock (_asyncResponseLock)
        {
            removed = _asyncResponseRules.Remove(rule);
            if (removed)
            {
                RebuildAsyncResponseSnapshot();
            }
        }

        if (removed)
        {
            RaiseChanged();
        }
    }

    /// <inheritdoc />
    public void UnregisterRequestPhaseRule(IRequestPhaseRule rule)
    {
        bool removed;
        lock (_requestLock)
        {
            removed = _requestRules.Remove(rule);
            if (removed)
            {
                RebuildRequestSnapshot();
            }
        }

        if (removed)
        {
            RaiseChanged();
        }
    }

    /// <inheritdoc />
    public void UnregisterResponsePhaseRule(IResponsePhaseRule rule)
    {
        bool removed;
        lock (_responseLock)
        {
            removed = _responseRules.Remove(rule);
            if (removed)
            {
                RebuildResponseSnapshot();
            }
        }

        if (removed)
        {
            RaiseChanged();
        }
    }

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }

    private void RebuildAsyncRequestSnapshot()
    {
        if (_asyncRequestRules.Count == 0)
        {
            Volatile.Write(ref _asyncRequestSnapshot, []);
            return;
        }

        var snapshot = _asyncRequestRules.ToArray();
        Array.Sort(snapshot, static (left, right) => left.Priority.CompareTo(right.Priority));
        Volatile.Write(ref _asyncRequestSnapshot, snapshot);
    }

    private void RebuildAsyncResponseSnapshot()
    {
        if (_asyncResponseRules.Count == 0)
        {
            Volatile.Write(ref _asyncResponseSnapshot, []);
            return;
        }

        var snapshot = _asyncResponseRules.ToArray();
        Array.Sort(snapshot, static (left, right) => left.Priority.CompareTo(right.Priority));
        Volatile.Write(ref _asyncResponseSnapshot, snapshot);
    }

    private void RebuildRequestSnapshot()
    {
        if (_requestRules.Count == 0)
        {
            Volatile.Write(ref _requestSnapshot, []);
            return;
        }

        var snapshot = _requestRules.ToArray();
        Array.Sort(snapshot, static (left, right) => left.Priority.CompareTo(right.Priority));
        Volatile.Write(ref _requestSnapshot, snapshot);
    }

    private void RebuildResponseSnapshot()
    {
        if (_responseRules.Count == 0)
        {
            Volatile.Write(ref _responseSnapshot, []);
            return;
        }

        var snapshot = _responseRules.ToArray();
        Array.Sort(snapshot, static (left, right) => left.Priority.CompareTo(right.Priority));
        Volatile.Write(ref _responseSnapshot, snapshot);
    }
}
