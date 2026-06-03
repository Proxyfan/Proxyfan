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
///     traffic list. Brushes are supplied via styled properties bound from XAML
///     (typically with <c>{DynamicResource …}</c> against
///     <c>App.axaml</c> theme dictionaries) so theming stays purely declarative
///     and the converter never touches global application state from C#.
///     <see cref="TrafficFlowColorTag.None" /> and unknown inputs yield a
///     transparent brush so unmarked rows remain visually quiet.
/// </summary>
public sealed class TrafficFlowColorTagToBrushConverter : AvaloniaObject, IValueConverter
{
    /// <summary>
    ///     Styled property backing <see cref="BlueBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BlueBrushProperty;

    /// <summary>
    ///     Styled property backing <see cref="GrayBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> GrayBrushProperty;

    /// <summary>
    ///     Styled property backing <see cref="GreenBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> GreenBrushProperty;

    /// <summary>
    ///     Styled property backing <see cref="OrangeBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> OrangeBrushProperty;

    /// <summary>
    ///     Styled property backing <see cref="PurpleBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PurpleBrushProperty;

    /// <summary>
    ///     Styled property backing <see cref="RedBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> RedBrushProperty;

    /// <summary>
    ///     Styled property backing <see cref="YellowBrush" />.
    /// </summary>
    public static readonly StyledProperty<IBrush?> YellowBrushProperty;

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Blue" />.
    /// </summary>
    public IBrush? BlueBrush
    {
        get => GetValue(BlueBrushProperty);
        set => SetValue(BlueBrushProperty, value);
    }

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Gray" />.
    /// </summary>
    public IBrush? GrayBrush
    {
        get => GetValue(GrayBrushProperty);
        set => SetValue(GrayBrushProperty, value);
    }

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Green" />.
    /// </summary>
    public IBrush? GreenBrush
    {
        get => GetValue(GreenBrushProperty);
        set => SetValue(GreenBrushProperty, value);
    }

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Orange" />.
    /// </summary>
    public IBrush? OrangeBrush
    {
        get => GetValue(OrangeBrushProperty);
        set => SetValue(OrangeBrushProperty, value);
    }

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Purple" />.
    /// </summary>
    public IBrush? PurpleBrush
    {
        get => GetValue(PurpleBrushProperty);
        set => SetValue(PurpleBrushProperty, value);
    }

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Red" />.
    /// </summary>
    public IBrush? RedBrush
    {
        get => GetValue(RedBrushProperty);
        set => SetValue(RedBrushProperty, value);
    }

    /// <summary>
    ///     Brush used for <see cref="TrafficFlowColorTag.Yellow" />.
    /// </summary>
    public IBrush? YellowBrush
    {
        get => GetValue(YellowBrushProperty);
        set => SetValue(YellowBrushProperty, value);
    }

    static TrafficFlowColorTagToBrushConverter()
    {
        BlueBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(BlueBrush));
        GrayBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(GrayBrush));
        GreenBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(GreenBrush));
        OrangeBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(OrangeBrush));
        PurpleBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(PurpleBrush));
        RedBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(RedBrush));
        YellowBrushProperty = AvaloniaProperty.Register<TrafficFlowColorTagToBrushConverter, IBrush?>(nameof(YellowBrush));
    }

    /// <summary>
    ///     Converts a <see cref="TrafficFlowColorTag" /> into the brush bound to
    ///     the corresponding styled property, falling back to
    ///     <see cref="Brushes.Transparent" /> when the tag is
    ///     <see cref="TrafficFlowColorTag.None" />, unrecognised, or unbound.
    /// </summary>
    /// <param name="value">The bound value, expected to be a <see cref="TrafficFlowColorTag" />.</param>
    /// <param name="targetType">Ignored.</param>
    /// <param name="parameter">Ignored.</param>
    /// <param name="culture">Ignored.</param>
    /// <returns>A brush representing the color, or transparent when not applicable.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TrafficFlowColorTag colorTag)
        {
            return Brushes.Transparent;
        }

        var brush = colorTag switch
        {
            TrafficFlowColorTag.Red => RedBrush,
            TrafficFlowColorTag.Orange => OrangeBrush,
            TrafficFlowColorTag.Yellow => YellowBrush,
            TrafficFlowColorTag.Green => GreenBrush,
            TrafficFlowColorTag.Blue => BlueBrush,
            TrafficFlowColorTag.Purple => PurpleBrush,
            TrafficFlowColorTag.Gray => GrayBrush,
            _ => null,
        };

        return brush ?? Brushes.Transparent;
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
