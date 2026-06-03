using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Proxyfan.Presentation;

/// <summary>
///     An Avalonia attached property that calls <see cref="IActivatable.Activate" /> on the
///     control's <see cref="Avalonia.StyledElement.DataContext" /> the first time the control
///     raises its <c>Loaded</c> event. Attach from AXAML instead of wiring
///     the event in code-behind.
/// </summary>
public static class ActivateOnLoadedBehavior
{
    /// <summary>
    ///     Identifies the <c>IsEnabled</c> attached property. Set to <c>True</c> in AXAML to
    ///     enable activation for the host control.
    /// </summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty;

    static ActivateOnLoadedBehavior()
    {
        IsEnabledProperty = AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(ActivateOnLoadedBehavior));
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    /// <summary>
    ///     Sets the <c>IsEnabled</c> value on <paramref name="control" />.
    /// </summary>
    /// <param name="control">The control to configure.</param>
    /// <param name="value">
    ///     <see langword="true" /> to hook the <c>Loaded</c> event and call
    ///     <see cref="IActivatable.Activate" /> on the DataContext; otherwise <see langword="false" />.
    /// </param>
    public static void SetIsEnabled(Control control, bool value)
    {
        control.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is true)
        {
            control.Loaded += OnLoaded;
        }
        else
        {
            control.Loaded -= OnLoaded;
        }
    }

    private static void OnLoaded(object? sender, RoutedEventArgs args)
    {
        if (sender is Control { DataContext: IActivatable activatable } control)
        {
            control.Loaded -= OnLoaded;
            activatable.Activate();
        }
    }
}
