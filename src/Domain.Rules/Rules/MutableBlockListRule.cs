using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     A mutable Block List rule suitable for runtime editing from the user interface.
///     Patterns can be added or removed at any time; the rule's <see cref="EvaluateRequest" />
///     observes the latest snapshot of compiled matchers without taking a lock.
///     A single instance is intended to be registered with <see cref="IRuleRegistry" /> once
///     and mutated throughout the application lifetime.
/// </summary>
public sealed class MutableBlockListRule : IRequestPhaseRule
{
    /// <summary>
    ///     Raised whenever the rule's enabled state or pattern collection changes.
    /// </summary>
    public event MutableBlockListChanged? Changed;

    private readonly Lock _mutationLock;
    private readonly List<MatchingRule> _patterns;
    private volatile bool _isEnabled;
    private volatile IReadOnlyList<IUrlMatcher> _matchers;

    /// <summary>
    ///     Initializes a new empty <see cref="MutableBlockListRule" />.
    /// </summary>
    /// <param name="priority">
    ///     The rule's priority within request-phase rules; lower priorities execute earlier.
    /// </param>
    /// <param name="isEnabled">Whether the rule is initially active.</param>
    public MutableBlockListRule(int priority, bool isEnabled)
    {
        Priority = priority;
        _isEnabled = isEnabled;
        _patterns = [];
        _matchers = [];
        var mutationLock = new Lock();
        _mutationLock = mutationLock;
    }

    /// <inheritdoc />
    public RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var snapshot = _matchers;
        if (snapshot.Count == 0)
        {
            return null;
        }

        var url = request.RequestUri.ToString();
        foreach (var matcher in snapshot)
        {
            if (matcher.HasMatch(url))
            {
                return new RequestPipelineAction.Block();
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc />
    public int Priority { get; }

    /// <summary>
    ///     Adds a new pattern to the block list. Duplicate patterns (same pattern text and kind)
    ///     are ignored.
    /// </summary>
    /// <param name="rule">The matching rule to add.</param>
    public void AddPattern(MatchingRule rule)
    {
        lock (_mutationLock)
        {
            foreach (var existing in _patterns)
            {
                if (existing.Kind == rule.Kind && existing.Pattern == rule.Pattern)
                {
                    return;
                }
            }

            var rebuilt = BuildMatchersWithAppendedRuleUnderLock(rule);
            _patterns.Add(rule);
            _matchers = rebuilt;
        }

        RaiseChanged();
    }

    /// <summary>
    ///     Returns the current snapshot of matching rules in registration order.
    /// </summary>
    /// <returns>An immutable snapshot of the patterns currently configured.</returns>
    public IReadOnlyList<MatchingRule> GetPatterns()
    {
        lock (_mutationLock)
        {
            return [.. _patterns];
        }
    }

    /// <summary>
    ///     Removes the supplied pattern from the block list. The first matching entry
    ///     (same pattern text and kind) is removed.
    /// </summary>
    /// <param name="rule">The matching rule to remove.</param>
    public void RemovePattern(MatchingRule rule)
    {
        var removed = false;
        lock (_mutationLock)
        {
            for (var index = 0; index < _patterns.Count; index++)
            {
                var existing = _patterns[index];
                if (existing.Kind == rule.Kind && existing.Pattern == rule.Pattern)
                {
                    _patterns.RemoveAt(index);
                    RebuildMatchersUnderLock();
                    removed = true;
                    break;
                }
            }
        }

        if (removed)
        {
            RaiseChanged();
        }
    }

    /// <summary>
    ///     Enables or disables the rule. When disabled the rule evaluates to <see langword="null" />
    ///     in the pipeline (handled by <see cref="RuleEngine" />).
    /// </summary>
    /// <param name="isEnabled">Whether the rule should be active.</param>
    public void SetEnabled(bool isEnabled)
    {
        if (_isEnabled == isEnabled)
        {
            return;
        }

        _isEnabled = isEnabled;
        RaiseChanged();
    }

    private List<IUrlMatcher> BuildMatchersWithAppendedRuleUnderLock(MatchingRule rule)
    {
        var rebuilt = new List<IUrlMatcher>(_patterns.Count + 1);
        foreach (var pattern in _patterns)
        {
            rebuilt.Add(pattern.Compile());
        }

        rebuilt.Add(rule.Compile());
        return rebuilt;
    }

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }

    private void RebuildMatchersUnderLock()
    {
        var rebuilt = new List<IUrlMatcher>(_patterns.Count);
        foreach (var pattern in _patterns)
        {
            rebuilt.Add(pattern.Compile());
        }

        _matchers = rebuilt;
    }
}
