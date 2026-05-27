using Proxyfan.Client.Traffic.ViewModels;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="SourceGroupViewModel" />.
/// </summary>
public sealed class SourceGroupViewModelTests
{
    /// <summary>
    ///     Verifies that the constructor initializes <see cref="SourceGroupViewModel.Count" /> to zero.
    /// </summary>
    [Test]
    public async Task Constructor_WhenCreated_HasZeroCount()
    {
        var group = new SourceGroupViewModel("example.com", false);

        await Assert.That(group.Count).IsEqualTo(0);
        await Assert.That(group.Host).IsEqualTo("example.com");
        await Assert.That(group.IsAllGroup).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="SourceGroupViewModel.Increment" /> increases the count.
    /// </summary>
    [Test]
    public async Task Increment_WhenCalled_IncreasesCount()
    {
        var group = new SourceGroupViewModel("h", false);

        group.Increment();
        group.Increment();

        await Assert.That(group.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that <see cref="SourceGroupViewModel.Decrement" /> decreases the count.
    /// </summary>
    [Test]
    public async Task Decrement_WhenPositive_DecreasesCount()
    {
        var group = new SourceGroupViewModel("h", false);
        group.Increment();
        group.Increment();

        group.Decrement();

        await Assert.That(group.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="SourceGroupViewModel.Decrement" /> does not go below zero.
    /// </summary>
    [Test]
    public async Task Decrement_WhenZero_DoesNotGoBelowZero()
    {
        var group = new SourceGroupViewModel("h", false);

        group.Decrement();

        await Assert.That(group.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the IsAllGroup flag is set when requested.
    /// </summary>
    [Test]
    public async Task Constructor_IsAllGroupFlag_PreservesValue()
    {
        var allGroup = new SourceGroupViewModel("*", true);
        var hostGroup = new SourceGroupViewModel("a", false);

        await Assert.That(allGroup.IsAllGroup).IsTrue();
        await Assert.That(hostGroup.IsAllGroup).IsFalse();
    }
}
