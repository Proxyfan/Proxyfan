namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Outcome of scanning a single plugin sub-directory for a manifest. Either
///     <see cref="Manifest" /> is non-null (valid candidate) or <see cref="ErrorMessage" />
///     describes why the directory was rejected.
/// </summary>
public sealed class PluginCandidate
{
    /// <summary>
    ///     Gets the absolute path of the plugin's directory.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    ///     Gets a non-null error message when <see cref="IsValid" /> is false (no manifest,
    ///     parse failure, etc.).
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets a value indicating whether the candidate is valid (manifest parsed cleanly).
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    ///     Gets the parsed manifest when <see cref="IsValid" /> is true, otherwise null.
    /// </summary>
    public PluginManifest? Manifest { get; }

    /// <summary>
    ///     Initializes a new <see cref="PluginCandidate" />. Use <see cref="PluginCandidates.Valid" />
    ///     or <see cref="PluginCandidates.Invalid" /> for typical construction.
    /// </summary>
    /// <param name="directoryPath">The absolute path of the candidate directory.</param>
    /// <param name="manifest">The parsed manifest when valid, otherwise null.</param>
    /// <param name="errorMessage">A human-readable description when invalid, otherwise null.</param>
    /// <param name="isValid">Whether the candidate is valid.</param>
    public PluginCandidate(string directoryPath, PluginManifest? manifest, string? errorMessage, bool isValid)
    {
        DirectoryPath = directoryPath;
        Manifest = manifest;
        ErrorMessage = errorMessage;
        IsValid = isValid;
    }
}
