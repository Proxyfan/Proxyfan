using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proxyfan.Client.EndToEndTests.Infrastructure;

/// <summary>
///     Visual-tree walking helpers used by page objects to locate controls by
///     <see cref="AutomationProperties.Name" /> or by .NET <see cref="Control.Name" />.
///     Walks both the logical and visual tree so that controls inside
///     <see cref="ItemsControl" /> templates are reachable before they are realized
///     in the visual tree.
/// </summary>
internal static class UiTreeFinder
{
    /// <summary>
    ///     Walks <paramref name="root" /> and returns the first control whose
    ///     <see cref="AutomationProperties.NameProperty" /> resolves to
    ///     <paramref name="automationName" />.
    /// </summary>
    /// <param name="root">The root visual to traverse.</param>
    /// <param name="automationName">The automation name to match (case-sensitive).</param>
    /// <typeparam name="TControl">The expected concrete control type.</typeparam>
    /// <returns>The matching control.</returns>
    /// <exception cref="InvalidOperationException">No control matched.</exception>
    public static TControl FindByAutomationName<TControl>(Visual root, string automationName)
        where TControl : Control
    {
        foreach (var control in EnumerateControls(root))
        {
            var name = AutomationProperties.GetName(control);
            if (string.Equals(name, automationName, StringComparison.Ordinal) && control is TControl typed)
            {
                return typed;
            }
        }

        throw new InvalidOperationException(
            $"No control of type {typeof(TControl).Name} with AutomationProperties.Name='{automationName}' found in the visual tree.");
    }

    /// <summary>
    ///     Walks <paramref name="root" /> and returns the first control whose
    ///     <see cref="StyledElement.Name" /> matches <paramref name="name" />.
    /// </summary>
    /// <param name="root">The root visual to traverse.</param>
    /// <param name="name">The x:Name to match.</param>
    /// <typeparam name="TControl">The expected concrete control type.</typeparam>
    /// <returns>The matching control.</returns>
    /// <exception cref="InvalidOperationException">No control matched.</exception>
    public static TControl FindByName<TControl>(Visual root, string name)
        where TControl : Control
    {
        foreach (var control in EnumerateControls(root))
        {
            if (string.Equals(control.Name, name, StringComparison.Ordinal) && control is TControl typed)
            {
                return typed;
            }
        }

        throw new InvalidOperationException(
            $"No control of type {typeof(TControl).Name} with Name='{name}' found in the visual tree.");
    }

    /// <summary>
    ///     Returns all descendant controls of <paramref name="root" /> assignable
    ///     to <typeparamref name="TControl" />, in pre-order traversal.
    /// </summary>
    /// <param name="root">The root visual to traverse.</param>
    /// <typeparam name="TControl">The expected concrete control type.</typeparam>
    /// <returns>Matching controls.</returns>
    public static IReadOnlyList<TControl> FindAll<TControl>(Visual root)
        where TControl : Control
    {
        var matches = new List<TControl>();
        foreach (var control in EnumerateControls(root))
        {
            if (control is TControl typed)
            {
                matches.Add(typed);
            }
        }
        return matches;
    }

    private static IEnumerable<Control> EnumerateControls(Visual root)
    {
        var stack = new Stack<Visual>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is Control asControl)
            {
                yield return asControl;
            }

            // Walk the visual tree (realized controls) AND the logical tree (templates,
            // items not yet materialized) so we can find controls in both states.
            foreach (var child in current.GetVisualChildren())
            {
                stack.Push(child);
            }

            if (current is ILogical logical)
            {
                foreach (var logicalChild in logical.LogicalChildren.OfType<Visual>())
                {
                    if (!current.GetVisualChildren().Contains(logicalChild))
                    {
                        stack.Push(logicalChild);
                    }
                }
            }
        }
    }
}
