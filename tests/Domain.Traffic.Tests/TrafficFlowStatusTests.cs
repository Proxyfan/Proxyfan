using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>Tests for <see cref="TrafficFlowStatus" />.</summary>
internal sealed class TrafficFlowStatusTests
{
    /// <summary>Verifies that the <see cref="TrafficFlowStatus.Pending" /> value exists.</summary>
    [Test]
    public async Task TrafficFlowStatus_HasPendingValue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Pending)).IsTrue();
    }

    /// <summary>Verifies that the <see cref="TrafficFlowStatus.Active" /> value exists.</summary>
    [Test]
    public async Task TrafficFlowStatus_HasActiveValue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Active)).IsTrue();
    }

    /// <summary>Verifies that the <see cref="TrafficFlowStatus.Completed" /> value exists.</summary>
    [Test]
    public async Task TrafficFlowStatus_HasCompletedValue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Completed)).IsTrue();
    }

    /// <summary>Verifies that the <see cref="TrafficFlowStatus.Failed" /> value exists.</summary>
    [Test]
    public async Task TrafficFlowStatus_HasFailedValue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Failed)).IsTrue();
    }
}
