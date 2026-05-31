using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Pages;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end smoke tests that verify the headless test infrastructure boots
///     <see cref="Proxyfan.Client.Shell.Views.ShellWindow" /> correctly. If these fail,
///     no other test in this assembly will run reliably, so they exist primarily to give
///     a clear failure signal for environment problems (missing assembly attribute, broken
///     AppBuilder, container locator polluted by a previous test, etc.).
/// </summary>
public sealed class ShellWindowSmokeEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Show_FreshEnvironment_RendersTitleAndPrimaryPanels()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            await Assert.That(page.GetTitle()).IsEqualTo("Proxyfan");
            await Assert.That(page.Menu()).IsNotNull();
            await Assert.That(page.SourceList()).IsNotNull();
            await Assert.That(page.FilterTextBox()).IsNotNull();
            await Assert.That(page.TabList()).IsNotNull();
        });
    }
}
