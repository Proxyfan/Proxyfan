namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Result of a load-context creation attempt used internally by
///     <see cref="IsolatedPluginInstanceFactory" />. Wrapped in its own type so the
///     factory orchestration methods stay short and testable.
/// </summary>
public sealed class PluginContextResult
{
    /// <summary>
    ///     Gets the load context, when <see cref="IsSuccess" /> is <c>true</c>.
    /// </summary>
    public PluginLoadContext? Context { get; }

    /// <summary>
    ///     Gets the failure message, when <see cref="IsSuccess" /> is <c>false</c>.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginContextResult" />. Use
    ///     <see cref="PluginContextResults.Success" /> or
    ///     <see cref="PluginContextResults.Failure" /> for typical construction.
    /// </summary>
    /// <param name="context">The created context on success.</param>
    /// <param name="errorMessage">The failure message on failure.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    public PluginContextResult(PluginLoadContext? context, string? errorMessage, bool isSuccess)
    {
        Context = context;
        ErrorMessage = errorMessage;
        IsSuccess = isSuccess;
    }
}
