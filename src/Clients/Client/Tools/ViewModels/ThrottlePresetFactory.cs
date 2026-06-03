using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Factory delegate that materialises a throttle preset profile, or returns
///     <see langword="null" /> for the "disabled" entry.
/// </summary>
/// <returns>The throttle profile, or <see langword="null" /> when the preset disables throttling.</returns>
public delegate ThrottleProfile? ThrottlePresetFactory();
