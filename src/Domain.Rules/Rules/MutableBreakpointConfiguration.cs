using Proxyfan.Domain.Rules.Matching;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Mutable breakpoint configuration. Holds the set of URL patterns that trigger a
///     breakpoint pause, the active phase mask (request/response/both), and an enabled
///     flag. Designed to be edited live from the user interface; the
///     <see cref="HasRequestMatch" /> and <see cref="HasResponseMatch" /> probes
///     observe the latest compiled snapshot without taking a lock.
/// </summary>
public sealed class MutableBreakpointConfiguration
{
    /// <summary>
    ///     Raised whenever the enabled flag, the phase mask, or the pattern collection changes.
    /// </summary>
    public event MutableBreakpointConfigurationChanged? Changed;
    private const int DefaultMaxPendingPauses = 100;
    private const int DefaultPauseTimeoutSeconds = 60;
    private readonly Lock _mutationLock;
    private readonly List<MatchingRule> _patterns;
    private volatile bool _isBackPressureEnabled;
    private volatile bool _isEnabled;
    private volatile IReadOnlyList<IUrlMatcher> _matchers;
    private volatile int _maxPendingPauses;
    private long _pauseTimeoutTicks;
    private volatile BreakpointPhase _phases;

    /// <summary>
    ///     Gets a value indicating whether back-pressure bypass is enabled when the pause queue is full.
    /// </summary>
    public bool IsBackPressureEnabled => _isBackPressureEnabled;

    /// <summary>
    ///     Gets the configuration's enabled state.
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>
    ///     Gets the maximum number of pending pauses retained before overflow handling is applied.
    /// </summary>
    public int MaxPendingPauses => _maxPendingPauses;

    /// <summary>
    ///     Gets the timeout after which unresolved pauses auto-resume.
    /// </summary>
    public TimeSpan PauseTimeout => TimeSpan.FromTicks(Interlocked.Read(ref _pauseTimeoutTicks));

    /// <summary>
    ///     Gets the currently selected breakpoint phases.
    /// </summary>
    public BreakpointPhase Phases => _phases;

    /// <summary>
    ///     Initializes a new <see cref="MutableBreakpointConfiguration" /> with no patterns,
    ///     <see cref="BreakpointPhase.Both" /> selected, and the supplied <paramref name="isEnabled" />.
    /// </summary>
    /// <param name="isEnabled">Whether the configuration starts enabled.</param>
    public MutableBreakpointConfiguration(bool isEnabled)
    {
        _isBackPressureEnabled = false;
        _isEnabled = isEnabled;
        _matchers = [];
        _maxPendingPauses = DefaultMaxPendingPauses;
        var mutationLock = new Lock();
        _mutationLock = mutationLock;
        _patterns = [];
        _pauseTimeoutTicks = TimeSpan.FromSeconds(DefaultPauseTimeoutSeconds).Ticks;
        _phases = BreakpointPhase.Both;
    }

    /// <summary>
    ///     Adds a new URL pattern to the configuration. Duplicate patterns are ignored.
    /// </summary>
    /// <param name="rule">The matching rule describing the URL pattern.</param>
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

            var compiled = rule.Compile();
            var existingMatchers = _matchers;
            var rebuilt = new List<IUrlMatcher>(existingMatchers.Count + 1);
            rebuilt.AddRange(existingMatchers);
            rebuilt.Add(compiled);

            _patterns.Add(rule);
            _matchers = rebuilt;
        }

        RaiseChanged();
    }

    /// <summary>
    ///     Returns a snapshot of the currently configured URL patterns.
    /// </summary>
    /// <returns>A snapshot of the configured patterns in insertion order.</returns>
    public IReadOnlyList<MatchingRule> GetPatterns()
    {
        lock (_mutationLock)
        {
            return [.. _patterns];
        }
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied <paramref name="url" /> should
    ///     trigger a request-phase breakpoint.
    /// </summary>
    /// <param name="url">The request URL to test.</param>
    /// <returns><see langword="true" /> when a request-phase match exists.</returns>
    public bool HasRequestMatch(string url)
    {
        if (!_isEnabled || (_phases & BreakpointPhase.Request) == 0)
        {
            return false;
        }

        return HasMatch(url);
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied <paramref name="url" /> should
    ///     trigger a response-phase breakpoint.
    /// </summary>
    /// <param name="url">The request URL to test.</param>
    /// <returns><see langword="true" /> when a response-phase match exists.</returns>
    public bool HasResponseMatch(string url)
    {
        if (!_isEnabled || (_phases & BreakpointPhase.Response) == 0)
        {
            return false;
        }

        return HasMatch(url);
    }

    /// <summary>
    ///     Removes the first matching pattern from the configuration.
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
    ///     Enables or disables back-pressure bypass when the pause queue reaches capacity.
    /// </summary>
    /// <param name="isBackPressureEnabled">Whether queue overflow should bypass new pauses.</param>
    public void SetBackPressureEnabled(bool isBackPressureEnabled)
    {
        if (_isBackPressureEnabled == isBackPressureEnabled)
        {
            return;
        }

        _isBackPressureEnabled = isBackPressureEnabled;
        RaiseChanged();
    }

    /// <summary>
    ///     Enables or disables the configuration.
    /// </summary>
    /// <param name="isEnabled">Whether the configuration should be active.</param>
    public void SetEnabled(bool isEnabled)
    {
        if (_isEnabled == isEnabled)
        {
            return;
        }

        _isEnabled = isEnabled;
        RaiseChanged();
    }

    /// <summary>
    ///     Sets the maximum number of pending pauses before overflow handling is applied.
    /// </summary>
    /// <param name="maxPendingPauses">The positive pending-pause cap.</param>
    public void SetMaxPendingPauses(int maxPendingPauses)
    {
        if (maxPendingPauses < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPendingPauses), "Max pending pauses must be at least 1.");
        }

        if (_maxPendingPauses == maxPendingPauses)
        {
            return;
        }

        _maxPendingPauses = maxPendingPauses;
        RaiseChanged();
    }

    /// <summary>
    ///     Sets the pause timeout after which unresolved pauses auto-resume.
    /// </summary>
    /// <param name="pauseTimeout">The strictly positive timeout.</param>
    public void SetPauseTimeout(TimeSpan pauseTimeout)
    {
        if (pauseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pauseTimeout), "Pause timeout must be greater than zero.");
        }

        if (Interlocked.Read(ref _pauseTimeoutTicks) == pauseTimeout.Ticks)
        {
            return;
        }

        Interlocked.Exchange(ref _pauseTimeoutTicks, pauseTimeout.Ticks);
        RaiseChanged();
    }

    /// <summary>
    ///     Sets the phase mask controlling which phases trigger a pause.
    /// </summary>
    /// <param name="phases">The new phase mask.</param>
    public void SetPhases(BreakpointPhase phases)
    {
        if (_phases == phases)
        {
            return;
        }

        _phases = phases;
        RaiseChanged();
    }

    private bool HasMatch(string url)
    {
        var snapshot = _matchers;
        if (snapshot.Count == 0)
        {
            return false;
        }

        foreach (var matcher in snapshot)
        {
            if (matcher.HasMatch(url))
            {
                return true;
            }
        }

        return false;
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
