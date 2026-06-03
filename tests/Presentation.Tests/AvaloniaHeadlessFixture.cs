using Avalonia;
using Avalonia.Headless;
using System.Threading;

namespace Proxyfan.Presentation.Tests;

/// <summary>
///     Shared helper that initializes the Avalonia headless platform exactly once for the
///     test assembly.  Each test class that requires Avalonia calls
///     <see cref="EnsureInitialized" /> from its static constructor.
/// </summary>
internal static class AvaloniaHeadlessFixture
{
    private static int _initialized;

    /// <summary>
    ///     Ensures the Avalonia headless platform has been set up.  Subsequent calls are
    ///     no-ops; the setup is performed at most once per process.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 0)
        {
            AppBuilder.Configure<HeadlessTestApplication>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .SetupWithoutStarting();
        }
    }

    internal sealed class HeadlessTestApplication : Application
    {
    }
}
