using System;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Options that govern how often <see cref="PeriodicUpdateChecker" /> polls the update
///     feed and which version it considers "current" when comparing release information.
/// </summary>
public sealed class PeriodicUpdateCheckOptions
{
    /// <summary>
    ///     Gets the version string used as the baseline when comparing the latest release
    ///     reported by the feed. Updates strictly newer than this value are published.
    /// </summary>
    public required string CurrentVersion { get; init; }

    /// <summary>
    ///     Gets the delay applied before the first poll runs. Use a short value to avoid
    ///     blocking start-up; tests typically use <see cref="TimeSpan.Zero" /> for determinism.
    /// </summary>
    public required TimeSpan InitialDelay { get; init; }

    /// <summary>
    ///     Gets the interval between successive polls after the first poll completes.
    ///     Must be greater than <see cref="TimeSpan.Zero" />.
    /// </summary>
    public required TimeSpan PollInterval { get; init; }
}
