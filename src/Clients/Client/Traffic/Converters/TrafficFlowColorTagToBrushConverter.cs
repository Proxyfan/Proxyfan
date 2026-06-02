using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;

namespace Proxyfan.Client.Traffic.Converters;

/// <summary>
///     Avalonia value converter that maps a <see cref="TrafficFlowColorTag" /> to
///     an <see cref="IBrush" /> suitable for rendering a small color dot in the
///     traffic list. Brushes are resolved from the current
///     <see cref="Application" />'s theme resources so they automatically adapt
///     to light, dark, and high-contrast palettes.
///     <see cref="TrafficFlowColorTag.None" /> yields a transparent brush so
///     unmarked rows remain visually quiet.
/// </summary>
public sealed class TrafficFlowColorTagToBrushConverter : IValueConverter
{
    /// <summary>
    ///     Gets the shared singleton instance for XAML usage.
    /// </summary>
    public static TrafficFlowColorTagToBrushConverter Instance { get; }

    static TrafficFlowColorTagToBrushConverter()
    {
        var instance = new TrafficFlowColorTagToBrushConverter();
        Instance = instance;
    }

    /// <summary>
    ///     Converts a <see cref="TrafficFlowColorTag" /> into the
    ///     theme-resolved brush registered in <see cref="Application.Resources" />.
    /// </summary>
    /// <param name="value">The bound value, expected to be a <see cref="TrafficFlowColorTag" />.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Ignored.</param>
    /// <param name="culture">Ignored.</param>
    /// <returns>A brush representing the color, or transparent when not applicable.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TrafficFlowColorTag colorTag || colorTag == TrafficFlowColorTag.None)
        {
            return Brushes.Transparent;
        }

        var key = TrafficFlowColorTagBrushResources.BuildResourceKey(colorTag);
        var application = Application.Current;
        if (application is not null
            && application.Resources.TryGetResource(key, application.ActualThemeVariant, out var resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Transparent;
    }

    /// <summary>
    ///     Not implemented. This converter is one-way.
    /// </summary>
    /// <param name="value">Ignored.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Ignored.</param>
    /// <param name="culture">Ignored.</param>
    /// <returns>Always throws <see cref="NotSupportedException" />.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("TrafficFlowColorTagToBrushConverter is one-way only.");
    }
}
