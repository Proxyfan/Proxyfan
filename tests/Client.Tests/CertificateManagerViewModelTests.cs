using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Certificates;
using Proxyfan.Presentation.Threading;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="CertificateManagerViewModel" />.
/// </summary>
public sealed class CertificateManagerViewModelTests
{
    /// <summary>
    ///     Activate performs the initial refresh once and no-ops on subsequent calls.
    /// </summary>
    [Test]
    public async Task Activate_CalledTwice_RefreshesOnlyOnce()
    {
        var (viewModel, _, _, store) = Create();

        viewModel.Activate();
        if (viewModel.RefreshCommand.ExecutionTask is { } activationRefresh)
        {
            await activationRefresh.ConfigureAwait(false);
        }

        viewModel.Activate();
        if (viewModel.RefreshCommand.ExecutionTask is { } repeatedActivationRefresh)
        {
            await repeatedActivationRefresh.ConfigureAwait(false);
        }

        await Assert.That(store.IsInstalledCallCount).IsEqualTo(1);
    }

    /// <summary>
    ///     The RefreshCommand loads metadata from the current authority and reports the trust-store state.
    /// </summary>
    [Test]
    public async Task RefreshCommand_FreshAuthority_PopulatesMetadataAndIsInstalled()
    {
        var (viewModel, _, _, store) = Create();

        await viewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(viewModel.Subject).IsEqualTo("CN=Proxyfan Client Test CA");
        await Assert.That(viewModel.Issuer).IsEqualTo("CN=Proxyfan Client Test CA");
        await Assert.That(viewModel.Thumbprint.Length).IsEqualTo(40);
        await Assert.That(viewModel.IsInstalled).IsFalse();
        await Assert.That(store.IsInstalledCallCount).IsEqualTo(1);
    }

    /// <summary>
    ///     The InstallCommand delegates to the certificate store and sets <see cref="CertificateManagerViewModel.IsInstalled" />.
    /// </summary>
    [Test]
    public async Task InstallCommand_FreshAuthority_InstallsAndUpdatesState()
    {
        var (viewModel, _, _, store) = Create();

        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(store.InstallCallCount).IsEqualTo(1);
        await Assert.That(viewModel.IsInstalled).IsTrue();
        await Assert.That(viewModel.StatusMessage).Contains("installed");
    }

    /// <summary>
    ///     The UninstallCommand delegates to the certificate store and clears <see cref="CertificateManagerViewModel.IsInstalled" />.
    /// </summary>
    [Test]
    public async Task UninstallCommand_PreviouslyInstalled_RemovesAndUpdatesState()
    {
        var (viewModel, _, _, store) = Create();
        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);

