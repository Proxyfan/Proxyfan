using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolRequestComposer" />.
/// </summary>
public sealed class HypertextTransferProtocolRequestComposerTests
{
    /// <summary>
    ///     Verifies that the default constructor produces a GET / HTTP/1.1 composer with
    ///     no body and no URI.
    /// </summary>
    [Test]
    public async Task Constructor_Default_HasGetAndHttp11Defaults()
    {
        var composer = new HypertextTransferProtocolRequestComposer();

        await Assert.That(composer.Method).IsEqualTo("GET");
        await Assert.That(composer.Version).IsEqualTo("HTTP/1.1");
        await Assert.That(composer.Body.Length).IsEqualTo(0);
        await Assert.That(composer.RequestUri).IsNull();
    }

    /// <summary>
    ///     Verifies that the source-cloning constructor copies method, version, URI, headers,
    ///     and body from the source request.
    /// </summary>
    [Test]
    public async Task Constructor_FromSource_ClonesAllFields()
    {
        var source = BuildSourceRequest();

        var composer = new HypertextTransferProtocolRequestComposer(source);

        await Assert.That(composer.Method).IsEqualTo("POST");
        await Assert.That(composer.RequestUri).IsEqualTo(source.RequestUri);
        await Assert.That(composer.Version).IsEqualTo("HTTP/2");
        await Assert.That(composer.Body.Length).IsEqualTo(source.Body.Length);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestComposer.Build" /> throws
    ///     when no URI is set.
    /// </summary>
    [Test]
    public async Task Build_WithoutRequestUri_Throws()
    {
        var composer = new HypertextTransferProtocolRequestComposer();

        await Assert.That(composer.Build).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestComposer.Build" /> throws
    ///     when the method is blank.
    /// </summary>
    [Test]
    public async Task Build_WithBlankMethod_Throws()
    {
        var composer = new HypertextTransferProtocolRequestComposer
        {
            RequestUri = new Uri("https://example.com/"),
            Method = " ",
        };

        await Assert.That(composer.Build).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies that headers set via <see cref="HypertextTransferProtocolRequestComposer.SetHeader" />
    ///     are emitted in the built request.
    /// </summary>
    [Test]
    public async Task Build_AfterSetHeader_EmitsHeader()
    {
        var composer = new HypertextTransferProtocolRequestComposer
        {
            RequestUri = new Uri("https://example.com/"),
        };
        composer.SetHeader("X-Test", "value");

        var built = composer.Build();

        await Assert.That(built.Headers.Get("X-Test")).IsEqualTo("value");
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRequestComposer.HasRemoved" />
    ///     removes the header so it is absent from the built request.
    /// </summary>
    [Test]
    public async Task RemoveHeader_AfterSet_RemovesHeader()
    {
        var composer = new HypertextTransferProtocolRequestComposer
        {
            RequestUri = new Uri("https://example.com/"),
        };
        composer.SetHeader("X-Test", "value");

        var removed = composer.HasRemoved("X-Test");

        var built = composer.Build();
        await Assert.That(removed).IsTrue();
        await Assert.That(built.Headers.HasHeader("X-Test")).IsFalse();
    }

    /// <summary>
    ///     Verifies that removing an unknown header returns false.
    /// </summary>
    [Test]
    public async Task RemoveHeader_UnknownHeader_ReturnsFalse()
    {
        var composer = new HypertextTransferProtocolRequestComposer();

        var removed = composer.HasRemoved("X-Unknown");

        await Assert.That(removed).IsFalse();
    }

    /// <summary>
    ///     Verifies that the cloning constructor preserves all values for repeated headers
    ///     so the built request matches the captured one.
    /// </summary>
    [Test]
    public async Task Constructor_FromSourceWithRepeatedHeader_PreservesAllValues()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = HeaderCollection.Empty
                .Add("Forwarded", "for=192.0.2.1")
                .Add("Forwarded", "for=192.0.2.2"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var source = new HypertextTransferProtocolRequestData(parameters);

        var composer = new HypertextTransferProtocolRequestComposer(source);
        var built = composer.Build();

        var values = built.Headers.GetAll("Forwarded");
        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo("for=192.0.2.1");
        await Assert.That(values[1]).IsEqualTo("for=192.0.2.2");
    }

    /// <summary>
    ///     Verifies that the cloning constructor throws when source is null.
    /// </summary>
    [Test]
    public async Task Constructor_NullSource_Throws()
    {
        await Assert.That(() => new HypertextTransferProtocolRequestComposer(null!))
            .Throws<NullReferenceException>();
    }

    private static HypertextTransferProtocolRequestData BuildSourceRequest()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Encoding.UTF8.GetBytes("payload"),
            Headers = HeaderCollection.Empty.Add("Host", "example.com").Add("X-Custom", "1"),
            Method = "POST",
            RequestUri = new Uri("https://example.com/data"),
            Version = "HTTP/2",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
