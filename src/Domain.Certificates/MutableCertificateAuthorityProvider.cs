using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Owns the live root certificate authority used for transport-layer-security
///     interception. The authority is generated lazily on first request and can be
///     rotated at runtime via <see cref="RegenerateAsync" />; consumers may listen
///     to <see cref="Changed" /> to refresh caches that depend on the previous
///     authority.
/// </summary>
public sealed class MutableCertificateAuthorityProvider
{
    /// <summary>
    ///     Occurs when the held authority has been rotated to a new instance.
    /// </summary>
    public event MutableCertificateAuthorityProviderChanged? Changed;

    private readonly ICertificateGenerator _generator;
    private readonly Lock _syncRoot;
    private Task<CertificateAuthority>? _currentTask;

    /// <summary>
    ///     Initializes a new <see cref="MutableCertificateAuthorityProvider" />.
    /// </summary>
    /// <param name="generator">The generator used to mint root certificate authorities.</param>
    public MutableCertificateAuthorityProvider(ICertificateGenerator generator)
    {
        var syncRoot = new Lock();
        _generator = generator;
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Gets the current authority, generating it lazily on first call.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels generation when the authority must be created.</param>
    /// <returns>The current certificate authority.</returns>
    public Task<CertificateAuthority> GetAsync(CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            if (_currentTask is null || (_currentTask.IsCompleted && !_currentTask.IsCompletedSuccessfully))
            {
                _currentTask = _generator.GenerateRootCertificateAuthorityAsync(cancellationToken);
            }

            return _currentTask;
        }
    }

    /// <summary>
    ///     Generates a fresh root certificate authority and replaces the current one,
    ///     then raises <see cref="Changed" /> so listeners can clear any leaf caches.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels generation.</param>
    /// <returns>The newly minted authority.</returns>
    public async Task<CertificateAuthority> RegenerateAsync(CancellationToken cancellationToken)
    {
        var authority = await _generator.GenerateRootCertificateAuthorityAsync(cancellationToken).ConfigureAwait(false);
        lock (_syncRoot)
        {
            _currentTask = Task.FromResult(authority);
        }

        RaiseChanged();
        return authority;
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this);
    }
}
