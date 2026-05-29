namespace Proxyfan.Domain.Updates;

/// <summary>
///     Callback raised by <see cref="MutableUpdateNotification" /> when the most recently
///     observed available update changes (including being cleared back to <see langword="null" />).
/// </summary>
/// <param name="update">The new available update, or <see langword="null" /> when cleared.</param>
public delegate void UpdateNotificationChanged(UpdateInfo? update);
