using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="TrafficFlowStatus" />.
/// </summary>
public sealed class TrafficFlowStatusTests
{
    /// <summary>
    ///     Verifies that <see cref="TrafficFlowStatus.Aborted" /> is a defined enum value.
    /// </summary>
    [Test]
    public async Task IsDefined_WithAbortedValue_IsTrue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Aborted)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlowStatus.Active" /> is a defined enum value.
    /// </summary>
    [Test]
    public async Task IsDefined_WithActiveValue_IsTrue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Active)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlowStatus.Complete" /> is a defined enum value.
    /// </summary>
    [Test]
    public async Task IsDefined_WithCompleteValue_IsTrue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Complete)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlowStatus.Failed" /> is a defined enum value.
    /// </summary>
    [Test]
    public async Task IsDefined_WithFailedValue_IsTrue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Failed)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficFlowStatus.Pending" /> is a defined enum value.
    /// </summary>
    [Test]
    public async Task IsDefined_WithPendingValue_IsTrue()
    {
        await Assert.That(Enum.IsDefined(TrafficFlowStatus.Pending)).IsTrue();
    }
}