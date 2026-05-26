using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Provides access to file contents used by <see cref="MapLocalRule" /> to serve responses
///     from disk. Abstracted to allow testing without touching the file system.
/// </summary>
public interface IMapLocalFileProvider
{
    /// <summary>
    ///     Reads the bytes at the supplied path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="cancellationToken">A token that cancels the read operation.</param>
    /// <returns>The file content, or <see langword="null" /> when the file does not exist.</returns>
    Task<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken);
}
