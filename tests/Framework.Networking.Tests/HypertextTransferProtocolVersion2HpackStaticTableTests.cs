using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HpackStaticTable" /> using the RFC
///     7541 Appendix A canonical static table.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackStaticTableTests
{
    /// <summary>
    ///     The RFC 7541 Appendix A static table contains 61 entries.
    /// </summary>
    [Test]
    public async Task Count_StaticTable_ReturnsSixtyOne()
    {
        await Assert.That(HypertextTransferProtocolVersion2HpackStaticTable.Count).IsEqualTo(61);
    }

    /// <summary>
    ///     Static table entry 1 is <c>:authority</c> with an empty value.
    /// </summary>
    [Test]
    public async Task Get_IndexOne_ReturnsAuthorityPseudoHeader()
    {
        var entry = HypertextTransferProtocolVersion2HpackStaticTable.Get(1);

        await Assert.That(entry.Name).IsEqualTo(":authority");
        await Assert.That(entry.Value).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Static table entry 14 is <c>:status</c> with value <c>500</c>.
    /// </summary>
    [Test]
    public async Task Get_IndexFourteen_ReturnsStatusFiveHundred()
    {
        var entry = HypertextTransferProtocolVersion2HpackStaticTable.Get(14);

        await Assert.That(entry.Name).IsEqualTo(":status");
        await Assert.That(entry.Value).IsEqualTo("500");
    }

    /// <summary>
    ///     Static table entry 61 is <c>www-authenticate</c> with an empty value.
    /// </summary>
    [Test]
    public async Task Get_IndexSixtyOne_ReturnsWwwAuthenticate()
    {
        var entry = HypertextTransferProtocolVersion2HpackStaticTable.Get(61);

        await Assert.That(entry.Name).IsEqualTo("www-authenticate");
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackStaticTable.Find" /> reports an exact
    ///     match when both name and value are in the table.
    /// </summary>
    [Test]
    public async Task Find_ExactMatch_ReturnsIndexAndExactFlag()
    {
        var lookup = HypertextTransferProtocolVersion2HpackStaticTable.Find(":method", "GET");

        await Assert.That(lookup.Index).IsEqualTo(2);
        await Assert.That(lookup.IsExactMatch).IsTrue();
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackStaticTable.Find" /> reports the lowest
    ///     name-only match when the value does not match a known table entry.
    /// </summary>
    [Test]
    public async Task Find_NameOnlyMatch_ReturnsLowestIndexAndNotExactFlag()
    {
        var lookup = HypertextTransferProtocolVersion2HpackStaticTable.Find(":status", "418");

        await Assert.That(lookup.Index).IsEqualTo(8);
        await Assert.That(lookup.IsExactMatch).IsFalse();
    }

    /// <summary>
    ///     Names absent from the static table return zero with no exact-match flag.
    /// </summary>
    [Test]
    public async Task Find_UnknownName_ReturnsZero()
    {
        var lookup = HypertextTransferProtocolVersion2HpackStaticTable.Find("x-proxyfan-custom", "v");

        await Assert.That(lookup.Index).IsEqualTo(0);
        await Assert.That(lookup.IsExactMatch).IsFalse();
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackStaticTable.Snapshot" /> returns a list
    ///     of 61 distinct entries that mirrors the canonical table.
    /// </summary>
    [Test]
    public async Task Snapshot_AllEntries_ReturnsSixtyOneItems()
    {
        var snapshot = HypertextTransferProtocolVersion2HpackStaticTable.Snapshot();

        await Assert.That(snapshot.Count).IsEqualTo(61);
        await Assert.That(snapshot[0].Name).IsEqualTo(":authority");
    }
}
