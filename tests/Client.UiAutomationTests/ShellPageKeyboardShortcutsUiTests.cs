using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Keyboard Shortcuts tool window opened
///     from <c>View → Keyboard Shortcuts...</c> (<c>docs/DESIGN.md § 9
///     Keyboard Shortcuts</c>).
/// </summary>
public sealed class ShellPageKeyboardShortcutsUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenKeyboardShortcuts_FromViewMenu_ShowsShortcutsWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var shortcuts = shell.OpenToolWindow("View", "Keyboard Shortcuts...", "Keyboard Shortcuts");
        try
        {
            await Assert.That(shortcuts.GetTitle()).IsEqualTo("Keyboard Shortcuts");
            await Assert.That(shortcuts.ListBoxByName("Keyboard shortcut bindings")).IsNotNull();
            await Assert.That(shortcuts.HasButton("Save")).IsTrue();
            await Assert.That(shortcuts.HasButton("Reset to defaults")).IsTrue();
        }
        finally
        {
            shortcuts.Close();
        }
    }

    [Test]
    public async Task OpenKeyboardShortcuts_FreshWindow_BindingsListIsPopulated()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var shortcuts = shell.OpenToolWindow("View", "Keyboard Shortcuts...", "Keyboard Shortcuts");
        try
        {
            var bindings = shortcuts.ListBoxByName("Keyboard shortcut bindings");
            shortcuts.WaitUntil(
                () => bindings.Items.Length >= 1,
                description: "keyboard shortcut bindings populated");
        }
        finally
        {
            shortcuts.Close();
        }

        await Task.CompletedTask;
    }
}
