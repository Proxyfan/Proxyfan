namespace Proxyfan.Framework.Platform;

/// <summary>
///     Delegate used by <see cref="WindowsSystemProxy" /> to notify the operating system
///     and already-running WinINet-based clients (Internet Explorer, .NET HttpClient with
///     the system proxy, Edge, Office, etc.) that the per-user proxy registry values have
///     changed and that cached settings must be refreshed. Implementations must throw on
///     failure so that callers can surface the error to the user; silent failure would
///     reproduce the original bug where the registry update succeeds but running clients
///     keep using the previous proxy configuration.
/// </summary>
public delegate void InternetSettingsRefreshDelegate();
