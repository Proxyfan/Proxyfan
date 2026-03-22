using System;

namespace Proxyfan.DependencyInjection;

/// <summary>Delegate that determines whether a <see cref="Type" /> should be registered with the DI container.</summary>
/// <param name="type">The type to evaluate.</param>
public delegate bool ServiceTypeFilter(Type type);