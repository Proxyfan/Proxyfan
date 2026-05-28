using System.Threading.Tasks;
using Proxyfan.Presentation.Theming;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Presentation.Tests;

/// <summary>
///     Tests for <see cref="AppThemeParser" />.
/// </summary>
public sealed class AppThemeParserTests
{
    [Test]
    [Arguments("Light", AppTheme.Light)]
    [Arguments("LIGHT", AppTheme.Light)]
    [Arguments("light", AppTheme.Light)]
    [Arguments("Dark", AppTheme.Dark)]
    [Arguments("DARK", AppTheme.Dark)]
    [Arguments("dark", AppTheme.Dark)]
    [Arguments("System", AppTheme.System)]
    [Arguments("Unknown", AppTheme.System)]
    [Arguments("", AppTheme.System)]
    public async Task Parse_KnownAndUnknownValues_ResolvesToExpectedTheme(string input, AppTheme expected)
    {
        var theme = AppThemeParser.Parse(input);

        await Assert.That(theme).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_Null_FallsBackToSystem()
    {
        var theme = AppThemeParser.Parse(null);

        await Assert.That(theme).IsEqualTo(AppTheme.System);
    }
}
