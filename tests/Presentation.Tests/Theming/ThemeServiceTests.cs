using System.Threading.Tasks;
using Proxyfan.Presentation.Theming;

namespace Proxyfan.Presentation.Tests.Theming;

/// <summary>
///     Tests for <see cref="ThemeService" />.
/// </summary>
public sealed class ThemeServiceTests
{
    /// <summary>
    ///     Verifies that the initial theme is retained.
    /// </summary>
    [Test]
    public async Task Constructor_InitialDark_SetsCurrentTheme()
    {
        var service = new ThemeService(AppTheme.Dark);

        await Assert.That(service.CurrentTheme).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that SwitchTheme updates the current theme.
    /// </summary>
    [Test]
    public async Task SwitchTheme_DifferentTheme_UpdatesCurrent()
    {
        var service = new ThemeService(AppTheme.Light);

        service.SwitchTheme(AppTheme.Dark);

        await Assert.That(service.CurrentTheme).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that SwitchTheme to the same theme does not fire events.
    /// </summary>
    [Test]
    public async Task SwitchTheme_SameTheme_DoesNotFireEvent()
    {
        var service = new ThemeService(AppTheme.Light);
        var fireCount = 0;
        service.ThemeChanged += (_, _) => fireCount++;

        service.SwitchTheme(AppTheme.Light);

        await Assert.That(fireCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that ThemeChanged fires with the new theme.
    /// </summary>
    [Test]
    public async Task SwitchTheme_DifferentTheme_RaisesThemeChanged()
    {
        var service = new ThemeService(AppTheme.System);
        var fired = AppTheme.System;
        service.ThemeChanged += (_, theme) => fired = theme;

        service.SwitchTheme(AppTheme.Dark);

        await Assert.That(fired).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that PropertyChanged fires with the CurrentTheme property name.
    /// </summary>
    [Test]
    public async Task SwitchTheme_DifferentTheme_RaisesPropertyChanged()
    {
        var service = new ThemeService(AppTheme.Light);
        var propertyName = string.Empty;
        service.PropertyChanged += (_, args) => propertyName = args.PropertyName ?? string.Empty;

        service.SwitchTheme(AppTheme.Dark);

        await Assert.That(propertyName).IsEqualTo("CurrentTheme");
    }
}
