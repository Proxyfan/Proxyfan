namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Notification raised by <see cref="MutableScriptingConfiguration" /> whenever the
///     enabled flag, source code, or compiled script reference changes.
/// </summary>
public delegate void MutableScriptingConfigurationChanged();
