using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Certificates;
using Proxyfan.Presentation;
using Proxyfan.Presentation.Files;
using Proxyfan.Presentation.Threading;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Certificate Manager tool window. Displays metadata about the
///     current root certificate authority and exposes commands to install/uninstall it
///     in the Windows trust store, regenerate the authority, and export it as a
///     DER-encoded <c>.cer</c> file.
/// </summary>
public sealed partial class CertificateManagerViewModel : ObservableObject, IActivatable, IDisposable
{
    private const string CerExtension = "cer";
    private const string CerTypeDescription = "X.509 Certificate (DER)";
    private const string DefaultCertificateFileName = "proxyfan-root-ca.cer";
    private readonly MutableCertificateAuthorityProvider _authorityProvider;
    private readonly ICertificateStore _certificateStore;
    private readonly IFilePickerService _filePickerService;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    private bool _isActivated;
    [ObservableProperty]
    private bool _isBusy;
    [ObservableProperty]
    private bool _isInstalled;
    [ObservableProperty]
    private string _issuer;
    [ObservableProperty]
    private DateTimeOffset _notAfter;
    [ObservableProperty]
    private DateTimeOffset _notBefore;
    [ObservableProperty]
    private string _statusMessage;
    [ObservableProperty]
    private string _subject;
    [ObservableProperty]
    private string _thumbprint;

    /// <summary>
    ///     Initializes a new <see cref="CertificateManagerViewModel" />.
    /// </summary>
    /// <param name="authorityProvider">The provider that owns the current root authority.</param>
    /// <param name="certificateStore">The store used to install or uninstall the authority.</param>
    /// <param name="filePickerService">The file picker used to choose an export destination.</param>
    /// <param name="userInterfaceScheduler">The scheduler used to marshal property updates onto the UI thread.</param>
    public CertificateManagerViewModel(
        MutableCertificateAuthorityProvider authorityProvider,
        ICertificateStore certificateStore,
        IFilePickerService filePickerService,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _authorityProvider = authorityProvider;
        _certificateStore = certificateStore;
        _filePickerService = filePickerService;
        _userInterfaceScheduler = userInterfaceScheduler;
        _subject = string.Empty;
        _issuer = string.Empty;
        _thumbprint = string.Empty;
        _statusMessage = string.Empty;
        _notBefore = DateTimeOffset.MinValue;
        _notAfter = DateTimeOffset.MinValue;
    }

    /// <inheritdoc />
    public void Activate()
    {
        if (_isActivated)
        {
            return;
        }

        _isActivated = true;
        RefreshCommand.Execute(null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void ApplyAuthority(CertificateAuthority authority, bool installed)
    {
        var certificate = authority.Certificate;
        var subject = certificate.Subject;
        var issuer = certificate.Issuer;
        var thumbprint = certificate.Thumbprint;
        var notBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero);
        var notAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero);
        _userInterfaceScheduler.Post(() =>
        {
            Subject = subject;
            Issuer = issuer;
            Thumbprint = thumbprint;
            NotBefore = notBefore;
            NotAfter = notAfter;
            IsInstalled = installed;
        });
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var authority = await _authorityProvider.GetAsync(cancellationToken).ConfigureAwait(false);
            var request = new FilePickerSaveRequest
            {
                Title = "Export Proxyfan Root CA",
                DefaultFileName = DefaultCertificateFileName,
                FileExtension = CerExtension,
                ExtensionDescription = CerTypeDescription,
            };
            var stream = await _filePickerService.OpenForWriteAsync(request, cancellationToken).ConfigureAwait(false);
            if (stream is null)
            {
                StatusMessage = "Export cancelled.";
                return;
            }

            try
            {
                var bytes = authority.Certificate.Export(X509ContentType.Cert);
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                StatusMessage = "Certificate exported.";
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var authority = await _authorityProvider.GetAsync(cancellationToken).ConfigureAwait(false);
            await _certificateStore.InstallAsync(authority, cancellationToken).ConfigureAwait(false);
            IsInstalled = true;
            StatusMessage = "Certificate installed in the Windows trust store.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var authority = await _authorityProvider.GetAsync(cancellationToken).ConfigureAwait(false);
        var installed = await _certificateStore.IsInstalledAsync(authority, cancellationToken).ConfigureAwait(false);
        ApplyAuthority(authority, installed);
    }

    [RelayCommand]
    private async Task RegenerateAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var wasInstalled = IsInstalled;
            var authority = await _authorityProvider.RegenerateAsync(cancellationToken).ConfigureAwait(false);
            if (wasInstalled)
            {
                await _certificateStore.InstallAsync(authority, cancellationToken).ConfigureAwait(false);
            }

            var installed = await _certificateStore.IsInstalledAsync(authority, cancellationToken).ConfigureAwait(false);
            ApplyAuthority(authority, installed);
            StatusMessage = "Certificate regenerated.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UninstallAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var authority = await _authorityProvider.GetAsync(cancellationToken).ConfigureAwait(false);
            await _certificateStore.UninstallAsync(authority, cancellationToken).ConfigureAwait(false);
            IsInstalled = false;
            StatusMessage = "Certificate removed from the Windows trust store.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
