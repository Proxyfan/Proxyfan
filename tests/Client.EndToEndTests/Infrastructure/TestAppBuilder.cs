using Avalonia;
using Avalonia.Headless;

namespace Proxyfan.Client.EndToEndTests.Infrastructure;

/// <summary>
///     Static builder consumed by <see cref="Avalonia.Headless.HeadlessUnitTestSession" />
///     to construct the per-assembly headless Avalonia runtime.
///     Referenced from <c>AssemblyInfo.cs</c> via <see cref="AvaloniaTestApplicationAttribute" />.
/// </summary>
public static class TestAppBuilder
{
    /// <summary>
    ///     Builds the headless Avalonia <see cref="AppBuilder" /> used by the test session.
    ///     <see cref="AvaloniaHeadlessPlatformOptions.UseHeadlessDrawing" /> is <c>true</c>
    ///     so that no GPU/Skia surface is required — tests run deterministically without a
    ///     real display. No real font is loaded because <c>UseHeadlessDrawing</c> bypasses
    ///     text rendering entirely.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder" />.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApp>()
                         .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
    }
}

