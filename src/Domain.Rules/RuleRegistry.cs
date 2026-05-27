using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Default thread-safe in-memory <see cref="IRuleRegistry" /> implementation
///     backed by independent locks for request- and response-phase rule lists.
/// </summary>
public sealed class RuleRegistry : IRuleRegistry
{
    /// <inheritdoc />
    public event RuleRegistryChanged? Changed;

    private readonly Lock _requestLock;
    private readonly List<IRequestPhaseRule> _requestRules;
    private readonly Lock _responseLock;
    private readonly List<IResponsePhaseRule> _responseRules;

    /// <summary>
    ///     Initializes a new empty <see cref="RuleRegistry" />.
    ///     Locks are constructed via temporary locals to satisfy ATXCS058.
    /// </summary>
    public RuleRegistry()
    {
        _requestRules = [];
        _responseRules = [];
        var requestLock = new Lock();
        var responseLock = new Lock();
        _requestLock = requestLock;
        _responseLock = responseLock;
    }

    /// <inheritdoc />
    public IReadOnlyList<IRequestPhaseRule> GetRequestPhaseRules()
    {
        lock (_requestLock)
        {
            var snapshot = new List<IRequestPhaseRule>(_requestRules);
            snapshot.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
            return snapshot;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IResponsePhaseRule> GetResponsePhaseRules()
    {
        lock (_responseLock)
        {
            var snapshot = new List<IResponsePhaseRule>(_responseRules);
            snapshot.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
            return snapshot;
        }
    }

    /// <inheritdoc />
    public void RegisterRequestPhaseRule(IRequestPhaseRule rule)
    {
        lock (_requestLock)
        {
            _requestRules.Add(rule);
        }

        RaiseChanged();
    }

    /// <inheritdoc />
    public void RegisterResponsePhaseRule(IResponsePhaseRule rule)
    {
        lock (_responseLock)
        {
            _responseRules.Add(rule);
        }

        RaiseChanged();
    }

    /// <inheritdoc />
    public void UnregisterRequestPhaseRule(IRequestPhaseRule rule)
    {
        bool removed;
        lock (_requestLock)
        {
            removed = _requestRules.Remove(rule);
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
}
