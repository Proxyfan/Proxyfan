using System.Threading.Tasks;
using Proxyfan.Domain.Composer;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.Composer.Tests;

public sealed class ComposerRequestBuilderTests
{
    [Test]
    public async Task Build_DefaultBuilder_ReturnsGetWithEmptyUrl()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetUrl("https://example.com/");

        var request = builder.Build();

        await Assert.That(request.Method).IsEqualTo("GET");
        await Assert.That(request.Url).IsEqualTo("https://example.com/");
        await Assert.That(request.Headers.Count).IsEqualTo(0);
        await Assert.That(request.Body.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SetMethod_LowerCaseMethod_StoresUppercase()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetUrl("https://example.com/").SetMethod("post");

        var request = builder.Build();

        await Assert.That(request.Method).IsEqualTo("POST");
    }

    [Test]
    public async Task AddHeader_TwoCalls_PreservesInsertionOrder()
    {
        var builder = new ComposerRequestBuilder();
        builder
            .SetUrl("https://example.com/")
            .AddHeader("Accept", "application/json")
            .AddHeader("Authorization", "Bearer token");

        var request = builder.Build();

        await Assert.That(request.Headers.Count).IsEqualTo(2);
        await Assert.That(request.Headers[0].Name).IsEqualTo("Accept");
        await Assert.That(request.Headers[1].Name).IsEqualTo("Authorization");
    }

    [Test]
    public async Task SetBody_NonEmptyBody_StoresAllBytes()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetUrl("https://example.com/").SetBody([0x01, 0x02, 0x03, 0xFF]);

        var request = builder.Build();

        await Assert.That(request.Body.Count).IsEqualTo(4);
        await Assert.That(request.Body[0]).IsEqualTo((byte)0x01);
        await Assert.That(request.Body[3]).IsEqualTo((byte)0xFF);
    }

    [Test]
    public async Task SetBody_CalledTwice_ReplacesPreviousBody()
    {
        var builder = new ComposerRequestBuilder();
        builder.SetUrl("https://example.com/").SetBody([0x01, 0x02, 0x03]).SetBody([0xAA]);

        var request = builder.Build();

        await Assert.That(request.Body.Count).IsEqualTo(1);
        await Assert.That(request.Body[0]).IsEqualTo((byte)0xAA);
    }

    [Test]
    public async Task SetUrl_EmptyString_Throws()
    {
        var builder = new ComposerRequestBuilder();

        await Assert.That(() => builder.SetUrl("")).Throws<System.ArgumentException>();
    }

    [Test]
    public async Task SetMethod_WhitespaceMethod_Throws()
    {
        var builder = new ComposerRequestBuilder();

        await Assert.That(() => builder.SetMethod("  ")).Throws<System.ArgumentException>();
    }
}
