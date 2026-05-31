using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the gRPC Descriptors tool window opened
///     from <c>Tools → gRPC Descriptors...</c> (<c>docs/DESIGN.md § 6.19 gRPC
///     Inspection</c>).
/// </summary>
public sealed class ShellPageRemoteProcedureCallDescriptorsUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenRemoteProcedureCallDescriptors_FromToolsMenu_ShowsGrpcDescriptorsWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var descriptors = shell.OpenToolWindow("Tools", "gRPC Descriptors...", "gRPC Descriptors");
        try
        {
            await Assert.That(descriptors.GetTitle()).IsEqualTo("gRPC Descriptors");
            await Assert.That(descriptors.HasButton("Load .pb File...")).IsTrue();
            await Assert.That(descriptors.HasButton("Unload Selected")).IsTrue();
            await Assert.That(descriptors.HasButton("Clear All")).IsTrue();
            await Assert.That(descriptors.ListBoxByName("Loaded descriptor files")).IsNotNull();
        }
        finally
        {
            descriptors.Close();
        }
    }

    [Test]
    public async Task ClearAllButton_EmptyDescriptors_LeavesWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var descriptors = shell.OpenToolWindow("Tools", "gRPC Descriptors...", "gRPC Descriptors");
        try
        {
            descriptors.Button("Clear All").Click();

            await Assert.That(descriptors.GetTitle()).IsEqualTo("gRPC Descriptors");
            await Assert.That(descriptors.HasButton("Load .pb File...")).IsTrue();
        }
        finally
        {
            descriptors.Close();
        }
    }
}
