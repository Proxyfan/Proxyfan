using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Proxyfan.Presentation;

/// <summary>
///     An Avalonia attached property that resolves a ViewModel from the DI container and sets it
///     as the <c>DataContext</c> of the target control.
/// </summary>
public static class ViewModelLocator
{
    /// <summary>
    ///     Identifies the <c>DataContext</c> attached property, whose value is the
    ///     <see cref="Type" /> of the ViewModel to resolve from the DI container.
    /// </summary>
    public static readonly AttachedProperty<Type?> DataContextProperty;

    static ViewModelLocator()
    {
        DataContextProperty = AvaloniaProperty.RegisterAttached<Control, Type?>("DataContext", typeof(ViewModelLocator));
        DataContextProperty.Changed.AddClassHandler<Control>(OnDataContextChanged);
    }

    /// <summary>
    ///     Gets the ViewModel type assigned to <paramref name="element" />.
    /// </summary>
    /// <param name="element">The control to query.</param>
    /// <returns>
    ///     The assigned <see cref="Type" />, or <see langword="null" />.
    /// </returns>
    public static Type? GetDataContext(Control element)
    {
        return element.GetValue(DataContextProperty);
    }

    /// <summary>
    ///     Sets the ViewModel type on <paramref name="element" />, causing the DI container to be queried.
    /// </summary>
    /// <param name="element">The control to configure.</param>
    /// <param name="value">The <see cref="Type" /> of the ViewModel to resolve.</param>
    public static void SetDataContext(Control element, Type? value)
    {
        element.SetValue(DataContextProperty, value);
    }

    private static void OnDataContextChanged(Control control, AvaloniaPropertyChangedEventArgs propertyChangedArgs)
    {
        if (propertyChangedArgs.NewValue is not Type newValueType)
        {
            return;
        }

        if (ContainerLocator.Current is null)
        {
            return;
        }

        control.DataContext = ContainerLocator.Current.GetRequiredService(newValueType);
    }
}