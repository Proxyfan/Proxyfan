using System;
using System.ComponentModel;

namespace Proxyfan.Presentation;

/// <summary>
///     Provides access to the application's DI container. Intended for use in XAML bindings only —
///     do not use from application code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContainerLocator
{
    private static Lazy<IServiceProvider>? _lazyContainer;

    /// <summary>
    ///     Gets the current <see cref="IServiceProvider" />, or <see langword="null" /> if not yet initialized.
    /// </summary>
    public static IServiceProvider? Current
    {
        get => field ??= _lazyContainer?.Value;
        private set;
    }

    /// <summary>
    ///     Resets the container to its uninitialized state. For use in tests only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Reset()
    {
        Current = null;
        _lazyContainer = null;
    }

    /// <summary>
    ///     Registers a factory that will be used to lazily resolve the <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="factory">A delegate that returns the application's <see cref="IServiceProvider" />.</param>
    public static void Set(ServiceLocatorFactory factory)
    {
        var lazyContainer = new Lazy<IServiceProvider>(factory.Invoke);
        _lazyContainer = lazyContainer;
    }
}