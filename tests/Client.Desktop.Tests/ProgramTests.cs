using Avalonia;
using Avalonia.Headless;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Desktop.Tests;

/// <summary>
///     Tests for <see cref="Program" />.
/// </summary>
[SupportedOSPlatform("windows")]
[NotInParallel]
public sealed class ProgramTests
{
    static ProgramTests()
    {
        if (Application.Current is null)
        {
            Program.BuildAppBuilder()
                   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                   .SetupWithoutStarting();
        }
    }

    [Test]
    public async Task BuildAppBuilder_WhenBootstrappedHeadlessly_InitializesClientApplication()
    {
        await Assert.That(Application.Current).IsNotNull();
        await Assert.That(Application.Current).IsTypeOf<Proxyfan.Client.App>();
    }

    [Test]
    public async Task Run_WhenDesktopLifetimeStarts_ReceivesConfiguredAppBuilder()
    {
        var args = new[] { "--listen", "8080" };
        var starter = new CapturingDesktopLifetimeStarter();

        Program.Run(args, starter.Invoke);

        await Assert.That(starter.AppBuilder).IsNotNull();
        await Assert.That(starter.Args).IsSameReferenceAs(args);
    }

    [Test]
    public async Task Run_WhenLifetimeStarterIsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => Program.Run(Array.Empty<string>(), null!))
            .Throws<ArgumentNullException>();
    }

    private sealed class CapturingDesktopLifetimeStarter
    {
        public AppBuilder? AppBuilder { get; private set; }

        public string[]? Args { get; private set; }

        public void Invoke(AppBuilder appBuilder, string[] args)
        {
            AppBuilder = appBuilder;
            Args = args;
        }
    }
}
