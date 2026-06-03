using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace Proxyfan.Presentation;

/// <summary>
///     An Avalonia attached property that executes a bound <see cref="ICommand" /> once
///     the target control raises its <c>Loaded</c> event.  Attach it to any
///     <see cref="Control" /> from AXAML to trigger an initial ViewModel refresh
///     without placing command-wiring logic in view code-behind.
/// </summary>
public static class ExecuteCommandOnLoadedBehavior
{
    /// <summary>
    ///     Identifies the <c>Command</c> attached property, whose value is the
    ///     <see cref="ICommand" /> to execute when the target control is loaded.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> CommandProperty;

    static ExecuteCommandOnLoadedBehavior()
    {
        CommandProperty = AvaloniaProperty.RegisterAttached<Control, ICommand?>("Command", typeof(ExecuteCommandOnLoadedBehavior));
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    /// <summary>
    ///     Gets the command assigned to <paramref name="element" />.
    /// </summary>
    /// <param name="element">The control to query.</param>
    /// <returns>The assigned <see cref="ICommand" />, or <see langword="null" />.</returns>
    public static ICommand? GetCommand(Control element)
    {
        return element.GetValue(CommandProperty);
    }

    /// <summary>
    ///     Sets the command on <paramref name="element" />, subscribing to its
    ///     <c>Loaded</c> event so the command is executed when the control loads.
    /// </summary>
    /// <param name="element">The control to configure.</param>
    /// <param name="value">The <see cref="ICommand" /> to execute on load, or <see langword="null" /> to detach.</param>
    public static void SetCommand(Control element, ICommand? value)
    {
        element.SetValue(CommandProperty, value);
    }

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs propertyChangedArgs)
    {
        if (propertyChangedArgs.OldValue is ICommand)
        {
            control.Loaded -= OnLoaded;
        }

        if (propertyChangedArgs.NewValue is ICommand)
        {
            control.Loaded += OnLoaded;
        }
    }

    private static void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (sender is not Control control)
        {
            return;
        }

        var command = control.GetValue(CommandProperty);

        if (command is not null && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
