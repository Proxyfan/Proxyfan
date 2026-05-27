namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Records a single invocation of <see cref="StubUserScriptCompiler.Compile" />
///     for verification in tests.
/// </summary>
public sealed record StubUserScriptCompilerInvocation
{
    /// <summary>
    ///     Initializes a new <see cref="StubUserScriptCompilerInvocation" />.
    /// </summary>
    /// <param name="displayName">The display name supplied by the caller.</param>
    /// <param name="requestScript">The request-phase script source supplied by the caller.</param>
    /// <param name="responseScript">The response-phase script source supplied by the caller.</param>
    public StubUserScriptCompilerInvocation(string displayName, string requestScript, string responseScript)
    {
        DisplayName = displayName;
        RequestScript = requestScript;
        ResponseScript = responseScript;
    }

    /// <summary>
    ///     Gets the display name supplied by the caller.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Gets the request-phase script source supplied by the caller.
    /// </summary>
    public string RequestScript { get; }

    /// <summary>
    ///     Gets the response-phase script source supplied by the caller.
    /// </summary>
    public string ResponseScript { get; }
}
