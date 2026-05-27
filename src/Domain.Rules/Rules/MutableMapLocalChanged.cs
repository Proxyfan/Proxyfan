namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Notifies subscribers that a <see cref="MutableMapLocalRule" />'s configuration
///     (entries collection or enabled state) has changed.
/// </summary>
public delegate void MutableMapLocalChanged();
