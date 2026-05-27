using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Tests.Har;

/// <summary>
///     Tests for <see cref="Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType" /> branch coverage.
/// </summary>
public sealed class HarDocumentWriterTests
{
    /// <summary>
    ///     Verifies that an empty MIME type is not considered text-like.
    /// </summary>
    [Test]
    public async Task HasTextLikeMimeType_Empty_ReturnsFalse()
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType(string.Empty);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a null-equivalent MIME type is not considered text-like.
    /// </summary>
    [Test]
    public async Task HasTextLikeMimeType_Null_ReturnsFalse()
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType(null!);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that "text/plain" is text-like.
    /// </summary>
    [Test]
    [Arguments("text/plain")]
    [Arguments("text/html")]
    [Arguments("TEXT/CSS")]
    public async Task HasTextLikeMimeType_TextPrefix_ReturnsTrue(string mimeType)
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType(mimeType);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that JSON MIME types are text-like.
    /// </summary>
    [Test]
    [Arguments("application/json")]
    [Arguments("application/ld+json")]
    public async Task HasTextLikeMimeType_JsonType_ReturnsTrue(string mimeType)
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType(mimeType);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that XML MIME types are text-like.
    /// </summary>
    [Test]
    public async Task HasTextLikeMimeType_XmlType_ReturnsTrue()
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType("application/xml");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that JavaScript MIME types are text-like.
    /// </summary>
    [Test]
    public async Task HasTextLikeMimeType_JavascriptType_ReturnsTrue()
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType("application/javascript");

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that binary MIME types are not text-like.
    /// </summary>
    [Test]
    [Arguments("image/png")]
    [Arguments("application/octet-stream")]
    public async Task HasTextLikeMimeType_BinaryType_ReturnsFalse(string mimeType)
    {
        var result = Proxyfan.Domain.Session.Har.HarDocumentWriter.HasTextLikeMimeType(mimeType);

        await Assert.That(result).IsFalse();
    }
}
