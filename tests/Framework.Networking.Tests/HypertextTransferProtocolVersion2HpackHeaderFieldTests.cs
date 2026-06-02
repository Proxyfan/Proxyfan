using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HpackHeaderField" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackHeaderFieldTests
{
    /// <summary>
    ///     RFC 7541 § 4.1 — ASCII name and value sizes equal their character counts plus the
    ///     32-byte overhead.
    /// </summary>
    [Test]
    public async Task EntrySize_AsciiNameAndValue_EqualsCharacterCountsPlusOverhead()
    {
        var field = new HypertextTransferProtocolVersion2HpackHeaderField("custom-key", "custom-value");

        await Assert.That(field.EntrySize).IsEqualTo("custom-key".Length + "custom-value".Length + 32);
    }

    /// <summary>
    ///     RFC 7541 § 4.1 — non-ASCII values must be sized in UTF-8 octets so the dynamic-table
    ///     budget reflects the encoded byte cost, not the UTF-16 character count.
    /// </summary>
    [Test]
    public async Task EntrySize_NonAsciiValue_CountsUtf8Octets()
    {
        // "é" encodes to 2 UTF-8 octets; "💡" encodes to 4 UTF-8 octets (surrogate pair, 2 UTF-16 chars).
        var field = new HypertextTransferProtocolVersion2HpackHeaderField("x-emoji", "é💡");

        await Assert.That(field.EntrySize).IsEqualTo("x-emoji".Length + 6 + 32);
    }
}
