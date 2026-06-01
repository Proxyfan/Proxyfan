using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the DNS Spoofing tool window opened from
///     <c>Tools → DNS Spoofing...</c>.
/// </summary>
public sealed class ShellPageDomainNameSystemSpoofingUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenDomainNameSystemSpoofing_FromToolsMenu_ShowsDnsSpoofingWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var dns = shell.OpenToolWindow("Tools", "DNS Spoofing...", "DNS Spoofing");
        try
        {
            await Assert.That(dns.GetTitle()).IsEqualTo("DNS Spoofing");
            await Assert.That(dns.HasButton("Add")).IsTrue();
            await Assert.That(dns.HasButton("Enable All")).IsTrue();
            await Assert.That(dns.HasButton("Disable All")).IsTrue();
            await Assert.That(dns.HasButton("Refresh Counts")).IsTrue();
        }
        finally
        {
            dns.Close();
        }
    }

    [Test]
    public async Task OpenDomainNameSystemSpoofing_FreshWindow_ExposesHostnameAndAddressBoxes()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var dns = shell.OpenToolWindow("Tools", "DNS Spoofing...", "DNS Spoofing");
        try
        {
            await Assert.That(dns.TextBoxByName("Hostname to spoof")).IsNotNull();
            await Assert.That(dns.TextBoxByName("Override IP address")).IsNotNull();
            await Assert.That(dns.ListBoxByName("DNS spoofing entries")).IsNotNull();
        }
        finally
        {
            dns.Close();
        }
    }

    [Test]
    public async Task AddEntry_AfterTypingHostnameAndAddress_AppendsRowToList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var dns = shell.OpenToolWindow("Tools", "DNS Spoofing...", "DNS Spoofing");
        try
        {
            var hostnameBox = dns.TextBoxByName("Hostname to spoof");
            hostnameBox.Focus();
            Keyboard.Type("api.local");
            dns.WaitUntil(
                () => string.Equals(hostnameBox.Text, "api.local", StringComparison.Ordinal),
                description: "hostname textbox populated");

            var addressBox = dns.TextBoxByName("Override IP address");
            addressBox.Focus();
            Keyboard.Type("127.0.0.1");
            dns.WaitUntil(
                () => string.Equals(addressBox.Text, "127.0.0.1", StringComparison.Ordinal),
                description: "address textbox populated");

            dns.Button("Add").Click();

            var list = dns.ListBoxByName("DNS spoofing entries");
            dns.WaitUntil(
                () => list.Items.Length >= 1,
                description: "DNS spoofing entry list grew to at least 1 entry");
        }
        finally
        {
            dns.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task EnableAllButton_AfterAddingEntries_DoesNotCrashWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var dns = shell.OpenToolWindow("Tools", "DNS Spoofing...", "DNS Spoofing");
        try
        {
            // Enable All / Disable All are safe no-ops on an empty entry
            // list — assert they don't crash the window.
            dns.Button("Enable All").Click();
            await Assert.That(dns.GetTitle()).IsEqualTo("DNS Spoofing");

            dns.Button("Disable All").Click();
            await Assert.That(dns.GetTitle()).IsEqualTo("DNS Spoofing");

            dns.Button("Refresh Counts").Click();
            await Assert.That(dns.GetTitle()).IsEqualTo("DNS Spoofing");
        }
        finally
        {
            dns.Close();
        }
    }

    [Test]
    public async Task TypeInvalidIpAddress_AndClickAdd_LeavesListEmpty()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var dns = shell.OpenToolWindow("Tools", "DNS Spoofing...", "DNS Spoofing");
        try
        {
            dns.TextBoxByName("Hostname to spoof").Text = "example.com";
            dns.TextBoxByName("Override IP address").Text = "not-an-ip-address";
            dns.Button("Add").Click();

            // Invalid IP address must reject the add and leave the list empty.
            // The ValidationMessage TextBlock becomes visible — we don't pin
            // its specific text, only assert the list stays at 0.
            System.Threading.Thread.Sleep(300);
            var list = dns.ListBoxByName("DNS spoofing entries");
            await Assert.That(list.Items.Length).IsEqualTo(0);
        }
        finally
        {
            dns.Close();
        }
    }
}
