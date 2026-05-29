using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginManifestParseResults" />.
/// </summary>
public sealed class PluginManifestParseResultsTests
{
    /// <summary>
    ///     Verifies that <see cref="PluginManifestParseResults.Success" /> produces a success result.
    /// </summary>
    [Test]
    public async Task Success_GivenManifest_ProducesIsSuccessTrue()
    {
        var metadata = new PluginMetadata("id", "n", "v", "a", "d", "1.0");
        var manifest = new PluginManifest(metadata, "x.dll", "X");

        var result = PluginManifestParseResults.Success(manifest);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Manifest).IsSameReferenceAs(manifest);
        await Assert.That(result.ErrorMessage).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="PluginManifestParseResults.Failure" /> produces a failure result.
    /// </summary>
    [Test]
    public async Task Failure_GivenError_ProducesIsSuccessFalse()
    {
        var result = PluginManifestParseResults.Failure("boom");

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Manifest).IsNull();
        await Assert.That(result.ErrorMessage).IsEqualTo("boom");
    }
}