        await viewModel.UninstallCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(store.UninstallCallCount).IsEqualTo(1);
        await Assert.That(viewModel.IsInstalled).IsFalse();
        await Assert.That(viewModel.StatusMessage).Contains("removed");
    }

    /// <summary>
    ///     The ExportCommand writes a DER-encoded certificate to the picker stream.
    /// </summary>
    [Test]
    public async Task ExportCommand_PickerProvidesStream_WritesDerBytes()
    {
        var captureStream = new MemoryStream();
        var stream = new NonDisposingStreamWrapper(captureStream);
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = stream };
        var (viewModel, _, _, _) = Create(picker);

        await viewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(picker.OpenForWriteCallCount).IsEqualTo(1);
        await Assert.That(captureStream.Length > 0).IsTrue();
        await Assert.That(viewModel.StatusMessage).Contains("exported");
    }

    /// <summary>
    ///     The ExportCommand reports a cancelled export when the file picker returns no stream.
    /// </summary>
    [Test]
    public async Task ExportCommand_PickerCancelled_ReportsCancellation()
    {
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = null };
        var (viewModel, _, _, _) = Create(picker);

        await viewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(picker.OpenForWriteCallCount).IsEqualTo(1);
        await Assert.That(viewModel.StatusMessage).Contains("cancelled");
    }

    /// <summary>
    ///     ExportCommand defers post-await property updates until scheduled UI work runs.
    /// </summary>
    [Test]
    public async Task ExportCommand_PickerCancelled_DefersBoundPropertyUpdatesUntilUiWorkRuns()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = null };
        var (viewModel, _, _, _) = Create(picker, scheduler);

        await viewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(viewModel.IsBusy).IsTrue();
        await Assert.That(viewModel.StatusMessage).IsEqualTo(string.Empty);

        scheduler.DrainQueue();

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.StatusMessage).Contains("cancelled");
    }

    /// <summary>
    ///     The RegenerateCommand replaces the authority and reapplies metadata.
    /// </summary>
    [Test]
    public async Task RegenerateCommand_FreshProvider_RotatesAuthority()
    {
        var (viewModel, generator, _, _) = Create();
        await viewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
        var originalThumbprint = viewModel.Thumbprint;

        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(viewModel.Thumbprint).IsNotEqualTo(originalThumbprint);
        await Assert.That(generator.RootGenerationCount).IsEqualTo(2);
        await Assert.That(viewModel.StatusMessage).Contains("regenerated");
    }

    /// <summary>
    ///     When the previous authority was installed, RegenerateCommand reinstalls the new one.
    /// </summary>
    [Test]
    public async Task RegenerateCommand_PreviouslyInstalled_ReinstallsRotatedAuthority()
    {
        var (viewModel, _, provider, store) = Create();
        var originalAuthority = await provider.GetAsync(default).ConfigureAwait(false);
        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);

        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(false);

        var rotatedAuthority = await provider.GetAsync(default).ConfigureAwait(false);
        await Assert.That(store.InstallCallCount).IsEqualTo(2);
        await Assert.That(store.UninstallCallCount).IsEqualTo(1);
        await Assert.That(await store.IsInstalledAsync(originalAuthority, default).ConfigureAwait(false)).IsFalse();
        await Assert.That(await store.IsInstalledAsync(rotatedAuthority, default).ConfigureAwait(false)).IsTrue();
        await Assert.That(viewModel.IsInstalled).IsTrue();
    }

    /// <summary>
    ///     Regeneration reports when removing the previous trusted root fails.
    /// </summary>
    [Test]
    public async Task RegenerateCommand_PreviouslyInstalledAndUninstallFails_ReportsOldRemovalFailure()
    {
        var (viewModel, _, _, store) = Create();
        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);
        store.ThrowOnUninstallCallNumber = 1;

        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(viewModel.IsInstalled).IsTrue();
        await Assert.That(viewModel.StatusMessage).Contains("removing the previous certificate");
        await Assert.That(viewModel.StatusMessage).Contains("failed");
    }

    /// <summary>
    ///     Regeneration reports when installing the rotated root fails.
    /// </summary>
    [Test]
    public async Task RegenerateCommand_PreviouslyInstalledAndInstallFails_ReportsNewInstallFailure()
    {
        var (viewModel, _, _, store) = Create();
        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);
        store.ThrowOnInstallCallNumber = 2;

        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(viewModel.IsInstalled).IsFalse();
        await Assert.That(viewModel.StatusMessage).Contains("installing the new certificate");
        await Assert.That(viewModel.StatusMessage).Contains("failed");
    }

    /// <summary>
    ///     InstallCommand defers post-await property updates until scheduled UI work runs.
    /// </summary>
    [Test]
    public async Task InstallCommand_FreshAuthority_DefersBoundPropertyUpdatesUntilUiWorkRuns()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        var (viewModel, _, _, store) = Create(userInterfaceScheduler: scheduler);

        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(store.InstallCallCount).IsEqualTo(1);
        await Assert.That(viewModel.IsBusy).IsTrue();
        await Assert.That(viewModel.IsInstalled).IsFalse();
        await Assert.That(viewModel.StatusMessage).IsEqualTo(string.Empty);

        scheduler.DrainQueue();

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.IsInstalled).IsTrue();
        await Assert.That(viewModel.StatusMessage).Contains("installed");
    }

    /// <summary>
    ///     RegenerateCommand defers post-await property updates until scheduled UI work runs.
    /// </summary>
    [Test]
    public async Task RegenerateCommand_FreshProvider_DefersBoundPropertyUpdatesUntilUiWorkRuns()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        var (viewModel, _, _, _) = Create(userInterfaceScheduler: scheduler);
        await viewModel.RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
        scheduler.DrainQueue();
        var originalThumbprint = viewModel.Thumbprint;

        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(viewModel.IsBusy).IsTrue();
        await Assert.That(viewModel.Thumbprint).IsEqualTo(originalThumbprint);
        await Assert.That(viewModel.StatusMessage).IsEqualTo(string.Empty);

        scheduler.DrainQueue();

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.Thumbprint).IsNotEqualTo(originalThumbprint);
        await Assert.That(viewModel.StatusMessage).Contains("regenerated");
    }

    /// <summary>
    ///     UninstallCommand defers post-await property updates until scheduled UI work runs.
    /// </summary>
    [Test]
    public async Task UninstallCommand_PreviouslyInstalled_DefersBoundPropertyUpdatesUntilUiWorkRuns()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        var (viewModel, _, _, store) = Create(userInterfaceScheduler: scheduler);
        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);
        scheduler.DrainQueue();

        await viewModel.UninstallCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(store.UninstallCallCount).IsEqualTo(1);
        await Assert.That(viewModel.IsBusy).IsTrue();
        await Assert.That(viewModel.IsInstalled).IsTrue();
        await Assert.That(viewModel.StatusMessage).Contains("installed");

        scheduler.DrainQueue();

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.IsInstalled).IsFalse();
        await Assert.That(viewModel.StatusMessage).Contains("removed");
    }

    /// <summary>
    ///     Dispose is a no-op and safe to call multiple times.
    /// </summary>
    [Test]
    public async Task Dispose_CalledTwice_DoesNotThrow()
    {
        var (viewModel, _, _, _) = Create();
        viewModel.Dispose();
        await Assert.That(() => viewModel.Dispose()).ThrowsNothing();
    }

    /// <summary>
    ///     All long-running commands short-circuit when IsBusy is already set.
    /// </summary>
    [Test]
    public async Task LongRunningCommands_IsBusyAlreadyTrue_AreShortCircuited()
    {
        var (viewModel, _, _, store) = Create();
        viewModel.IsBusy = true;

        await viewModel.InstallCommand.ExecuteAsync(null).ConfigureAwait(false);
        await viewModel.UninstallCommand.ExecuteAsync(null).ConfigureAwait(false);
        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(false);
        await viewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        await Assert.That(store.InstallCallCount).IsEqualTo(0);
        await Assert.That(store.UninstallCallCount).IsEqualTo(0);
    }

    private static (CertificateManagerViewModel ViewModel, StubCertificateGenerator Generator, MutableCertificateAuthorityProvider Provider, StubCertificateStore Store) Create(
        ShellViewModelFactory.StubFilePickerService? picker = null,
        IUserInterfaceScheduler? userInterfaceScheduler = null)
    {
        var generator = new StubCertificateGenerator();
        var provider = new MutableCertificateAuthorityProvider(generator);
        var store = new StubCertificateStore();
        var filePicker = picker ?? new ShellViewModelFactory.StubFilePickerService();
        var scheduler = userInterfaceScheduler ?? InlineUserInterfaceScheduler.Instance;
        var viewModel = new CertificateManagerViewModel(provider, store, filePicker, scheduler);
        return (viewModel, generator, provider, store);
    }
}
