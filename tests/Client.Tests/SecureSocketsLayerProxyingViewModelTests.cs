using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Certificates;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="SecureSocketsLayerProxyingViewModel" />.
/// </summary>
public sealed class SecureSocketsLayerProxyingViewModelTests
{
    /// <summary>
    ///     The constructor seeds <see cref="SecureSocketsLayerProxyingViewModel.IncludedPatterns" /> from the list state.
    /// </summary>
    [Test]
    public async Task Constructor_ListWithIncludedPattern_SeedsCollection()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("*.example.com");
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.IncludedPatterns).Contains("*.example.com");
    }

    /// <summary>
    ///     Setting <see cref="SecureSocketsLayerProxyingViewModel.IsEnabled" /> flips the underlying list state.
    /// </summary>
    [Test]
    public async Task IsEnabled_FlipsListState_WhenChanged()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: false);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);

        viewModel.IsEnabled = true;
        await Assert.That(list.IsEnabled).IsTrue();

        viewModel.IsEnabled = false;
        await Assert.That(list.IsEnabled).IsFalse();
    }

    /// <summary>
    ///     The AddIncludedPatternCommand pushes the pending pattern into the list.
    /// </summary>
    [Test]
    public async Task AddIncludedPatternCommand_ValidPattern_AddsToList()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.NewIncludedPattern = "api.example.com";

        viewModel.AddIncludedPatternCommand.Execute(null);

        await Assert.That(list.IncludedPatterns).Contains("api.example.com");
        await Assert.That(viewModel.NewIncludedPattern).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     The AddExcludedPatternCommand pushes the pending pattern into the list.
    /// </summary>
    [Test]
    public async Task AddExcludedPatternCommand_ValidPattern_AddsToList()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.NewExcludedPattern = "secret.example.com";

        viewModel.AddExcludedPatternCommand.Execute(null);

        await Assert.That(list.ExcludedPatterns).Contains("secret.example.com");
    }

    /// <summary>
    ///     Whitespace patterns are ignored by AddIncludedPatternCommand.
    /// </summary>
    [Test]
    public async Task AddIncludedPatternCommand_WhitespacePattern_DoesNothing()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.NewIncludedPattern = "   ";

        viewModel.AddIncludedPatternCommand.Execute(null);

        await Assert.That(list.IncludedPatterns.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     The RemoveIncludedPatternCommand removes the selected pattern from the list.
    /// </summary>
    [Test]
    public async Task RemoveIncludedPatternCommand_SelectedPattern_RemovesFromList()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("a.example.com");
        list.AddIncludedPattern("b.example.com");
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.SelectedIncludedPattern = "a.example.com";

        viewModel.RemoveIncludedPatternCommand.Execute(null);

        await Assert.That(list.IncludedPatterns).Contains("b.example.com");
        await Assert.That(list.IncludedPatterns.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     The RemoveExcludedPatternCommand removes the selected pattern from the list.
    /// </summary>
    [Test]
    public async Task RemoveExcludedPatternCommand_SelectedPattern_RemovesFromList()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddExcludedPattern("blocked.example.com");
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.SelectedExcludedPattern = "blocked.example.com";

        viewModel.RemoveExcludedPatternCommand.Execute(null);

        await Assert.That(list.ExcludedPatterns.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     External mutations to the list propagate into the view model collections.
    /// </summary>
    [Test]
    public async Task ExternalAdd_AfterConstruction_AppearsInIncludedPatterns()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);

        list.AddIncludedPattern("late.example.com");

        await Assert.That(viewModel.IncludedPatterns).Contains("late.example.com");
    }

    /// <summary>
    ///     Disposing the view model stops it from receiving further updates.
    /// </summary>
    [Test]
    public async Task Dispose_AfterChange_StopsReceivingUpdates()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.Dispose();

        list.AddIncludedPattern("ignored.example.com");

        await Assert.That(viewModel.IncludedPatterns.Any(p => p == "ignored.example.com")).IsFalse();
    }

    /// <summary>
    ///     The RemoveIncludedPatternCommand is a no-op when nothing is selected.
    /// </summary>
    [Test]
    public async Task RemoveIncludedPatternCommand_NoSelection_DoesNothing()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("kept.example.com");
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.SelectedIncludedPattern = null;

        viewModel.RemoveIncludedPatternCommand.Execute(null);

        await Assert.That(list.IncludedPatterns).Contains("kept.example.com");
    }

    /// <summary>
    ///     Whitespace patterns are ignored by AddExcludedPatternCommand.
    /// </summary>
    [Test]
    public async Task AddExcludedPatternCommand_WhitespacePattern_DoesNothing()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.NewExcludedPattern = "   ";

        viewModel.AddExcludedPatternCommand.Execute(null);

        await Assert.That(list.ExcludedPatterns.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     RemoveExcludedPatternCommand is a no-op when nothing is selected.
    /// </summary>
    [Test]
    public async Task RemoveExcludedPatternCommand_NoSelection_DoesNothing()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddExcludedPattern("kept.example.com");
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.SelectedExcludedPattern = null;

        viewModel.RemoveExcludedPatternCommand.Execute(null);

        await Assert.That(list.ExcludedPatterns).Contains("kept.example.com");
    }

    /// <summary>
    ///     When the underlying list is already enabled, setting <see cref="SecureSocketsLayerProxyingViewModel.IsEnabled" />
    ///     to true does not call Enable again (covers the !_list.IsEnabled false branch).
    /// </summary>
    [Test]
    public async Task IsEnabled_SetTrueWhenListAlreadyEnabled_NoOp()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);
        viewModel.IsEnabled = false;
        list.Enable();

        viewModel.IsEnabled = true;

        await Assert.That(list.IsEnabled).IsTrue();
    }

    /// <summary>
    ///     When the underlying list is already disabled, setting <see cref="SecureSocketsLayerProxyingViewModel.IsEnabled" />
    ///     to false does not call Disable again (covers the _list.IsEnabled false branch).
    /// </summary>
    [Test]
    public async Task IsEnabled_SetFalseWhenListAlreadyDisabled_NoOp()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: false);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);

        viewModel.IsEnabled = false;

        await Assert.That(list.IsEnabled).IsFalse();
    }

    /// <summary>
    ///     When external mutation propagates and the IsEnabled flags are already in sync,
    ///     the OnListChanged handler skips the IsEnabled assignment (covers the false
    ///     branch of the equality check).
    /// </summary>
    [Test]
    public async Task ExternalAdd_WhenIsEnabledMatches_DoesNotReassignIsEnabled()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var viewModel = new SecureSocketsLayerProxyingViewModel(list, InlineUserInterfaceScheduler.Instance);

        list.AddExcludedPattern("late.example.com");

        await Assert.That(viewModel.ExcludedPatterns).Contains("late.example.com");
        await Assert.That(viewModel.IsEnabled).IsTrue();
    }
}
