using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Framework.Platform;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Tests for <see cref="GitHubReleasesUpdateFeed" />. Uses a stubbed
///     <see cref="HttpMessageHandler" /> so no real network requests are made.
/// </summary>
public sealed class GitHubReleasesUpdateFeedTests
{
    /// <summary>
    ///     Verifies that the feed strips a leading 'v' from the tag name.
    /// </summary>
    [Test]
    public async Task Create_TagWithLeadingV_StripsPrefix()
    {
        var json = """{"tag_name":"v1.2.3","html_url":"https://github.com/x/y/releases/tag/v1.2.3","body":"notes"}""";
        using var handler = new StubHttpMessageHandler(json);
        using var client = new HttpClient(handler);
        var feed = GitHubReleasesUpdateFeed.Create(client, "x", "y");

        var info = await feed.Invoke(CancellationToken.None);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.Version).IsEqualTo("1.2.3");
        await Assert.That(info.DownloadUrl).IsEqualTo("https://github.com/x/y/releases/tag/v1.2.3");
        await Assert.That(info.ReleaseNotes).IsEqualTo("notes");
    }

    /// <summary>
    ///     Verifies that a missing tag_name yields null.
    /// </summary>
    [Test]
    public async Task Create_MissingTagName_ReturnsNull()
    {
        var json = """{"html_url":"https://x"}""";
        using var handler = new StubHttpMessageHandler(json);
        using var client = new HttpClient(handler);
        var feed = GitHubReleasesUpdateFeed.Create(client, "x", "y");

        var info = await feed.Invoke(CancellationToken.None);

        await Assert.That(info).IsNull();
    }

    /// <summary>
    ///     Verifies that a tag without 'v' prefix is preserved verbatim.
    /// </summary>
    [Test]
    public async Task Create_TagWithoutPrefix_PreservesVerbatim()
    {
        var json = """{"tag_name":"2.0.0","html_url":"https://example.com"}""";
        using var handler = new StubHttpMessageHandler(json);
        using var client = new HttpClient(handler);
        var feed = GitHubReleasesUpdateFeed.Create(client, "x", "y");

        var info = await feed.Invoke(CancellationToken.None);

        await Assert.That(info!.Version).IsEqualTo("2.0.0");
    }

    /// <summary>
    ///     A response body of literal JSON null causes <see cref="GitHubReleasesUpdateFeed.Create" />
    ///     to return null. Covers the null-response short-circuit.
    /// </summary>
    [Test]
    public async Task Create_NullResponseBody_ReturnsNull()
    {
        using var handler = new StubHttpMessageHandler("null");
        using var client = new HttpClient(handler);
        var feed = GitHubReleasesUpdateFeed.Create(client, "x", "y");

        var info = await feed.Invoke(CancellationToken.None);

        await Assert.That(info).IsNull();
    }

    /// <summary>
    ///     A release without an <c>html_url</c> field falls back to an empty download URL.
    /// </summary>
    [Test]
    public async Task Create_MissingHtmlUrl_DownloadUrlIsEmpty()
    {
        var json = """{"tag_name":"1.0.0"}""";
        using var handler = new StubHttpMessageHandler(json);
        using var client = new HttpClient(handler);
        var feed = GitHubReleasesUpdateFeed.Create(client, "x", "y");

        var info = await feed.Invoke(CancellationToken.None);

        await Assert.That(info).IsNotNull();
        await Assert.That(info!.DownloadUrl).IsEqualTo(string.Empty);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public StubHttpMessageHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(_responseBody, Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }
}
