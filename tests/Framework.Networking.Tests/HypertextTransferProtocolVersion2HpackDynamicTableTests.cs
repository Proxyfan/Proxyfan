using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HpackDynamicTable" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackDynamicTableTests
{
    /// <summary>
    ///     A freshly constructed dynamic table has the default 4096-byte budget.
    /// </summary>
    [Test]
    public async Task Constructor_Default_HasFourKilobyteBudget()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();

        await Assert.That(table.MaximumByteSize).IsEqualTo(4096);
        await Assert.That(table.CurrentByteSize).IsEqualTo(0);
        await Assert.That(table.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Insertions accumulate byte sizes and shift older entries toward the tail.
    /// </summary>
    [Test]
    public async Task Add_TwoEntries_NewestIsIndexOne()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        var first = new HypertextTransferProtocolVersion2HpackHeaderField("a", "1");
        var second = new HypertextTransferProtocolVersion2HpackHeaderField("b", "2");

        table.Add(first);
        table.Add(second);

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.Get(1).Name).IsEqualTo("b");
        await Assert.That(table.Get(2).Name).IsEqualTo("a");
        await Assert.That(table.CurrentByteSize).IsEqualTo(first.EntrySize + second.EntrySize);
    }

    /// <summary>
    ///     Inserting an entry that exceeds the maximum budget evicts older entries until the
    ///     entry fits, or drops the new entry entirely (RFC 7541 § 4.4).
    /// </summary>
    [Test]
    public async Task Add_EntryLargerThanBudget_DropsAndClearsTable()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable(40);
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));
        var oversized = new HypertextTransferProtocolVersion2HpackHeaderField("looooong", "valuuuue");

        table.Add(oversized);

        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(table.CurrentByteSize).IsEqualTo(0);
    }

    /// <summary>
    ///     When budget pressure requires evicting older entries to fit a new one, only the
    ///     necessary number of entries are evicted.
    /// </summary>
    [Test]
    public async Task Add_EvictsOldest_UntilNewEntryFits()
    {
        var smallField = new HypertextTransferProtocolVersion2HpackHeaderField("a", "1");
        var budget = (smallField.EntrySize * 2) + 1;
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable(budget);
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("b", "2"));

        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("c", "3"));

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.Get(1).Name).IsEqualTo("c");
        await Assert.That(table.Get(2).Name).IsEqualTo("b");
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackDynamicTable.Resize" /> with zero
    ///     evicts all entries.
    /// </summary>
    [Test]
    public async Task Resize_ToZero_ClearsAllEntries()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("b", "2"));

        table.Resize(0);

        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(table.CurrentByteSize).IsEqualTo(0);
        await Assert.That(table.MaximumByteSize).IsEqualTo(0);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackDynamicTable.Clear" /> empties the
    ///     table without changing the maximum size.
    /// </summary>
    [Test]
    public async Task Clear_AfterAdditions_LeavesEmptyTableWithSameBudget()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable(1024);
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));

        table.Clear();

        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(table.CurrentByteSize).IsEqualTo(0);
        await Assert.That(table.MaximumByteSize).IsEqualTo(1024);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackDynamicTable.Find" /> locates entries
    ///     by exact name + value match and reports the 1-based position.
    /// </summary>
    [Test]
    public async Task Find_ExactMatch_ReturnsPositionAndExactFlag()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("b", "2"));

        var lookup = table.Find("a", "1");

        await Assert.That(lookup.Index).IsEqualTo(2);
        await Assert.That(lookup.IsExactMatch).IsTrue();
    }

    /// <summary>
    ///     When only the name matches an entry, the lookup reports the position with no exact
    ///     match flag.
    /// </summary>
    [Test]
    public async Task Find_NameOnlyMatch_ReturnsLowestPositionAndNoExactFlag()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "2"));

        var lookup = table.Find("a", "999");

        await Assert.That(lookup.Index).IsEqualTo(1);
        await Assert.That(lookup.IsExactMatch).IsFalse();
    }

    /// <summary>
    ///     Names absent from the table return zero with no exact-match flag.
    /// </summary>
    [Test]
    public async Task Find_UnknownName_ReturnsZero()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        table.Add(new HypertextTransferProtocolVersion2HpackHeaderField("a", "1"));

        var lookup = table.Find("missing", "anything");

        await Assert.That(lookup.Index).IsEqualTo(0);
    }
}
