using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ComposerResponseHeaderProjector" />.
/// </summary>
public sealed class ComposerResponseHeaderProjectorTests
{
    /// <summary>
    ///     Verifies that response-level headers are copied across.
    /// </summary>
    [Test]
    public async Task Project_ResponseHeader_Copied()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("X-Trace-Id", "abc");

        var headers = ComposerResponseHeaderProjector.Project(response);

        await Assert.That(headers.Get("X-Trace-Id")).IsEqualTo("abc");
    }

    /// <summary>
    ///     Verifies that content-level headers (e.g. Content-Type) are also copied across.
    /// </summary>
    [Test]
    public async Task Project_ContentTypeHeader_Copied()
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("body"));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content,
        };

        var headers = ComposerResponseHeaderProjector.Project(response);

        await Assert.That(headers.Get("Content-Type")).IsEqualTo("application/json");
    }

    /// <summary>
    ///     Verifies that responses without content still project successfully.
    /// </summary>
    [Test]
    public async Task Project_NullContent_Succeeds()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        response.Headers.TryAddWithoutValidation("X-Custom", "value");
        response.Content = null!;

        var headers = ComposerResponseHeaderProjector.Project(response);

        await Assert.That(headers.Get("X-Custom")).IsEqualTo("value");
    }
}
