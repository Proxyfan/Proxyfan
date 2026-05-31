using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using System;
using System.Linq;
using System.Runtime.Versioning;

namespace Proxyfan.Client.UiAutomationTests.Infrastructure;

/// <summary>
///     Page-object wrapper around a live Proxyfan shell window discovered via
///     FlaUI. Encapsulates control discovery (by accessibility name) and the
///     waiting / hit-testing primitives every test needs so individual test
///     bodies stay declarative.
///     <para>
///         All discovery methods are <em>polling</em> with a bounded timeout:
///         Avalonia's UI Automation provider populates names asynchronously as
///         the visual tree materialises, so a hard one-shot <c>FindFirst</c>
///         is racy. Use <see cref="WaitForElement{TElement}" /> for any
///         control that may not yet be rendered.
///     </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ShellPage
{
    /// <summary>
    ///     Default upper bound on element discovery. Picked to be long enough to
    ///     ride out a JIT pause on a slow agent yet short enough that a true
    ///     missing-element bug fails fast.
    /// </summary>
    public static TimeSpan DefaultElementTimeout { get; } = TimeSpan.FromSeconds(15);

    private readonly ProxyfanApp _app;

    /// <summary>
    ///     The underlying main window. Tests should prefer the typed helpers on
    ///     this class but the raw window is exposed for visual-tree assertions.
    /// </summary>
    public Window Window { get; }

    /// <summary>
    ///     Initializes a new <see cref="ShellPage" /> pointing at the supplied
    ///     launched app.
    /// </summary>
    /// <param name="app">The launched app to wrap.</param>
    public ShellPage(ProxyfanApp app)
    {
        _app = app;
        Window = app.GetMainWindow();
    }

    /// <summary>
    ///     Returns the localized text shown in the window title bar.
    /// </summary>
    /// <returns>The window title.</returns>
    public string GetTitle()
    {
        return Window.Title ?? string.Empty;
    }

    /// <summary>
    ///     Returns the live <see cref="TextBox" /> bound to
    ///     <see cref="Proxyfan.Client.Traffic.ViewModels.TrafficListViewModel.FilterText" />,
    ///     discovered by its automation name ("Filter traffic").
    /// </summary>
    /// <returns>The filter text box.</returns>
    public TextBox FilterTextBox()
    {
        var raw = WaitForRaw(cf => cf.ByAutomationId("FilterTextBox").Or(cf.ByName("Filter traffic")));
        return raw.AsTextBox();
    }

    /// <summary>
    ///     Returns the source-list <see cref="ListBox" />, discovered by its
    ///     automation name ("Sources").
    /// </summary>
    /// <returns>The source list.</returns>
    public ListBox SourceList()
    {
        var raw = WaitForRaw(cf => cf.ByName("Sources"));
        return raw.AsListBox();
    }

    /// <summary>
    ///     Returns the workspace tab strip, discovered by its automation name
    ///     ("Open tabs").
    /// </summary>
    /// <returns>The tab strip.</returns>
    public ListBox TabList()
    {
        var raw = WaitForRaw(cf => cf.ByName("Open tabs"));
        return raw.AsListBox();
    }

    /// <summary>
    ///     Returns the "+" new-tab <see cref="Button" />, discovered by its
    ///     automation name ("New tab").
    /// </summary>
    /// <returns>The new-tab button.</returns>
    public Button NewTabButton()
    {
        var raw = WaitForRaw(cf => cf.ByName("New tab"));
        return raw.AsButton();
    }

    /// <summary>
    ///     Returns the first menu bar found in the shell (there is exactly one).
    /// </summary>
    /// <returns>The menu bar.</returns>
    public Menu MenuBar()
    {
        var raw = WaitForRaw(cf => cf.ByControlType(ControlType.MenuBar));
        return raw.AsMenu();
    }

    /// <summary>
    ///     Returns a toolbar button whose visible text content matches
    ///     <paramref name="text" />. The shell toolbar swaps visible Pause/Resume,
    ///     Enable/Disable buttons based on application state, so this helper
    ///     polls until the requested label is visible.
    /// </summary>
    /// <param name="text">The visible label to match (e.g. "Pause Capture").</param>
    /// <returns>The matching button.</returns>
    public Button ToolbarButton(string text)
    {
        var raw = WaitForRaw(cf => cf.ByName(text).And(cf.ByControlType(ControlType.Button)));
        return raw.AsButton();
    }

    /// <summary>
    ///     Returns every "Close tab" button currently visible in the tab strip
    ///     (one per closeable tab). The default first tab is sticky and renders
    ///     no close button, so the count of close buttons equals
    ///     <c>TabList().Items.Length - 1</c>.
    /// </summary>
    /// <returns>The list of close buttons.</returns>
    public Button[] CloseTabButtons()
    {
        var matches = Window.FindAllDescendants(cf =>
            cf.ByName("Close tab").And(cf.ByControlType(ControlType.Button)));
        return matches.Select(m => m.AsButton()).ToArray();
    }

    /// <summary>
    ///     Returns <see langword="true" /> if any visible text on the window
    ///     matches <paramref name="text" /> exactly (case-sensitive). Used to
    ///     assert the presence of status-bar labels like "Capture paused".
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
    ///     Returns the first <see cref="TextBlock" />-like control whose accessible
    ///     name matches <paramref name="text" />. Used to assert status bar /
    ///     toolbar labels.
    /// </summary>
    /// <param name="text">The accessible name to match.</param>
    /// <returns>The matching element.</returns>
    public AutomationElement TextElement(string text)
    {
        return WaitForRaw(cf => cf.ByName(text));
    }

    /// <summary>
    ///     Polls the visual tree until an element matching <paramref name="conditionFactory" />
    ///     is found, then returns it cast to <typeparamref name="TElement" />.
    /// </summary>
    /// <typeparam name="TElement">The expected concrete element type.</typeparam>
    /// <param name="conditionFactory">Builds the search condition from the live factory.</param>
    /// <param name="timeout">Optional upper bound; defaults to <see cref="DefaultElementTimeout" />.</param>
    /// <returns>The matched element.</returns>
    public TElement WaitForElement<TElement>(
        Func<ConditionFactory, ConditionBase> conditionFactory,
        TimeSpan? timeout = null)
        where TElement : AutomationElement
    {
        var raw = WaitForRaw(conditionFactory, timeout);
        if (raw is TElement typed)
        {
            return typed;
        }

        // Some Avalonia controls surface as their AutomationElement base. Try a
        // typed cast via FlaUI's pattern-aware factory.
        var asTyped = raw as TElement
                      ?? throw new InvalidOperationException(
                          $"Element found but cannot be cast to {typeof(TElement).Name}. Actual type: {raw.GetType().Name}.");
        return asTyped;
    }

    /// <summary>
    ///     Polls the visual tree for the first element matching the supplied
    ///     condition. Returns the raw <see cref="AutomationElement" />; callers
    ///     usually want <see cref="WaitForElement{TElement}" /> instead.
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
                var condition = conditionFactory(_app.Automation.ConditionFactory);
                result = Window.FindFirstDescendant(condition);
                return result;
            },
            effectiveTimeout,
            interval: TimeSpan.FromMilliseconds(150),
            throwOnTimeout: false);

        if (!found.Success || result is null)
        {
            throw new TimeoutException(
                $"Timed out after {effectiveTimeout.TotalSeconds:F1}s waiting for an element matching the supplied condition.");
        }

        return result;
    }

    /// <summary>
    ///     Polls until <paramref name="predicate" /> returns <see langword="true" />.
    ///     Useful for waiting on observable state that a UI gesture is expected to
    ///     produce (e.g. a button swap, a count change).
    /// </summary>
    /// <param name="predicate">The condition that must become true.</param>
    /// <param name="description">Human-readable description used in timeout messages.</param>
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
    ///     Returns the top-level menu items in the shell menu bar.
    /// </summary>
    /// <returns>The top-level menu items.</returns>
    public Menu[] TopLevelMenus()
    {
        return Window.FindAllChildren(cf => cf.ByControlType(ControlType.MenuBar))
                     .OfType<Menu>()
                     .ToArray();
    }
}
