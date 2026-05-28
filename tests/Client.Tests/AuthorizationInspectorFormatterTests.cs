using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="AuthorizationInspectorFormatter" />.
/// </summary>
public sealed class AuthorizationInspectorFormatterTests
{
    [Test]
    public async Task Format_NullRequest_ReturnsEmpty()
    {
        var result = AuthorizationInspectorFormatter.Format(null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_NoAuthorizationHeader_ReturnsEmpty()
    {
        var request = BuildRequest(HeaderCollection.Empty);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_BasicAuth_DecodesUsernameAndPassword()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:secret"));
        var headers = HeaderCollection.Empty.Add("Authorization", $"Basic {encoded}");
        var request = BuildRequest(headers);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Scheme: Basic")).IsTrue();
        await Assert.That(result.Contains("Username: alice")).IsTrue();
        await Assert.That(result.Contains("Password: secret")).IsTrue();
    }

    [Test]
    public async Task Format_BasicAuthMalformedBase64_ShowsErrorMessage()
    {
        var headers = HeaderCollection.Empty.Add("Authorization", "Basic ***not-base64***");
        var request = BuildRequest(headers);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Invalid Base64")).IsTrue();
    }

    [Test]
    public async Task Format_BearerJsonWebToken_DecodesHeaderAndPayload()
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"1234567890\",\"name\":\"Alice\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var signature = "abc";
        var token = $"{header}.{payload}.{signature}";
        var headers = HeaderCollection.Empty.Add("Authorization", $"Bearer {token}");
        var request = BuildRequest(headers);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Scheme: Bearer")).IsTrue();
        await Assert.That(result.Contains("JSON Web Token")).IsTrue();
        await Assert.That(result.Contains("\"sub\"")).IsTrue();
        await Assert.That(result.Contains("\"Alice\"")).IsTrue();
    }

    [Test]
    public async Task Format_BearerOpaqueToken_ShowsTokenOnly()
    {
        var headers = HeaderCollection.Empty.Add("Authorization", "Bearer xyz-token-without-segments");
        var request = BuildRequest(headers);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Scheme: Bearer")).IsTrue();
        await Assert.That(result.Contains("xyz-token-without-segments")).IsTrue();
        await Assert.That(result.Contains("JSON Web Token")).IsFalse();
    }

    [Test]
    public async Task Format_Digest_ParsesQuotedParameters()
    {
        var digest = "username=\"alice\", realm=\"example.com\", nonce=\"abc123\", uri=\"/api\", response=\"deadbeef\"";
        var headers = HeaderCollection.Empty.Add("Authorization", $"Digest {digest}");
        var request = BuildRequest(headers);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Scheme: Digest")).IsTrue();
        await Assert.That(result.Contains("username: alice")).IsTrue();
        await Assert.That(result.Contains("realm: example.com")).IsTrue();
        await Assert.That(result.Contains("nonce: abc123")).IsTrue();
        await Assert.That(result.Contains("response: deadbeef")).IsTrue();
    }

    [Test]
    public async Task Format_UnknownScheme_ShowsSchemeAndValue()
    {
        var headers = HeaderCollection.Empty.Add("Authorization", "Custom my-secret-value");
        var request = BuildRequest(headers);

        var result = AuthorizationInspectorFormatter.Format(request);

        await Assert.That(result.Contains("Scheme: Custom")).IsTrue();
        await Assert.That(result.Contains("Value: my-secret-value")).IsTrue();
    }

    private static HypertextTransferProtocolRequestData BuildRequest(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
