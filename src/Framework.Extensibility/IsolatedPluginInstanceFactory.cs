using System.IO;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     Default <see cref="IPluginInstanceFactory" /> that loads each plugin into its own
///     collectible <see cref="PluginLoadContext" /> for runtime isolation. All filesystem,
///     reflection, and instantiation failures are caught and surfaced as failure results
///     so that one bad plugin can never crash the host or block subsequent plugins. The
///     heavy lifting lives in <see cref="PluginInstanceCreator" />.
/// </summary>
public sealed class IsolatedPluginInstanceFactory : IPluginInstanceFactory
{
    /// <inheritdoc />
    public PluginInstantiationResult Create(PluginCandidate candidate)
    {
        if (candidate is null || !candidate.IsValid || candidate.Manifest is null)
        {
            var error = candidate?.ErrorMessage ?? "Invalid candidate.";
            return PluginInstantiationResults.Failure(error);
        }

        var manifest = candidate.Manifest;
        var assemblyPath = Path.Combine(candidate.DirectoryPath, manifest.AssemblyFileName);
        if (!File.Exists(assemblyPath))
        {
            return PluginInstantiationResults.Failure($"Assembly file '{manifest.AssemblyFileName}' not found in '{candidate.DirectoryPath}'.");
        }

        var contextResult = PluginInstanceCreator.CreateContext(assemblyPath);
        if (!contextResult.IsSuccess || contextResult.Context is null)
        {
            return PluginInstantiationResults.Failure(contextResult.ErrorMessage ?? "Failed to create load context.");
        }

        return PluginInstanceCreator.InstantiateFromContext(contextResult.Context, assemblyPath, manifest);
    }
}
