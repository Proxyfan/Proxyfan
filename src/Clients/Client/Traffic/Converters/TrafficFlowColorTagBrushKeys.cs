using Proxyfan.Domain.Traffic;

namespace Proxyfan.Client.Traffic.Converters;

/// <summary>
///     Provides the application resource keys used to look up the themed
///     <see cref="Avalonia.Media.IBrush" /> for each <see cref="TrafficFlowColorTag" />
///     value. The actual brushes live in <c>App.axaml</c> under
///     <c>ResourceDictionary.ThemeDictionaries</c> so light, dark, and future
///     high-contrast theme variants can supply their own colours.
/// </summary>
public static class TrafficFlowColorTagBrushKeys
{
    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Blue" />.
    /// </summary>
    public const string Blue = "TrafficFlowColorTag.Blue.Brush";

    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Gray" />.
    /// </summary>
    public const string Gray = "TrafficFlowColorTag.Gray.Brush";

    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Green" />.
    /// </summary>
    public const string Green = "TrafficFlowColorTag.Green.Brush";

    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Orange" />.
    /// </summary>
    public const string Orange = "TrafficFlowColorTag.Orange.Brush";

    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Purple" />.
    /// </summary>
    public const string Purple = "TrafficFlowColorTag.Purple.Brush";

    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Red" />.
    /// </summary>
    public const string Red = "TrafficFlowColorTag.Red.Brush";

    /// <summary>
    ///     Resource key for <see cref="TrafficFlowColorTag.Yellow" />.
    /// </summary>
    public const string Yellow = "TrafficFlowColorTag.Yellow.Brush";

    /// <summary>
    ///     Returns the resource key for the given tag, or <see langword="null" />
    ///     for <see cref="TrafficFlowColorTag.None" /> or unknown values.
    /// </summary>
    /// <param name="colorTag">The color tag to look up.</param>
    /// <returns>The resource key, or <see langword="null" /> when no brush applies.</returns>
    public static string? GetResourceKey(TrafficFlowColorTag colorTag)
    {
        return colorTag switch
        {
            TrafficFlowColorTag.Red => Red,
            TrafficFlowColorTag.Orange => Orange,
            TrafficFlowColorTag.Yellow => Yellow,
            TrafficFlowColorTag.Green => Green,
            TrafficFlowColorTag.Blue => Blue,
            TrafficFlowColorTag.Purple => Purple,
            TrafficFlowColorTag.Gray => Gray,
            _ => null,
        };
    }
}
