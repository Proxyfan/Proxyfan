using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic.Columns;

namespace Proxyfan.Domain.Traffic.Tests.Columns;

/// <summary>
///     Tests for <see cref="CustomColumnRegistry" />.
/// </summary>
public sealed class CustomColumnRegistryTests
{
    /// <summary>
    ///     Verifies a new registry has zero columns.
    /// </summary>
    [Test]
    public async Task Constructor_Default_IsEmpty()
    {
        var registry = new CustomColumnRegistry();

        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Add appends a column and raises the Changed event.
    /// </summary>
    [Test]
    public async Task Add_NewColumn_IncrementsCountAndRaisesChanged()
    {
        var registry = new CustomColumnRegistry();
        var changed = false;
        registry.Changed += _ => changed = true;

        registry.Add(BuildColumn("X-Test"));

        await Assert.That(registry.Count).IsEqualTo(1);
        await Assert.That(changed).IsTrue();
    }

    /// <summary>
    ///     Verifies Add throws when a duplicate id is added.
    /// </summary>
    [Test]
    public async Task Add_DuplicateId_Throws()
    {
        var registry = new CustomColumnRegistry();
        var column = BuildColumn("X-Test");
        registry.Add(column);

        await Assert.That(() => registry.Add(column)).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies Remove deletes a present column.
    /// </summary>
    [Test]
    public async Task Remove_PresentId_RemovesColumn()
    {
        var registry = new CustomColumnRegistry();
        var column = BuildColumn("X-Test");
        registry.Add(column);

        registry.Remove(column.Id);

        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Remove on an absent id is silently ignored.
    /// </summary>
    [Test]
    public async Task Remove_AbsentId_IsNoop()
    {
        var registry = new CustomColumnRegistry();

        registry.Remove(Guid.NewGuid());

        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Update replaces a column and raises Changed.
    /// </summary>
    [Test]
    public async Task Update_PresentId_ReplacesAndRaisesChanged()
    {
        var registry = new CustomColumnRegistry();
        var original = BuildColumn("X-Test");
        registry.Add(original);
        var updated = new CustomColumnDefinition
        {
            DisplayName = "Renamed",
            HeaderKey = "X-Test",
            Id = original.Id,
            Source = original.Source,
        };
        var changed = false;
        registry.Changed += _ => changed = true;

        registry.Update(updated);

        var snapshot = registry.Snapshot();
        await Assert.That(snapshot[0].DisplayName).IsEqualTo("Renamed");
        await Assert.That(changed).IsTrue();
    }

    /// <summary>
    ///     Verifies Update throws when the id is unknown.
    /// </summary>
    [Test]
    public async Task Update_UnknownId_Throws()
    {
        var registry = new CustomColumnRegistry();
        var unknown = BuildColumn("X-Test");

        await Assert.That(() => registry.Update(unknown)).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies Clear empties the registry and raises Changed.
    /// </summary>
    [Test]
    public async Task Clear_NonEmpty_RemovesAllAndRaisesChanged()
    {
        var registry = new CustomColumnRegistry();
        registry.Add(BuildColumn("A"));
        registry.Add(BuildColumn("B"));
        var changed = false;
        registry.Changed += _ => changed = true;

        registry.Clear();

        await Assert.That(registry.Count).IsEqualTo(0);
        await Assert.That(changed).IsTrue();
    }

    /// <summary>
    ///     Verifies Clear on an empty registry does not raise Changed.
    /// </summary>
    [Test]
    public async Task Clear_Empty_DoesNotRaiseChanged()
    {
        var registry = new CustomColumnRegistry();
        var changed = false;
        registry.Changed += _ => changed = true;

        registry.Clear();

        await Assert.That(changed).IsFalse();
    }

    /// <summary>
    ///     Verifies Snapshot returns columns in insertion order.
    /// </summary>
    [Test]
    public async Task Snapshot_AfterAdds_ReturnsInsertionOrder()
    {
        var registry = new CustomColumnRegistry();
        var first = BuildColumn("A");
        var second = BuildColumn("B");
        registry.Add(first);
        registry.Add(second);

        var snapshot = registry.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(2);
        await Assert.That(snapshot[0].DisplayName).IsEqualTo("A");
        await Assert.That(snapshot[1].DisplayName).IsEqualTo("B");
    }

    /// <summary>
    ///     Verifies that Remove with no Changed subscriber exercises the null-conditional
    ///     branch of the Changed event invocation.
    /// </summary>
    [Test]
    public async Task Remove_PresentIdWithoutSubscriber_DoesNotThrow()
    {
        var registry = new CustomColumnRegistry();
        var column = BuildColumn("X-Test");
        registry.Add(column);

        registry.Remove(column.Id);

        await Assert.That(registry.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that Remove walks past non-matching entries before removing the matching
    ///     one, exercising the false branch of the inner id comparison.
    /// </summary>
    [Test]
    public async Task Remove_PresentIdAfterOthers_RemovesOnlyMatching()
    {
        var registry = new CustomColumnRegistry();
        var first = BuildColumn("A");
        var target = BuildColumn("B");
        var third = BuildColumn("C");
        registry.Add(first);
        registry.Add(target);
        registry.Add(third);

        registry.Remove(target.Id);

        var snapshot = registry.Snapshot();
        await Assert.That(snapshot.Count).IsEqualTo(2);
        await Assert.That(snapshot[0].Id).IsEqualTo(first.Id);
        await Assert.That(snapshot[1].Id).IsEqualTo(third.Id);
    }

    /// <summary>
    ///     Verifies that Update with no Changed subscriber exercises the null-conditional
    ///     branch of the Changed event invocation.
    /// </summary>
    [Test]
    public async Task Update_PresentIdWithoutSubscriber_DoesNotThrow()
    {
        var registry = new CustomColumnRegistry();
        var original = BuildColumn("X-Test");
        registry.Add(original);
        var updated = new CustomColumnDefinition
        {
            DisplayName = "Renamed",
            HeaderKey = "X-Test",
            Id = original.Id,
            Source = original.Source,
        };

        registry.Update(updated);

        var snapshot = registry.Snapshot();
        await Assert.That(snapshot[0].DisplayName).IsEqualTo("Renamed");
    }

    /// <summary>
    ///     Verifies that Update walks past non-matching entries before replacing the matching
    ///     one, exercising the false branch of the inner id comparison.
    /// </summary>
    [Test]
    public async Task Update_PresentIdAfterOthers_ReplacesOnlyMatching()
    {
        var registry = new CustomColumnRegistry();
        var first = BuildColumn("A");
        var target = BuildColumn("B");
        registry.Add(first);
        registry.Add(target);
        var renamed = new CustomColumnDefinition
        {
            DisplayName = "Renamed",
            HeaderKey = "B",
            Id = target.Id,
            Source = target.Source,
        };

        registry.Update(renamed);

        var snapshot = registry.Snapshot();
        await Assert.That(snapshot[0].DisplayName).IsEqualTo("A");
        await Assert.That(snapshot[1].DisplayName).IsEqualTo("Renamed");
    }

    /// <summary>
    ///     Verifies that Clear with no Changed subscriber exercises the null-conditional
    ///     branch of the Changed event invocation.
    /// </summary>
    [Test]
    public async Task Clear_NonEmptyWithoutSubscriber_DoesNotThrow()
    {
        var registry = new CustomColumnRegistry();
        registry.Add(BuildColumn("A"));
        registry.Add(BuildColumn("B"));

        registry.Clear();

        await Assert.That(registry.Count).IsEqualTo(0);
    }

    private static CustomColumnDefinition BuildColumn(string name)
    {
        return new CustomColumnDefinition
        {
            DisplayName = name,
            HeaderKey = name,
            Id = Guid.NewGuid(),
            Source = CustomColumnSource.Request,
        };
    }
}
