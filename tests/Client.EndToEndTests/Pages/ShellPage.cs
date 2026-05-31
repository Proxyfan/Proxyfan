using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.Shell.Views;
using System;
using System.Linq;

namespace Proxyfan.Client.EndToEndTests.Pages;

/// <summary>
///     Page-object wrapper around the live <see cref="ShellWindow" /> used by
///     end-to-end tests. Encapsulates control discovery (by localized automation
///     name) and high-level interaction primitives so individual tests can read
///     like a user story.
///     <para>
///         The automation-name lookups use the <i>resolved</i> English text from
///         <c>Resources/Strings.resx</c>, which is what the bound XAML markup
///         expands to once the <see cref="Proxyfan.Presentation.Localization.LocalizationService" />
///         is registered in the test environment.
///     </para>
/// </summary>
public sealed class ShellPage
{
    /// <summary>
    ///     The underlying window. Tests should prefer the typed helpers on this
    ///     class but the raw window is exposed for visual-tree assertions.
    /// </summary>
    public ShellWindow Window { get; }

    /// <summary>
    ///     Initializes a new <see cref="ShellPage" /> pointing at <paramref name="window" />.
    /// </summary>
    /// <param name="window">The shell window to wrap.</param>
    public ShellPage(ShellWindow window)
    {
        Window = window;
    }

    /// <summary>
    ///     Returns the localized text shown in <see cref="Window" />'s
    ///     <see cref="Window.Title" /> property.
    /// </summary>
    /// <returns>The window title.</returns>
    public string GetTitle()
    {
        return Window.Title ?? string.Empty;
    }

    /// <summary>
    ///     Returns the top-level <see cref="Menu" /> control rendered along the top of the shell.
    /// </summary>
    /// <returns>The menu.</returns>
    public Menu Menu()
    {
        var menus = UiTreeFinder.FindAll<Menu>(Window);
        if (menus.Count == 0)
        {
            throw new InvalidOperationException("Shell window does not contain a Menu control.");
        }

        return menus[0];
    }

    /// <summary>
    ///     Returns the source-list <see cref="ListBox" /> control rendered in the left panel.
    /// </summary>
    /// <returns>The source list.</returns>
    public ListBox SourceList()
    {
        return UiTreeFinder.FindByAutomationName<ListBox>(Window, "Sources");
    }

    /// <summary>
    ///     Returns the filter <see cref="TextBox" /> in the toolbar.
    /// </summary>
    /// <returns>The filter text box.</returns>
    public TextBox FilterTextBox()
    {
        return UiTreeFinder.FindByName<TextBox>(Window, "FilterTextBox");
    }

    /// <summary>
    ///     Returns the tab strip <see cref="ListBox" /> hosting workspace tabs.
    /// </summary>
    /// <returns>The tab strip list box.</returns>
    public ListBox TabList()
    {
        return UiTreeFinder.FindByAutomationName<ListBox>(Window, "Open tabs");
    }

    /// <summary>
    ///     Returns the top-level menu items in the menu bar.
    /// </summary>
    /// <returns>All top-level menu items.</returns>
    public MenuItem[] TopLevelMenuItems()
    {
        return Menu().Items.OfType<MenuItem>().ToArray();
    }

    /// <summary>
    ///     Simulates a key press on the shell <see cref="Window" />, processing the
    ///     gesture through Avalonia's input pipeline so <see cref="Window" />-level
    ///     <see cref="KeyBinding" />s fire.
    /// </summary>
    /// <param name="key">The physical key to press.</param>
    /// <param name="modifiers">Optional modifier keys.</param>
    public void PressKey(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPressQwerty(key, modifiers);
        Window.KeyReleaseQwerty(key, modifiers);
    }

    /// <summary>
    ///     Types <paramref name="text" /> into the currently focused control.
    /// </summary>
    /// <param name="text">The text to type.</param>
    public void TypeText(string text)
    {
        Window.KeyTextInput(text);
    }

    /// <summary>
    ///     Forces a layout/render pass so subsequent control geometry queries return
    ///     real (post-arrange) values. Necessary right after <c>window.Show()</c> or
    ///     after a property change that triggers IsVisible flips.
    /// </summary>
    public void PumpUiJobs()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    ///     Returns the centre point of <paramref name="control" /> in the
    ///     <see cref="Window" />'s coordinate system. Requires that a layout pass
    ///     has already run; call <see cref="PumpUiJobs" /> first when in doubt.
    /// </summary>
    /// <param name="control">The control to locate.</param>
    /// <returns>The centre point in window coordinates.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the control has not been arranged into the visual tree (no transform back to the window).
    /// </exception>
    public Point GetCentre(Control control)
    {
        var topLeft = control.TranslatePoint(new Point(0, 0), Window);
        if (topLeft is null)
        {
            throw new InvalidOperationException(
                $"Control of type {control.GetType().Name} has no transform back to the window; was the layout pass run?");
        }

        return topLeft.Value + new Vector(control.Bounds.Width / 2, control.Bounds.Height / 2);
    }

    /// <summary>
    ///     Routes a single left-click through the headless input pipeline at the
    ///     centre of <paramref name="control" />. Equivalent to a user moving the
    ///     mouse over the control and pressing + releasing the primary button.
    /// </summary>
    /// <param name="control">The control to click.</param>
    public void Click(Control control)
    {
        PumpUiJobs();
        var centre = GetCentre(control);
        Window.MouseMove(centre);
        Window.MouseDown(centre, MouseButton.Left);
        Window.MouseUp(centre, MouseButton.Left);
        PumpUiJobs();
    }

    /// <summary>
    ///     Returns the first toolbar <see cref="Button" /> whose visible child
    ///     <see cref="TextBlock" /> matches <paramref name="label" />.
    /// </summary>
    /// <param name="label">The localized label to match.</param>
    /// <returns>The matching button.</returns>
    /// <exception cref="InvalidOperationException">When no matching visible button exists.</exception>
    public Button ToolbarButton(string label)
    {
        PumpUiJobs();
        var buttons = UiTreeFinder.FindAll<Button>(Window);
        foreach (var button in buttons)
        {
            if (!button.IsVisible || !button.IsEffectivelyVisible)
            {
                continue;
            }

            if (button.Content is TextBlock textBlock && string.Equals(textBlock.Text, label, StringComparison.Ordinal))
            {
                return button;
            }
        }

        throw new InvalidOperationException(
            $"No visible toolbar Button with TextBlock content '{label}' found in the shell window.");
    }
}

