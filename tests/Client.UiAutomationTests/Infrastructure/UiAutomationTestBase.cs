using System;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests.Infrastructure;

/// <summary>
///     Base class for FlaUI-driven end-to-end tests. Enforces strict serialisation
///     across the entire assembly so only one Proxyfan UI is ever on screen at any
///     given moment — this is non-negotiable because UI automation grabs the
///     global mouse and keyboard cursor.
///     <para>
///         Tests inheriting from this class must perform all UI work inside a
///         <c>await using var app = ProxyfanApp.Launch();</c> block to guarantee
///         deterministic shutdown of the launched process even when assertions fail.
///     </para>
/// </summary>
[NotInParallel(nameof(UiAutomationTestBase))]
public abstract class UiAutomationTestBase : IDisposable
{
    /// <summary>
    ///     Per-test hard timeout. Aborts the test if FlaUI element discovery or a
    ///     blocked dispatcher wedges the process. Generous to allow for cold-start
    ///     JIT, but bounded so the suite can never hang the developer.
    /// </summary>
    private CancellationTokenSource? _testCts;

    /// <summary>
    ///     The active cancellation token for the current test.
    /// </summary>
    protected CancellationToken TestCancellation => _testCts?.Token ?? CancellationToken.None;

    /// <summary>
    ///     Initializes per-test state. Called automatically by TUnit before each test.
    /// </summary>
    [Before(Test)]
    public Task BeforeEachTestAsync()
    {
        _testCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Cleans up per-test state.
    /// </summary>
    [After(Test)]
    public Task AfterEachTestAsync()
    {
        _testCts?.Dispose();
        _testCts = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _testCts?.Dispose();
        _testCts = null;
        GC.SuppressFinalize(this);
    }
}
