using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for the <see cref="UpdateInfo" /> value object.
/// </summary>
public sealed class UpdateInfoTests
{
    /// <summary>
    ///     Verifies that the required and optional init properties round-trip the values
    ///     supplied at construction.
    /// </summary>
    [Test]
    public async Task InitProperties_AssignedAtConstruction_RoundTripValues()
    {
        var info = new UpdateInfo
        {
            DownloadUrl = "https://example.com/download/v2.zip",
            ReleaseNotes = "Notes",
            Version = "2.0.0",
        };

        await Assert.That(info.DownloadUrl).IsEqualTo("https://example.com/download/v2.zip");
        await Assert.That(info.ReleaseNotes).IsEqualTo("Notes");
        await Assert.That(info.Version).IsEqualTo("2.0.0");
    }

    /// <summary>
    ///     Verifies that <see cref="UpdateInfo.ReleaseNotes" /> defaults to <see langword="null" />
    ///     when omitted.
    /// </summary>
    [Test]
    public async Task ReleaseNotes_NotProvided_DefaultsToNull()
    {
        var info = new UpdateInfo
        {
            DownloadUrl = "https://example.com/download/v2.zip",
            Version = "2.0.0",
        };

        await Assert.That(info.ReleaseNotes).IsNull();
    }
}
