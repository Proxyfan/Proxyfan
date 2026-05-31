using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Certificate Manager tool window opened
///     from <c>Tools → Certificate Manager...</c> (<c>docs/DESIGN.md § 6.26
///     Certificate Management</c>).
/// </summary>
public sealed class ShellPageCertificateManagerUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenCertificateManager_FromToolsMenu_ShowsCertificateManagerWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var certManager = shell.OpenToolWindow("Tools", "Certificate Manager...", "Certificate Manager");
        try
        {
            await Assert.That(certManager.GetTitle()).IsEqualTo("Certificate Manager");
            await Assert.That(certManager.HasButton("Install in Trust Store")).IsTrue();
            await Assert.That(certManager.HasButton("Remove from Trust Store")).IsTrue();
            await Assert.That(certManager.HasButton("Export...")).IsTrue();
            await Assert.That(certManager.HasButton("Regenerate")).IsTrue();
        }
        finally
        {
            certManager.Close();
        }
    }

    [Test]
    public async Task OpenCertificateManager_FreshWindow_ShowsCertificateMetadataLabels()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var certManager = shell.OpenToolWindow("Tools", "Certificate Manager...", "Certificate Manager");
        try
        {
            await Assert.That(certManager.HasVisibleText("Subject:")).IsTrue();
            await Assert.That(certManager.HasVisibleText("Issuer:")).IsTrue();
            await Assert.That(certManager.HasVisibleText("Thumbprint:")).IsTrue();
            await Assert.That(certManager.HasVisibleText("Valid from:")).IsTrue();
            await Assert.That(certManager.HasVisibleText("Valid until:")).IsTrue();
        }
        finally
        {
            certManager.Close();
        }
    }
}
