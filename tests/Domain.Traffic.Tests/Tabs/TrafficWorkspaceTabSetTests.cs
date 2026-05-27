using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Tabs;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Traffic.Tests.Tabs;

public sealed class TrafficWorkspaceTabSetTests
{
    [Test]
    public async Task Constructor_Default_CreatesSingleAllTrafficTab()
    {
        var set = new TrafficWorkspaceTabSet();
        await Assert.That(set.Count).IsEqualTo(1);
        await Assert.That(set.ActiveTab.Name).IsEqualTo(TrafficWorkspaceTabSet.DefaultFirstTabName);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_WithTab_UsesProvidedTab()
    {
        var tab = new TrafficWorkspaceTab("Custom");
        var set = new TrafficWorkspaceTabSet(tab);
        await Assert.That(set.Count).IsEqualTo(1);
        await Assert.That(set.ActiveTab.Name).IsEqualTo("Custom");
    }

    [Test]
    public async Task Add_NewTab_ActivatesItAndRaisesChanged()
    {
        var set = new TrafficWorkspaceTabSet();
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        var newTab = new TrafficWorkspaceTab("Second");
        set.Add(newTab);
        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(1);
        await Assert.That(set.ActiveTab.Name).IsEqualTo("Second");
        await Assert.That(changedCount).IsEqualTo(1);
    }

    [Test]
    public async Task Close_OnlyTab_DoesNotRemove()
    {
        var set = new TrafficWorkspaceTabSet();
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        set.Close(0);
        await Assert.That(set.Count).IsEqualTo(1);
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task Close_NonActiveTabBefore_DecrementsActiveIndex()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Add(new TrafficWorkspaceTab("C"));
        set.Activate(2);
        set.Close(0);
        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(1);
        await Assert.That(set.ActiveTab.Name).IsEqualTo("C");
    }

    [Test]
    public async Task Close_NonActiveTabAfter_KeepsActiveIndex()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Add(new TrafficWorkspaceTab("C"));
        set.Activate(0);
        set.Close(2);
        await Assert.That(set.Count).IsEqualTo(2);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Close_ActiveTabAtEnd_ActivatesPreviousTab()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Close(1);
        await Assert.That(set.Count).IsEqualTo(1);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Close_OutOfRange_NoOp()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Close(5);
        set.Close(-1);
        await Assert.That(set.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Activate_ValidIndex_UpdatesActiveTab()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        set.Activate(0);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(0);
        await Assert.That(changedCount).IsEqualTo(1);
    }

    [Test]
    public async Task Activate_OutOfRange_NoOp()
    {
        var set = new TrafficWorkspaceTabSet();
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        set.Activate(5);
        set.Activate(-1);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(0);
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task Activate_SameIndex_NoOp()
    {
        var set = new TrafficWorkspaceTabSet();
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        set.Activate(0);
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task Move_ActiveTab_FollowsToNewIndex()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Add(new TrafficWorkspaceTab("C"));
        set.Activate(0);
        set.Move(0, 2);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(2);
    }

    [Test]
    public async Task Move_ItemBeforeActive_DecrementsActiveIndex()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Add(new TrafficWorkspaceTab("C"));
        set.Activate(1);
        set.Move(0, 2);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Move_ItemAfterToBeforeActive_IncrementsActiveIndex()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Add(new TrafficWorkspaceTab("C"));
        set.Activate(1);
        set.Move(2, 0);
        await Assert.That(set.ActiveTabIndex).IsEqualTo(2);
    }

    [Test]
    public async Task Move_OutOfRange_NoOp()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        set.Move(5, 0);
        set.Move(0, 5);
        set.Move(-1, 0);
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task Move_SameIndex_NoOp()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        var changedCount = 0;
        set.Changed += _ => changedCount++;
        set.Move(1, 1);
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task Snapshot_AfterAdds_ReturnsTabsInOrder()
    {
        var set = new TrafficWorkspaceTabSet();
        set.Add(new TrafficWorkspaceTab("B"));
        set.Add(new TrafficWorkspaceTab("C"));
        var snapshot = set.Snapshot();
        await Assert.That(snapshot.Count).IsEqualTo(3);
        await Assert.That(snapshot[0].Name).IsEqualTo(TrafficWorkspaceTabSet.DefaultFirstTabName);
        await Assert.That(snapshot[1].Name).IsEqualTo("B");
        await Assert.That(snapshot[2].Name).IsEqualTo("C");
    }
}
