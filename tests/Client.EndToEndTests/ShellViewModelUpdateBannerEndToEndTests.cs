using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Domain.Updates;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the in-shell update banner described in
///     <c>docs/DESIGN.md § 12 Auto-Update</c>. The banner appears whenever the
///     <see cref="Proxyfan.Domain.Updates.MutableUpdateNotification" /> reports a
///     newer version available, surfaces the download URL, and can be dismissed
///     by the user. Republishing the same update after dismissal does not
///     re-raise the banner (per the no-duplicate-notification spec).
/// </summary>
public sealed class ShellViewModelUpdateBannerEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task IsUpdateBannerVisible_FreshShell_StartsHidden()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsFalse();
        });
    }

    [Test]
    public async Task PublishUpdate_NewVersionAvailable_BannerBecomesVisible()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var update = new UpdateInfo
            {
                Version = "2026.6.1",
                DownloadUrl = "https://example.com/proxyfan-2026.6.1.msi",
            };

            env.UpdateNotification.Publish(update);

            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsTrue();
            await Assert.That(env.ShellViewModel.UpdateBannerMessage).IsEqualTo("Proxyfan 2026.6.1 is available.");
            await Assert.That(env.ShellViewModel.UpdateBannerDownloadUrl).IsEqualTo("https://example.com/proxyfan-2026.6.1.msi");
        });
    }

    [Test]
    public async Task DismissUpdateBanner_AfterPublish_HidesBanner()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.UpdateNotification.Publish(new UpdateInfo
            {
                Version = "2026.6.1",
                DownloadUrl = "https://example.com/proxyfan.msi",
            });
            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsTrue();

            env.ShellViewModel.DismissUpdateBannerCommand.Execute(null);

            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsFalse();
        });
    }

    [Test]
    public async Task PublishUpdate_SameVersionTwice_DoesNotReRaiseBannerAfterDismiss()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var update = new UpdateInfo
            {
                Version = "2026.6.1",
                DownloadUrl = "https://example.com/proxyfan.msi",
            };
            env.UpdateNotification.Publish(update);
            env.ShellViewModel.DismissUpdateBannerCommand.Execute(null);

            // Republishing the *same* version is a no-op per UpdateInfoEquivalence;
            // the dismissed banner must stay hidden.
            env.UpdateNotification.Publish(update);

            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsFalse();
        });
    }

    [Test]
    public async Task PublishUpdate_NewerVersionAfterDismiss_ReRaisesBanner()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.UpdateNotification.Publish(new UpdateInfo
            {
                Version = "2026.6.1",
                DownloadUrl = "https://example.com/proxyfan-2026.6.1.msi",
            });
            env.ShellViewModel.DismissUpdateBannerCommand.Execute(null);

            env.UpdateNotification.Publish(new UpdateInfo
            {
                Version = "2026.6.2",
                DownloadUrl = "https://example.com/proxyfan-2026.6.2.msi",
            });

            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsTrue();
            await Assert.That(env.ShellViewModel.UpdateBannerMessage).IsEqualTo("Proxyfan 2026.6.2 is available.");
        });
    }

    [Test]
    public async Task Clear_AfterPublish_HidesBanner()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            env.UpdateNotification.Publish(new UpdateInfo
            {
                Version = "2026.6.1",
                DownloadUrl = "https://example.com/proxyfan.msi",
            });

            env.UpdateNotification.Clear();

            await Assert.That(env.ShellViewModel.IsUpdateBannerVisible).IsFalse();
            await Assert.That(env.ShellViewModel.UpdateBannerMessage).IsNull();
            await Assert.That(env.ShellViewModel.UpdateBannerDownloadUrl).IsNull();
        });
    }
}
