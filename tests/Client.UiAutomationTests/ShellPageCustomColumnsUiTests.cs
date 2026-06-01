using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Custom Header Columns tool window
///     opened from <c>Tools → Custom Header Column...</c>
///     (<c>docs/DESIGN.md § 6.24 Custom Columns</c>).
/// </summary>
public sealed class ShellPageCustomColumnsUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenCustomColumns_FromToolsMenu_ShowsCustomColumnsWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var columns = shell.OpenToolWindow("Tools", "Custom Header Column...", "Custom Header Columns");
        try
        {
            await Assert.That(columns.GetTitle()).IsEqualTo("Custom Header Columns");
            await Assert.That(columns.HasButton("Add Column")).IsTrue();
            await Assert.That(columns.TextBoxByName("Column display name")).IsNotNull();
            await Assert.That(columns.TextBoxByName("Header key")).IsNotNull();
            await Assert.That(columns.ComboBoxByName("Column source")).IsNotNull();
            await Assert.That(columns.ListBoxByName("Configured columns")).IsNotNull();
        }
        finally
        {
            columns.Close();
        }
    }

    [Test]
    public async Task AddColumnButton_AfterTypingNameAndHeader_AppendsRowToList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var columns = shell.OpenToolWindow("Tools", "Custom Header Column...", "Custom Header Columns");
        try
        {
            var name = columns.TextBoxByName("Column display name");
            name.Focus();
            Keyboard.Type("Request ID");
            columns.WaitUntil(
                () => string.Equals(name.Text, "Request ID", StringComparison.Ordinal),
                description: "display name textbox populated");

            var headerKey = columns.TextBoxByName("Header key");
            headerKey.Focus();
            Keyboard.Type("X-Request-Id");
            columns.WaitUntil(
                () => string.Equals(headerKey.Text, "X-Request-Id", StringComparison.Ordinal),
                description: "header key textbox populated");

            columns.Button("Add Column").Click();

            var list = columns.ListBoxByName("Configured columns");
            columns.WaitUntil(
                () => list.Items.Length >= 1,
                description: "configured columns grew to at least 1 entry");
        }
        finally
        {
            columns.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task AddColumnButton_EmptyNameAndHeader_LeavesListEmpty()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var columns = shell.OpenToolWindow("Tools", "Custom Header Column...", "Custom Header Columns");
        try
        {
            columns.Button("Add Column").Click();
            System.Threading.Thread.Sleep(200);

            var list = columns.ListBoxByName("Configured columns");
            await Assert.That(list.Items.Length).IsEqualTo(0);
        }
        finally
        {
            columns.Close();
        }
    }
}
