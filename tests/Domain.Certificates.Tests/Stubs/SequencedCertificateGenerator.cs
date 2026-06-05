using Proxyfan.Domain.Certificates;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests.Stubs;

/// <summary>
///     A stub <see cref="ICertificateGenerator" /> that returns a configured sequence of root
///     generation results so tests can exercise retry behavior after failures.
/// </summary>
internal sealed class SequencedCertificateGenerator : ICertificateGenerator
{
    private readonly Queue<Func<CancellationToken, Task<CertificateAuthority>>> _rootGenerationSequence;

    public SequencedCertificateGenerator(params Func<CancellationToken, Task<CertificateAuthority>>[] rootGenerationSequence)
    {
        _rootGenerationSequence = new Queue<Func<CancellationToken, Task<CertificateAuthority>>>(rootGenerationSequence);
    }

    public int RootGenerationCount { get; private set; }

    /// <inheritdoc />
    public Task<X509Certificate2> GenerateLeafCertificateAsync(
        string hostname,
        CertificateAuthority authority,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(authority.Sign(hostname));
    }

    /// <inheritdoc />
    public Task<CertificateAuthority> GenerateRootCertificateAuthorityAsync(CancellationToken cancellationToken)
    {
        RootGenerationCount++;
        return _rootGenerationSequence.Dequeue().Invoke(cancellationToken);
    }
}
