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
///     traffic list. Brushes are resolved from the application's resource
///     dictionary so theme and accessibility variants can override them;
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
    ///     Converts a <see cref="TrafficFlowColorTag" /> into a corresponding
    ///     <see cref="IBrush" /> resolved from the application's theme-aware
    ///     resource dictionary.
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

        var resourceKey = TrafficFlowColorTagBrushKeys.GetResourceKey(colorTag);
        if (resourceKey is null)
        {
            return Brushes.Transparent;
        }

        var application = Application.Current;
        if (application is not null
            && application.TryGetResource(resourceKey, application.ActualThemeVariant, out var resource)
            && resource is IBrush themedBrush)
        {
            return themedBrush;
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
