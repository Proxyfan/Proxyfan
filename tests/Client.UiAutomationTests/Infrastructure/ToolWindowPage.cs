using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;

namespace Proxyfan.Client.UiAutomationTests.Infrastructure;

/// <summary>
///     Page-object wrapper around a tool window that the shell opens in response
///     to a menu command (Preferences, Block List, Map Local, etc.). Encapsulates
///     element discovery + bounded polling, and provides graceful close behaviour.
///     <para>
///         All discovery helpers <em>poll</em> the visual tree with a bounded
///         timeout — Avalonia surfaces UIA peers asynchronously as the window
///         materialises, so a one-shot <c>FindFirstDescendant</c> is racy. Use
///         the typed accessors (<see cref="Button" />, <see cref="TextBoxByName" />,
///         etc.) instead of raw FlaUI calls.
///     </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ToolWindowPage : IDisposable
{
    /// <summary>
    ///     Default upper bound on element discovery. Matches
    ///     <see cref="ShellPage.DefaultElementTimeout" /> for consistency.
    /// </summary>
    public static TimeSpan DefaultElementTimeout { get; } = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     The underlying FlaUI tool window. Tests should prefer the typed
    ///     helpers on this class but the raw window is exposed for visual-tree
    ///     assertions that need direct access.
    /// </summary>
    public Window Window { get; }

    /// <summary>
    ///     Initializes a new <see cref="ToolWindowPage" /> wrapping the supplied
    ///     FlaUI window.
    /// </summary>
    /// <param name="window">The tool window to wrap.</param>
    public ToolWindowPage(Window window)
    {
        Window = window;
    }

    /// <summary>
    ///     Returns the window title text. Tests use this to assert the
    ///     expected tool window opened.
    /// </summary>
    /// <returns>The tool window title.</returns>
    public string GetTitle()
    {
        return Window.Title ?? string.Empty;
    }

    /// <summary>
    ///     Returns a button whose visible text matches <paramref name="text" />.
    ///     Polls until the button materialises or the timeout elapses.
    /// </summary>
    /// <param name="text">The button label to match.</param>
    /// <returns>The matched button.</returns>
    public Button Button(string text)
    {
        var raw = WaitForRaw(cf => cf.ByName(text).And(cf.ByControlType(ControlType.Button)));
        return raw.AsButton();
    }

    /// <summary>
    ///     Returns a check box whose visible text matches <paramref name="text" />.
    /// </summary>
    /// <param name="text">The check box label to match.</param>
    /// <returns>The matched check box.</returns>
    public CheckBox CheckBox(string text)
    {
        var raw = WaitForRaw(cf => cf.ByName(text).And(cf.ByControlType(ControlType.CheckBox)));
        return raw.AsCheckBox();
    }

    /// <summary>
    ///     Returns a text box discovered by its accessibility name (typically
    ///     set on the XAML via <c>AutomationProperties.Name</c>).
    /// </summary>
    /// <param name="accessibilityName">The accessibility name.</param>
    /// <returns>The matched text box.</returns>
    public TextBox TextBoxByName(string accessibilityName)
    {
        var raw = WaitForRaw(cf => cf.ByName(accessibilityName).And(cf.ByControlType(ControlType.Edit)));
        return raw.AsTextBox();
    }

    /// <summary>
    ///     Returns a list box discovered by its accessibility name.
    /// </summary>
    /// <param name="accessibilityName">The accessibility name.</param>
    /// <returns>The matched list box.</returns>
    public ListBox ListBoxByName(string accessibilityName)
    {
        var raw = WaitForRaw(cf => cf.ByName(accessibilityName).And(cf.ByControlType(ControlType.List)));
        return raw.AsListBox();
    }

    /// <summary>
    ///     Returns a combo box discovered by its accessibility name.
    /// </summary>
    /// <param name="accessibilityName">The accessibility name.</param>
    /// <returns>The matched combo box.</returns>
    public ComboBox ComboBoxByName(string accessibilityName)
    {
        var raw = WaitForRaw(cf => cf.ByName(accessibilityName).And(cf.ByControlType(ControlType.ComboBox)));
        return raw.AsComboBox();
    }

    /// <summary>
    ///     Returns <see langword="true" /> if the visible window has any text
    ///     element matching <paramref name="text" /> exactly. Used for label /
    ///     status assertions.
    /// </summary>
    /// <param name="text">The text to look for.</param>
    /// <returns>Whether the text was found.</returns>
    public bool HasVisibleText(string text)
    {
        var match = Window.FindFirstDescendant(cf =>
            cf.ByName(text).And(cf.ByControlType(ControlType.Text)));
        return match is not null;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if the visible window has any button
    ///     whose label matches <paramref name="label" /> exactly. Useful for
    ///     asserting toolbar layouts.
    /// </summary>
    /// <param name="label">The button label to look for.</param>
    /// <returns>Whether the button was found.</returns>
    public bool HasButton(string label)
    {
        var match = Window.FindFirstDescendant(cf =>
            cf.ByName(label).And(cf.ByControlType(ControlType.Button)));
        return match is not null;
    }

    /// <summary>
    ///     Polls the visual tree for an element matching the supplied condition.
    /// </summary>
    /// <param name="conditionFactory">Builds the search condition.</param>
    /// <param name="timeout">Optional upper bound; defaults to <see cref="DefaultElementTimeout" />.</param>
    /// <returns>The matched element.</returns>
    public AutomationElement WaitForRaw(
        Func<ConditionFactory, ConditionBase> conditionFactory,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultElementTimeout;
        AutomationElement? result = null;
        var found = Retry.WhileNull(
            () =>
            {
                var condition = conditionFactory(Window.Automation.ConditionFactory);
                result = Window.FindFirstDescendant(condition);
                return result;
            },
            effectiveTimeout,
            interval: TimeSpan.FromMilliseconds(150),
            throwOnTimeout: false);

        if (!found.Success || result is null)
        {
            throw new TimeoutException(
                $"Timed out after {effectiveTimeout.TotalSeconds:F1}s waiting for an element in tool window '{GetTitle()}'.");
        }

        return result;
    }

    /// <summary>
    ///     Polls until <paramref name="predicate" /> returns <see langword="true" />.
    /// </summary>
    /// <param name="predicate">The condition that must become true.</param>
    /// <param name="description">Human-readable description for timeout messages.</param>
    /// <param name="timeout">Optional upper bound; defaults to <see cref="DefaultElementTimeout" />.</param>
    public void WaitUntil(Func<bool> predicate, string description, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultElementTimeout;
        var success = Retry.WhileFalse(predicate, effectiveTimeout, interval: TimeSpan.FromMilliseconds(150), throwOnTimeout: false);
        if (!success.Success)
        {
            throw new TimeoutException(
                $"Timed out after {effectiveTimeout.TotalSeconds:F1}s waiting for: {description}.");
        }
    }

    /// <summary>
    ///     Returns every button currently visible in the tool window. Used to
    ///     assert the presence of a fixed button row (Install/Export/etc.).
    /// </summary>
    /// <returns>The list of buttons.</returns>
    public string[] AllButtonLabels()
    {
        return Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                     .Select(b => b.Name ?? string.Empty)
                     .Where(name => !string.IsNullOrEmpty(name))
                     .ToArray();
    }

    /// <summary>
    ///     Closes this tool window via FlaUI's native window-close. Tests should
    ///     call this in a <c>finally</c> block so that a failed assertion does not
    ///     leave a window on screen for the next test in the suite.
    /// </summary>
    public void Close()
    {
        try
        {
            Window.Close();
        }
        catch
        {
            // Best-effort: a window may already be gone (e.g. owner closed it).
        }
    }

    /// <summary>
    ///     Disposes the wrapper by closing the underlying window.
    /// </summary>
    public void Dispose()
    {
        Close();
        // Window itself is owned by the test app process; nothing else to release.
        Thread.Sleep(50);
    }
}
