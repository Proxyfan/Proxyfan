namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Notifies subscribers that the current certificate authority held by a
///     <see cref="MutableCertificateAuthorityProvider" /> has been rotated.
/// </summary>
/// <param name="sender">The provider whose authority was rotated.</param>
public delegate void MutableCertificateAuthorityProviderChanged(MutableCertificateAuthorityProvider sender);
