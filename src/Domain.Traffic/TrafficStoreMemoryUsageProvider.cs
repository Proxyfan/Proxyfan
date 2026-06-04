namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Provides the current process memory usage as a ratio in the inclusive range [0, 1],
///     where 1 means the process has reached its available memory budget.
/// </summary>
/// <returns>The current memory usage ratio.</returns>
public delegate double TrafficStoreMemoryUsageProvider();
