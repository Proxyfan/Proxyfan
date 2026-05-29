using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginCandidates" />.
/// </summary>
public sealed class PluginCandidatesTests
{
    /// <summary>
    ///     Verifies that <see cref="PluginCandidates.Valid" /> builds a valid candidate.
    /// </summary>
    [Test]
    public async Task Valid_GivenManifest_BuildsValidCandidate()
    {
        var metadata = new PluginMetadata("id", "n", "v", "a", "d", "1.0");
        var manifest = new PluginManifest(metadata, "x.dll", "X");

        var candidate = PluginCandidates.Valid(@"C:\plugins\foo", manifest);

        await Assert.That(candidate.IsValid).IsTrue();
        await Assert.That(candidate.Manifest).IsSameReferenceAs(manifest);
        await Assert.That(candidate.ErrorMessage).IsNull();
        await Assert.That(candidate.DirectoryPath).IsEqualTo(@"C:\plugins\foo");
    }

    /// <summary>
    ///     Verifies that <see cref="PluginCandidates.Invalid" /> builds an invalid candidate.
    /// </summary>
    [Test]
    public async Task Invalid_GivenError_BuildsInvalidCandidate()
    {
        var candidate = PluginCandidates.Invalid(@"C:\plugins\bar", "boom");

        await Assert.That(candidate.IsValid).IsFalse();
        await Assert.That(candidate.Manifest).IsNull();
        await Assert.That(candidate.ErrorMessage).IsEqualTo("boom");
        await Assert.That(candidate.DirectoryPath).IsEqualTo(@"C:\plugins\bar");
    }
}
