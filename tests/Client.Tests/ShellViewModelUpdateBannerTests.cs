using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Domain.Updates;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests covering the in-shell update banner behaviour of <see cref="ShellViewModel" />.
/// </summary>
public sealed class ShellViewModelUpdateBannerTests
{
    /// <summary>
    ///     Verifies the banner is hidden when no notification has been published.
    /// </summary>
    [Test]
    public async Task UpdateBanner_InitialState_IsHidden()
    {
        var notification = new MutableUpdateNotification();
        var viewModel = CreateViewModel(notification);

        await Assert.That(viewModel.IsUpdateBannerVisible).IsFalse();
        await Assert.That(viewModel.UpdateBannerMessage).IsNull();
        await Assert.That(viewModel.UpdateBannerDownloadUrl).IsNull();
    }

    /// <summary>
    ///     Verifies the banner becomes visible and reflects the version when an update is published.
    /// </summary>
    [Test]
    public async Task UpdateBanner_WhenUpdatePublished_ReflectsNewVersion()
    {
        var notification = new MutableUpdateNotification();
        var viewModel = CreateViewModel(notification);

        notification.Publish(CreateUpdate("2.5.0", "https://example.com/release"));

        await Assert.That(viewModel.IsUpdateBannerVisible).IsTrue();
        await Assert.That(viewModel.UpdateBannerMessage).IsEqualTo("Proxyfan 2.5.0 is available.");
        await Assert.That(viewModel.UpdateBannerDownloadUrl).IsEqualTo("https://example.com/release");
    }

    /// <summary>
    ///     Verifies the banner reflects the latest update if one was published before the
    ///     view model was constructed.
    /// </summary>
    [Test]
    public async Task UpdateBanner_WhenUpdateAlreadyKnown_BecomesVisibleOnConstruction()
    {
        var notification = new MutableUpdateNotification();
        notification.Publish(CreateUpdate("3.0.0", "https://example.com/r/3"));

        var viewModel = CreateViewModel(notification);

        await Assert.That(viewModel.IsUpdateBannerVisible).IsTrue();
        await Assert.That(viewModel.UpdateBannerMessage).IsEqualTo("Proxyfan 3.0.0 is available.");
    }

    /// <summary>
    ///     Verifies the dismiss command hides the banner without affecting message text.
    /// </summary>
    [Test]
    public async Task DismissUpdateBanner_WhenVisible_HidesBanner()
    {
        var notification = new MutableUpdateNotification();
        var viewModel = CreateViewModel(notification);
        notification.Publish(CreateUpdate("2.5.0", "https://example.com/release"));

        viewModel.DismissUpdateBannerCommand.Execute(null);

        await Assert.That(viewModel.IsUpdateBannerVisible).IsFalse();
    }

    /// <summary>
    ///     Verifies clearing the notification hides the banner and clears the message.
    /// </summary>
    [Test]
    public async Task UpdateBanner_WhenNotificationCleared_HidesAndClears()
    {
        var notification = new MutableUpdateNotification();
        var viewModel = CreateViewModel(notification);
        notification.Publish(CreateUpdate("2.5.0", "https://example.com/release"));

        notification.Clear();

        await Assert.That(viewModel.IsUpdateBannerVisible).IsFalse();
        await Assert.That(viewModel.UpdateBannerMessage).IsNull();
        await Assert.That(viewModel.UpdateBannerDownloadUrl).IsNull();
    }

    /// <summary>
    ///     Verifies disposing the view model unsubscribes from the notification so further
    ///     publishes are ignored.
    /// </summary>
    [Test]
    public async Task Dispose_AfterDispose_FurtherPublishesAreIgnored()
    {
        var notification = new MutableUpdateNotification();
        var viewModel = CreateViewModel(notification);

        viewModel.Dispose();
        notification.Publish(CreateUpdate("4.0.0", "https://example.com/four"));

        await Assert.That(viewModel.IsUpdateBannerVisible).IsFalse();
        await Assert.That(viewModel.UpdateBannerMessage).IsNull();
    }

    private static ShellViewModel CreateViewModel(MutableUpdateNotification notification)
    {
        return ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            new StubToolWindowOpener(),
            notification);
    }

    private static UpdateInfo CreateUpdate(string version, string downloadUrl)
    {
        var update = new UpdateInfo
        {
            Version = version,
            DownloadUrl = downloadUrl,
            ReleaseNotes = "notes",
        };
        return update;
    }
}
