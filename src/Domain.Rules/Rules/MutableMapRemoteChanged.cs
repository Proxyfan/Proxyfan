namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Notifies subscribers that a <see cref="MutableMapRemoteRule" />'s configuration
///     (entries collection or enabled state) has changed.
/// </summary>
public delegate void MutableMapRemoteChanged();
