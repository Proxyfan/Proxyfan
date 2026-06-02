using Avalonia;
using Proxyfan.Domain.Traffic;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Proxyfan.Client.Traffic.Converters;

/// <summary>
///     Conventions for the <see cref="TrafficFlowColorTag" /> brush resources
///     registered in <c>App.axaml</c>. Brushes are looked up through
///     <see cref="Application.Resources" /> under
///     <c>TrafficFlowColorTag.&lt;Tag&gt;.Brush</c> so theme variants
///     (light, dark, high-contrast) can override the color palette.
/// </summary>
public static class TrafficFlowColorTagBrushResources
{
    /// <summary>
    ///     Resource-key prefix used to look up the per-tag brush.
    /// </summary>
    public const string ResourceKeyPrefix = "TrafficFlowColorTag.";

    /// <summary>
    ///     Resource-key suffix used to look up the per-tag brush.
    /// </summary>
    public const string ResourceKeySuffix = ".Brush";

    private static readonly FrozenDictionary<TrafficFlowColorTag, string> ResourceKeys = BuildResourceKeys();

    /// <summary>
    ///     Returns the application-resource key for the supplied color tag.
    ///     Keys are precomputed so the converter's hot path avoids per-call
    ///     string allocations.
    /// </summary>
    /// <param name="colorTag">The color tag to resolve.</param>
    /// <returns>The resource key registered in <c>App.axaml</c>.</returns>
    public static string BuildResourceKey(TrafficFlowColorTag colorTag)
    {
        if (ResourceKeys.TryGetValue(colorTag, out var cached))
        {
            return cached;
        }

        return string.Concat(ResourceKeyPrefix, colorTag.ToString(), ResourceKeySuffix);
    }

    private static FrozenDictionary<TrafficFlowColorTag, string> BuildResourceKeys()
    {
        var map = new Dictionary<TrafficFlowColorTag, string>();
        foreach (var value in System.Enum.GetValues<TrafficFlowColorTag>())
        {
            map[value] = string.Concat(ResourceKeyPrefix, value.ToString(), ResourceKeySuffix);
        }

        return map.ToFrozenDictionary();
    }
}
