using System.Threading.Tasks;
using Proxyfan.Domain.Composer;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Composer.Tests;

public sealed class CurlExporterTests
{
    [Test]
    public async Task ToCurl_DefaultGet_OmitsExplicitMethod()
    {
        var request = Build("GET", "https://example.com/");

        var curl = CurlExporter.ToCurl(request);

        await Assert.That(curl).IsEqualTo("curl 'https://example.com/'");
    }

    [Test]
    public async Task ToCurl_PostMethod_IncludesXFlag()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetMethod("POST").SetUrl("https://example.com/");
        var request = builder.Build();

        var curl = CurlExporter.ToCurl(request);

        await Assert.That(curl).IsEqualTo("curl -X POST 'https://example.com/'");
    }

    [Test]
    public async Task ToCurl_WithHeaders_EmitsHFlagPerHeader()
    {
        var builder = new ComposerRequestBuilder();
        builder
            .SetMethod("GET")
            .SetUrl("https://example.com/")
            .AddHeader("Accept", "application/json")
            .AddHeader("Authorization", "Bearer abc");
        var request = builder.Build();

        var curl = CurlExporter.ToCurl(request);

        await Assert.That(curl).IsEqualTo(
            "curl -H 'Accept: application/json' -H 'Authorization: Bearer abc' 'https://example.com/'");
    }

    [Test]
    public async Task ToCurl_WithUtf8Body_EmitsDataBinaryFlag()
    {
        var builder = new ComposerRequestBuilder();
        builder
            .SetMethod("POST")
            .SetUrl("https://example.com/api")
            .AddHeader("Content-Type", "application/json")
            .SetBody(System.Text.Encoding.UTF8.GetBytes("{\"x\":1}"));
        var request = builder.Build();

        var curl = CurlExporter.ToCurl(request);

        await Assert.That(curl).IsEqualTo(
            "curl -X POST -H 'Content-Type: application/json' --data-binary '{\"x\":1}' 'https://example.com/api'");
    }

    [Test]
    public async Task ToCurl_WithSingleQuoteInValue_EscapesQuoteWithPosixDance()
    {
        var builder = new ComposerRequestBuilder();
        builder
            .SetMethod("GET")
            .SetUrl("https://example.com/")
            .AddHeader("X-Phrase", "it's fine");
        var request = builder.Build();

        var curl = CurlExporter.ToCurl(request);

        await Assert.That(curl).IsEqualTo("curl -H 'X-Phrase: it'\\''s fine' 'https://example.com/'");
    }

    [Test]
    public async Task ToCurl_WithBinaryBody_EmitsPlaceholder()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetMethod("POST").SetUrl("https://example.com/").SetBody([0xFF, 0xFE, 0x00, 0x01]);
        var request = builder.Build();

        var curl = CurlExporter.ToCurl(request);

        await Assert.That(curl).Contains("--data-binary");
        await Assert.That(curl).Contains("@<binary 4 bytes>");
    }

    private static ComposerRequest Build(string method, string url)
    {
        var builder = new ComposerRequestBuilder();
        builder.SetMethod(method).SetUrl(url);
        return builder.Build();
    }
}
