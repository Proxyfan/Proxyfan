using System;

namespace Proxyfan.Presentation;

/// <summary>
///     Delegate that returns the application's <see cref="IServiceProvider" /> for use in lazy container
///     initialization.
/// </summary>
public delegate IServiceProvider ServiceLocatorFactory();