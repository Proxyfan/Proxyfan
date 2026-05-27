using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Configuration.Tests;

/// <summary>
///     Tests for <see cref="ConfigurationMerger" />.
/// </summary>
public sealed class ConfigurationMergerTests
{
    /// <summary>
    ///     Verifies that merging a single snapshot returns its contents.
    /// </summary>
    [Test]
    public async Task Merge_SingleSnapshot_ReturnsCopy()
    {
        var data = new Dictionary<string, string> { ["proxy.port"] = "8080" };
        var snapshot = new ConfigurationSnapshot(data);

        var merged = ConfigurationMerger.Merge(new[] { snapshot });

        await Assert.That(merged.Get("proxy.port", "missing")).IsEqualTo("8080");
    }

    /// <summary>
    ///     Verifies that later snapshots override earlier ones.
    /// </summary>
    [Test]
    public async Task Merge_LaterSnapshotOverridesEarlier_LaterWins()
    {
        var lowPriority = new ConfigurationSnapshot(new Dictionary<string, string> { ["proxy.port"] = "8080" });
        var highPriority = new ConfigurationSnapshot(new Dictionary<string, string> { ["proxy.port"] = "9090" });

        var merged = ConfigurationMerger.Merge(new[] { lowPriority, highPriority });

        await Assert.That(merged.Get("proxy.port", "missing")).IsEqualTo("9090");
    }

    /// <summary>
    ///     Verifies that keys present only in earlier snapshots survive.
    /// </summary>
    [Test]
    public async Task Merge_KeyOnlyInEarlier_Survives()
    {
        var lowPriority = new ConfigurationSnapshot(new Dictionary<string, string> { ["proxy.host"] = "localhost" });
        var highPriority = new ConfigurationSnapshot(new Dictionary<string, string> { ["proxy.port"] = "9090" });

        var merged = ConfigurationMerger.Merge(new[] { lowPriority, highPriority });

        await Assert.That(merged.Get("proxy.host", "missing")).IsEqualTo("localhost");
        await Assert.That(merged.Get("proxy.port", "missing")).IsEqualTo("9090");
    }

    /// <summary>
    ///     Verifies that an empty list returns an empty snapshot.
    /// </summary>
    [Test]
    public async Task Merge_EmptyList_ReturnsEmptySnapshot()
    {
        var merged = ConfigurationMerger.Merge(System.Array.Empty<ConfigurationSnapshot>());

        await Assert.That(merged.Count).IsEqualTo(0);
    }
}
