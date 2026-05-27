using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ShellViewModel" />.
/// </summary>
public sealed class ShellViewModelTests
{
    private static ShellViewModel CreateViewModel(StubSystemProxy systemProxy, int port)
    {
        return ShellViewModelFactory.Create(systemProxy, port);
    }

    /// <summary>
    ///     Verifies that a new view model starts with system proxy disabled.
    /// </summary>
    [Test]
    public async Task IsSystemProxyEnabled_InitialState_IsFalse()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        await Assert.That(viewModel.IsSystemProxyEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that toggling with proxy disabled calls RegisterAsync with the configured port.
    /// </summary>
    [Test]
    public async Task ToggleSystemProxyCommand_WhenDisabled_RegistersProxy()
    {
        var systemProxy = new StubSystemProxy();
        var viewModel = CreateViewModel(systemProxy, 8888);

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

        await Assert.That(systemProxy.RegisteredPorts.Count).IsEqualTo(1);
        await Assert.That(systemProxy.RegisteredPorts[0]).IsEqualTo(8888);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that toggling when proxy is enabled calls UnregisterAsync.
    /// </summary>
    [Test]
    public async Task ToggleSystemProxyCommand_WhenEnabled_UnregistersProxy()
    {
        var systemProxy = new StubSystemProxy();
        var viewModel = CreateViewModel(systemProxy, 8080);

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

        await Assert.That(systemProxy.UnregisterCount).IsEqualTo(1);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that multiple toggles alternate between enabled and disabled states.
    /// </summary>
    [Test]
    public async Task ToggleSystemProxyCommand_MultipleToggles_AlternatesState()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsTrue();

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsFalse();

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that the TrafficList property is exposed and not null.
    /// </summary>
    [Test]
    public async Task TrafficList_AfterConstruction_IsExposed()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        await Assert.That(viewModel.TrafficList).IsNotNull();
        await Assert.That(viewModel.TrafficList.IsCapturing).IsTrue();
    }
}