using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="UserAgentParser" />.
/// </summary>
public sealed class UserAgentParserTests
{
    /// <summary>
    ///     Verifies that "curl/7.85.0" splits into product+version.
    /// </summary>
    [Test]
    public async Task Parse_SimpleProductSlashVersion_ParsesBoth()
    {
        var info = UserAgentParser.Parse("curl/7.85.0");

        await Assert.That(info.ProductName).IsEqualTo("curl");
        await Assert.That(info.ProductVersion).IsEqualTo("7.85.0");
    }

    /// <summary>
    ///     Verifies that the trailing tokens after the first space are ignored when computing
    ///     product/version.
    /// </summary>
    [Test]
    public async Task Parse_ProductWithComment_UsesFirstTokenOnly()
    {
        var info = UserAgentParser.Parse("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        await Assert.That(info.ProductName).IsEqualTo("Mozilla");
        await Assert.That(info.ProductVersion).IsEqualTo("5.0");
    }

    /// <summary>
    ///     Verifies that a single-token UA without version yields empty ProductVersion.
    /// </summary>
    [Test]
    public async Task Parse_ProductWithoutVersion_VersionIsEmpty()
    {
        var info = UserAgentParser.Parse("custom-bot");

        await Assert.That(info.ProductName).IsEqualTo("custom-bot");
        await Assert.That(info.ProductVersion).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that the raw value is preserved.
    /// </summary>
    [Test]
    public async Task Parse_RawValue_IsPreserved()
    {
        var raw = "Mozilla/5.0 (Windows NT 10.0)";
        var info = UserAgentParser.Parse(raw);

        await Assert.That(info.RawValue).IsEqualTo(raw);
    }

    /// <summary>
    ///     Verifies that blank values throw.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Parse_BlankValue_Throws(string? value)
    {
        await Assert.That(() => UserAgentParser.Parse(value!)).Throws<ArgumentException>();
    }
}
