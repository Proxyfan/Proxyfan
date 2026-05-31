using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Proxyfan.Client.EndToEndTests.Infrastructure;

/// <summary>
///     Minimal Avalonia <see cref="Application" /> used by the headless end-to-end
///     UI test session. Loads the same FluentTheme the production
///     <see cref="Proxyfan.Client.App" /> uses so that XAML resources resolve
///     identically, but does NOT spin up the dependency-injection host, the
///     <c>ProxyServer</c>, the periodic update checker, plugin activation, or any
///     of the other side-effecting components production startup wires up.
///     Each test owns its own <see cref="ContainerLocator" /> container, so this
///     class deliberately does <b>not</b> touch <see cref="ContainerLocator" />.
/// </summary>
public sealed class TestApp : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Default;
    }
}
