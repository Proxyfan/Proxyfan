using Avalonia.Headless;
using System;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests.Infrastructure;

/// <summary>
///     Base class for end-to-end UI tests. Provides the shared per-assembly
///     <see cref="HeadlessUnitTestSession" /> and a typed <see cref="RunOnUiThreadAsync(Func{Task})" />
///     helper that marshals work onto the headless UI thread.
///     <para>
///         The session is created once via <see cref="HeadlessUnitTestSession.GetOrStartForAssembly" />
///         (driven by the <see cref="AvaloniaTestApplicationAttribute" /> in
///         <c>AssemblyInfo.cs</c>) and disposed on assembly teardown. Per-test isolation
///         is achieved by tagging each subclass with <see cref="NotInParallelAttribute" />
///         so the single shared UI thread is never multiplexed across tests.
///     </para>
/// </summary>
[NotInParallel(nameof(EndToEndTestBase))]
public abstract class EndToEndTestBase
{
    /// <summary>
    ///     Per-test cancellation token tied to a 30-second hard timeout so a deadlocked
    ///     test cannot wedge the entire suite indefinitely.
    /// </summary>
    private CancellationTokenSource? _testCts;

    /// <summary>
    ///     Initializes per-test state. Called automatically by TUnit before each test.
    /// </summary>
    [Before(Test)]
    public void BeforeEachTest()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _testCts = cts;
    }

    /// <summary>
    ///     Cleans up per-test state.
    /// </summary>
    [After(Test)]
    public void AfterEachTest()
    {
        _testCts?.Dispose();
        _testCts = null;
    }

    /// <summary>
    ///     Dispatches <paramref name="action" /> onto the headless Avalonia UI thread and
    ///     awaits its completion. All XAML/control construction, property mutation, and
    ///     visual-tree traversal must happen inside this call.
    /// </summary>
    /// <param name="action">The asynchronous UI work to perform.</param>
    /// <returns>A task that completes when the dispatched work finishes.</returns>
    protected Task RunOnUiThreadAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(EndToEndTestBase).Assembly);
        var token = _testCts?.Token ?? CancellationToken.None;
        return session.Dispatch(action, token);
    }

    /// <summary>
    ///     Synchronous-result overload of <see cref="RunOnUiThreadAsync(Func{Task})" />.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned from the UI work.</typeparam>
    /// <param name="action">The asynchronous UI work to perform.</param>
    /// <returns>The value produced on the UI thread.</returns>
    protected Task<TResult> RunOnUiThreadAsync<TResult>(Func<Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(EndToEndTestBase).Assembly);
        var token = _testCts?.Token ?? CancellationToken.None;
        return session.Dispatch(action, token);
    }
}
