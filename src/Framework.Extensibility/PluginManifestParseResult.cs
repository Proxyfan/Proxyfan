namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Outcome of <see cref="PluginManifestReader.Parse" />. Either <see cref="Manifest" />
///     is non-null (success) or <see cref="ErrorMessage" /> describes why parsing failed.
/// </summary>
public sealed class PluginManifestParseResult
{
    /// <summary>
    ///     Gets a non-null error message when <see cref="IsSuccess" /> is false.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the parsed manifest when <see cref="IsSuccess" /> is true, otherwise null.
    /// </summary>
    public PluginManifest? Manifest { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginManifestParseResult" />. Use
    ///     <see cref="PluginManifestParseResults.Success" /> or
    ///     <see cref="PluginManifestParseResults.Failure" /> for typical construction.
    /// </summary>
    /// <param name="manifest">The parsed manifest on success.</param>
    /// <param name="errorMessage">The error message on failure.</param>
    /// <param name="isSuccess">Whether the parse succeeded.</param>
    public PluginManifestParseResult(PluginManifest? manifest, string? errorMessage, bool isSuccess)
    {
        Manifest = manifest;
        ErrorMessage = errorMessage;
        IsSuccess = isSuccess;
    }
}
