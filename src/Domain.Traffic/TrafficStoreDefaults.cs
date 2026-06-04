using System;
using System.IO;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Provides default memory-pressure and spill-directory behavior for <see cref="TrafficStore" />.
/// </summary>
public static class TrafficStoreDefaults
{
    /// <summary>
    ///     Computes process memory usage ratio based on GC memory availability.
    /// </summary>
    /// <returns>A ratio in the range [0, 1] when available, otherwise 0.</returns>
    public static double GetMemoryUsageRatio()
    {
        var memoryInfo = GC.GetGCMemoryInfo();
        if (memoryInfo.TotalAvailableMemoryBytes <= 0)
        {
            return 0D;
        }

        var allocatedBytes = GC.GetTotalMemory(false);
        var ratio = allocatedBytes / (double)memoryInfo.TotalAvailableMemoryBytes;
        return ratio;
    }

    /// <summary>
    ///     Gets the default spill directory under the current profile's local app-data root.
    /// </summary>
    /// <returns>The full spill directory path.</returns>
    public static string GetSpillDirectoryPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            return Path.Combine(Path.GetTempPath(), "Proxyfan", "spill");
        }

        return Path.Combine(localAppData, "Proxyfan", "spill");
    }
}
