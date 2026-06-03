using Proxyfan.Client.Tools.ViewModels;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="PluginUpdateAvailabilityViewModel" />.
/// </summary>
public sealed class PluginUpdateAvailabilityViewModelTests
{
    /// <summary>
    ///     A compatible availability renders a clean version-transition label.
    /// </summary>
    [Test]
    public async Task VersionTransition_Compatible_OmitsIncompatSuffix()
    {
        var availability = BuildAvailability("1.0.0", "1.1.0", isCompatible: true);

        var viewModel = new PluginUpdateAvailabilityViewModel(availability);

        await Assert.That(viewModel.VersionTransition).IsEqualTo("1.0.0 → 1.1.0");
        await Assert.That(viewModel.IsCompatible).IsTrue();
    }

    /// <summary>
    ///     An incompatible availability appends the suffix.
    /// </summary>
    [Test]
    public async Task VersionTransition_Incompatible_AppendsSuffix()
    {
        var availability = BuildAvailability("1.0.0", "2.0.0", isCompatible: false);

        var viewModel = new PluginUpdateAvailabilityViewModel(availability);

        await Assert.That(viewModel.VersionTransition).IsEqualTo("1.0.0 → 2.0.0 (incompatible)");
        await Assert.That(viewModel.IsCompatible).IsFalse();
    }

    /// <summary>
    ///     All bound properties round-trip from the underlying availability.
    /// </summary>
    [Test]
    public async Task Construct_PopulatedAvailability_ExposesAllFields()
    {
        var availability = new PluginUpdateAvailabilitySnapshot(
            "com.x",
            "X plugin",
            "Author X",
            "1.0.0",
            "1.2.3",
            "https://example.com/x.zip",
            true);

        var viewModel = new PluginUpdateAvailabilityViewModel(availability);

        await Assert.That(viewModel.Identifier).IsEqualTo("com.x");
        await Assert.That(viewModel.Name).IsEqualTo("X plugin");
        await Assert.That(viewModel.Author).IsEqualTo("Author X");
        await Assert.That(viewModel.CurrentVersion).IsEqualTo("1.0.0");
        await Assert.That(viewModel.LatestVersion).IsEqualTo("1.2.3");
        await Assert.That(viewModel.DownloadUrl).IsEqualTo("https://example.com/x.zip");
    }

    private static PluginUpdateAvailabilitySnapshot BuildAvailability(string current, string latest, bool isCompatible)
    {
        var availability = new PluginUpdateAvailabilitySnapshot(
            "com.x",
            "X",
            "A",
            current,
            latest,
            "https://example.com/x.zip",
            isCompatible);
        return availability;
    }
}
