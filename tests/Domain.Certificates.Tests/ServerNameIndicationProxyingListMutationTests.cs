using Proxyfan.Domain.Certificates;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Certificates.Tests;

/// <summary>
///     Tests for the mutation/event surface added to <see cref="ServerNameIndicationProxyingList" />.
/// </summary>
public sealed class ServerNameIndicationProxyingListMutationTests
{
    /// <summary>
    ///     Enable flips state and raises the change event.
    /// </summary>
    [Test]
    public async Task Enable_DisabledList_FlipsStateAndRaisesEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: false);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.Enable();

        await Assert.That(list.IsEnabled).IsTrue();
        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Enable is a no-op when the list is already enabled.
    /// </summary>
    [Test]
    public async Task Enable_AlreadyEnabled_DoesNotRaiseEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.Enable();

        await Assert.That(changeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Disable flips state and raises the change event.
    /// </summary>
    [Test]
    public async Task Disable_EnabledList_FlipsStateAndRaisesEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.Disable();

        await Assert.That(list.IsEnabled).IsFalse();
        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Disable is a no-op when the list is already disabled.
    /// </summary>
    [Test]
    public async Task Disable_AlreadyDisabled_DoesNotRaiseEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: false);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.Disable();

        await Assert.That(changeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a new included pattern raises the change event.
    /// </summary>
    [Test]
    public async Task AddIncludedPattern_NewPattern_RaisesEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.AddIncludedPattern("example.com");

        await Assert.That(list.IncludedPatterns).Contains("example.com");
        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Adding an already-present included pattern does not raise the change event.
    /// </summary>
    [Test]
    public async Task AddIncludedPattern_Duplicate_DoesNotRaiseEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("example.com");
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.AddIncludedPattern("example.com");

        await Assert.That(changeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Adding a new excluded pattern raises the change event.
    /// </summary>
    [Test]
    public async Task AddExcludedPattern_NewPattern_RaisesEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.AddExcludedPattern("blocked.example.com");

        await Assert.That(list.ExcludedPatterns).Contains("blocked.example.com");
        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Removing an existing included pattern raises the change event.
    /// </summary>
    [Test]
    public async Task RemoveIncludedPattern_Existing_RaisesEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("example.com");
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.RemoveIncludedPattern("example.com");

        await Assert.That(list.IncludedPatterns.Count).IsEqualTo(0);
        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Removing a missing included pattern does not raise the event.
    /// </summary>
    [Test]
    public async Task RemoveIncludedPattern_Missing_NoEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.RemoveIncludedPattern("missing.example.com");

        await Assert.That(changeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing an excluded pattern raises the change event.
    /// </summary>
    [Test]
    public async Task RemoveExcludedPattern_Existing_RaisesEvent()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddExcludedPattern("blocked.example.com");
        var changeCount = 0;
        list.Changed += _ => changeCount++;

        list.RemoveExcludedPattern("blocked.example.com");

        await Assert.That(list.ExcludedPatterns.Count).IsEqualTo(0);
        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Whitespace patterns are silently ignored when removing.
    /// </summary>
    [Test]
    public async Task RemoveIncludedPattern_Whitespace_NoEffect()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddIncludedPattern("kept.example.com");

        list.RemoveIncludedPattern("   ");

        await Assert.That(list.IncludedPatterns).Contains("kept.example.com");
    }

    /// <summary>
    ///     Whitespace patterns are silently ignored when removing from the excluded list.
    /// </summary>
    [Test]
    public async Task RemoveExcludedPattern_Whitespace_NoEffect()
    {
        var list = new ServerNameIndicationProxyingList(isEnabled: true);
        list.AddExcludedPattern("kept.example.com");

        list.RemoveExcludedPattern("");

        await Assert.That(list.ExcludedPatterns).Contains("kept.example.com");
    }
}
