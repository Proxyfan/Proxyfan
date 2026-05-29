using System.Buffers;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolMethodPrefixDetector" /> verifying that
///     all standard HTTP request method tokens are detected and non-HTTP byte sequences are
///     rejected.
/// </summary>
public sealed class HypertextTransferProtocolMethodPrefixDetectorTests
{
    /// <summary>
    ///     Every standard HTTP/1.1 method token followed by a space is detected.
    /// </summary>
    [Test]
    [Arguments("GET / HTTP/1.1\r\n")]
    [Arguments("POST /api HTTP/1.1\r\n")]
    [Arguments("PUT /resource HTTP/1.1\r\n")]
    [Arguments("DELETE /item HTTP/1.1\r\n")]
    [Arguments("HEAD / HTTP/1.1\r\n")]
    [Arguments("OPTIONS * HTTP/1.1\r\n")]
    [Arguments("PATCH /api HTTP/1.1\r\n")]
    [Arguments("TRACE /diag HTTP/1.1\r\n")]
    public async Task HasMethodPrefix_StandardMethodToken_ReturnsTrue(string line)
    {
        var bytes = Encoding.ASCII.GetBytes(line);
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = HypertextTransferProtocolMethodPrefixDetector.HasMethodPrefix(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Bytes that do not look like an HTTP method token are rejected.
    /// </summary>
    [Test]
    [Arguments("hello world")]
    [Arguments("\x16\x03\x01")]
    [Arguments("FOOBAR / HTTP/1.1\r\n")]
    [Arguments("CONNECT example.com:443 HTTP/1.1\r\n")]
    public async Task HasMethodPrefix_NonHttpBytes_ReturnsFalse(string line)
    {
        var bytes = Encoding.ASCII.GetBytes(line);
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = HypertextTransferProtocolMethodPrefixDetector.HasMethodPrefix(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     A partial method prefix that has not yet received the trailing space is rejected so
    ///     the dispatcher waits for more bytes.
    /// </summary>
    [Test]
    public async Task HasMethodPrefix_MethodWithoutTrailingSpace_ReturnsFalse()
    {
        var bytes = Encoding.ASCII.GetBytes("GE");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = HypertextTransferProtocolMethodPrefixDetector.HasMethodPrefix(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     An empty buffer is rejected.
    /// </summary>
    [Test]
    public async Task HasMethodPrefix_EmptyBuffer_ReturnsFalse()
    {
        var sequence = ReadOnlySequence<byte>.Empty;

        var result = HypertextTransferProtocolMethodPrefixDetector.HasMethodPrefix(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Lower-case method tokens are NOT detected (HTTP/1.1 methods are case-sensitive,
    ///     per RFC 7230 §3.1.1).
    /// </summary>
    [Test]
    public async Task HasMethodPrefix_LowerCaseMethod_ReturnsFalse()
    {
        var bytes = Encoding.ASCII.GetBytes("get / HTTP/1.1\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = HypertextTransferProtocolMethodPrefixDetector.HasMethodPrefix(sequence);

        await Assert.That(result).IsFalse();
    }
}
