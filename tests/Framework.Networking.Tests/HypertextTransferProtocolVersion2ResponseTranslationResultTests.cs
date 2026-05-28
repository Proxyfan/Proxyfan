using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2ResponseTranslationResult" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2ResponseTranslationResultTests
{
    /// <summary>
    ///     Constructor stores the provided headers and body.
    /// </summary>
    [Test]
    public async Task Constructor_HeadersAndBody_StoresValues()
    {
        var fields = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "204"),
        };
        var body = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

        var result = new HypertextTransferProtocolVersion2ResponseTranslationResult(fields, body);

        await Assert.That(result.Headers.Count).IsEqualTo(1);
        await Assert.That(result.Headers[0].Name).IsEqualTo(":status");
        await Assert.That(result.Body.Length).IsEqualTo(3);
    }
}
