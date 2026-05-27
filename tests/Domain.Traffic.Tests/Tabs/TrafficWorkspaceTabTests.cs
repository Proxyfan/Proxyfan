using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Tabs;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Traffic.Tests.Tabs;

public sealed class TrafficWorkspaceTabTests
{
    [Test]
    public async Task Constructor_EmptyName_Throws()
    {
        await Assert
            .That(() => new TrafficWorkspaceTab(string.Empty))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_WhitespaceName_Throws()
    {
        await Assert
            .That(() => new TrafficWorkspaceTab("   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_ValidName_InitializesProperties()
    {
        var tab = new TrafficWorkspaceTab("My Tab");
        await Assert.That(tab.Name).IsEqualTo("My Tab");
        await Assert.That(tab.FilterQuery).IsEqualTo(string.Empty);
        await Assert.That(tab.SelectedFlowId).IsNull();
        await Assert.That(tab.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task SetName_NewValue_RaisesChangedAndUpdatesName()
    {
        var tab = new TrafficWorkspaceTab("Old");
        var changedCount = 0;
        tab.Changed += _ => changedCount++;
        tab.SetName("New");
        await Assert.That(tab.Name).IsEqualTo("New");
        await Assert.That(changedCount).IsEqualTo(1);
    }

    [Test]
    public async Task SetName_SameValue_DoesNotRaiseChanged()
    {
        var tab = new TrafficWorkspaceTab("Same");
        var changedCount = 0;
        tab.Changed += _ => changedCount++;
        tab.SetName("Same");
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task SetName_EmptyValue_Throws()
    {
        var tab = new TrafficWorkspaceTab("Original");
        await Assert
            .That(() => tab.SetName(string.Empty))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task SetFilterQuery_NewValue_RaisesChangedAndUpdatesQuery()
    {
        var tab = new TrafficWorkspaceTab("Tab");
        var changedCount = 0;
        tab.Changed += _ => changedCount++;
        tab.SetFilterQuery("host:example.com");
        await Assert.That(tab.FilterQuery).IsEqualTo("host:example.com");
        await Assert.That(changedCount).IsEqualTo(1);
    }

    [Test]
    public async Task SetFilterQuery_SameValue_DoesNotRaiseChanged()
    {
        var tab = new TrafficWorkspaceTab("Tab");
        tab.SetFilterQuery("foo");
        var changedCount = 0;
        tab.Changed += _ => changedCount++;
        tab.SetFilterQuery("foo");
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task SetFilterQuery_Null_NormalisesToEmpty()
    {
        var tab = new TrafficWorkspaceTab("Tab");
        tab.SetFilterQuery("filter");
        tab.SetFilterQuery(null);
        await Assert.That(tab.FilterQuery).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SetSelectedFlowId_NewValue_RaisesChangedAndUpdatesSelection()
    {
        var tab = new TrafficWorkspaceTab("Tab");
        var changedCount = 0;
        tab.Changed += _ => changedCount++;
        var flowId = Guid.NewGuid();
        tab.SetSelectedFlowId(flowId);
        await Assert.That(tab.SelectedFlowId).IsEqualTo(flowId);
        await Assert.That(changedCount).IsEqualTo(1);
    }

    [Test]
    public async Task SetSelectedFlowId_SameValue_DoesNotRaiseChanged()
    {
        var tab = new TrafficWorkspaceTab("Tab");
        var flowId = Guid.NewGuid();
        tab.SetSelectedFlowId(flowId);
        var changedCount = 0;
        tab.Changed += _ => changedCount++;
        tab.SetSelectedFlowId(flowId);
        await Assert.That(changedCount).IsEqualTo(0);
    }

    [Test]
    public async Task SetSelectedFlowId_Null_ClearsSelection()
    {
        var tab = new TrafficWorkspaceTab("Tab");
        var flowId = Guid.NewGuid();
        tab.SetSelectedFlowId(flowId);
        tab.SetSelectedFlowId(null);
        await Assert.That(tab.SelectedFlowId).IsNull();
    }
}
