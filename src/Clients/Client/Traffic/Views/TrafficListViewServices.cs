using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Domain.Traffic.Columns;
using Proxyfan.Presentation;
using Proxyfan.Presentation.Dialogs;
using Proxyfan.Presentation.Localization;

namespace Proxyfan.Client.Traffic.Views;

/// <summary>
///     Helper that resolves the small set of cross-cutting services the
///     <see cref="TrafficListView" /> code-behind needs to display modal prompts.
///     The traffic list view does not take a constructor dependency on the DI
///     container, so it locates these singletons via <see cref="ContainerLocator" />.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Avalonia/host plumbing: requires UI thread/desktop integration, not unit-testable.")]
public static class TrafficListViewServices
{
    /// <summary>
    ///     Resolves the application's <see cref="CustomColumnRegistry" /> or returns
    ///     <c>null</c> when the DI container is not yet initialised.
    /// </summary>
    /// <returns>The custom column registry when available; otherwise <c>null</c>.</returns>
    public static CustomColumnRegistry? ResolveCustomColumnRegistry()
    {
        var container = ContainerLocator.Current;
        if (container is null)
        {
            return null;
        }

        return container.GetService<CustomColumnRegistry>();
    }

    /// <summary>
    ///     Resolves the application's <see cref="LocalizationService" /> or returns
    ///     <c>null</c> when the DI container is not yet initialised.
    /// </summary>
    /// <returns>The localization service when available; otherwise <c>null</c>.</returns>
    public static LocalizationService? ResolveLocalizationService()
    {
        var container = ContainerLocator.Current;
        if (container is null)
        {
            return null;
        }

        return container.GetService<LocalizationService>();
    }

    /// <summary>
    ///     Resolves the application's <see cref="ITextPromptService" /> or returns
    ///     <c>null</c> when the DI container is not yet initialised.
    /// </summary>
    /// <returns>The text prompt service when available; otherwise <c>null</c>.</returns>
    public static ITextPromptService? ResolvePromptService()
    {
        var container = ContainerLocator.Current;
        if (container is null)
        {
            return null;
        }

        return container.GetService<ITextPromptService>();
    }
}
