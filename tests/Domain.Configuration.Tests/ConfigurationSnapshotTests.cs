using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Configuration.Tests;

/// <summary>
///     Tests for <see cref="ConfigurationSnapshot" />.
/// </summary>
public sealed class ConfigurationSnapshotTests
{
    /// <summary>
    ///     Verifies that the count reflects the number of stored values.
    /// </summary>
    [Test]
    public async Task Count_AfterConstruction_ReflectsValues()
    {
        var data = new Dictionary<string, string>
        {
            ["proxy.port"] = "8080",
            ["proxy.host"] = "localhost",
        };
        var snapshot = new ConfigurationSnapshot(data);

        await Assert.That(snapshot.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.Get" /> returns the stored value when present.
    /// </summary>
    [Test]
    public async Task Get_PresentKey_ReturnsStoredValue()
    {
        var data = new Dictionary<string, string> { ["proxy.port"] = "9090" };
        var snapshot = new ConfigurationSnapshot(data);

        var value = snapshot.Get("proxy.port", "8080");

        await Assert.That(value).IsEqualTo("9090");
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.Get" /> returns the default when absent.
    /// </summary>
    [Test]
    public async Task Get_AbsentKey_ReturnsDefault()
    {
        var snapshot = new ConfigurationSnapshot(new Dictionary<string, string>());

        var value = snapshot.Get("missing", "fallback");

        await Assert.That(value).IsEqualTo("fallback");
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.Get" /> is case-insensitive.
    /// </summary>
    [Test]
    public async Task Get_DifferentCase_ReturnsStoredValue()
    {
        var data = new Dictionary<string, string> { ["Proxy.Port"] = "9090" };
        var snapshot = new ConfigurationSnapshot(data);

        var value = snapshot.Get("PROXY.PORT", "8080");

        await Assert.That(value).IsEqualTo("9090");
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.GetInteger" /> parses valid integers.
    /// </summary>
    [Test]
    public async Task GetInteger_ValidValue_ReturnsParsed()
    {
        var data = new Dictionary<string, string> { ["proxy.port"] = "9000" };
        var snapshot = new ConfigurationSnapshot(data);

        var value = snapshot.GetInteger("proxy.port", 8080);

        await Assert.That(value).IsEqualTo(9000);
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.GetInteger" /> returns the default on parse failure.
    /// </summary>
    [Test]
    public async Task GetInteger_InvalidValue_ReturnsDefault()
    {
        var data = new Dictionary<string, string> { ["proxy.port"] = "notanumber" };
        var snapshot = new ConfigurationSnapshot(data);

        var value = snapshot.GetInteger("proxy.port", 8080);

        await Assert.That(value).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.GetInteger" /> returns the default on absent key.
    /// </summary>
    [Test]
    public async Task GetInteger_AbsentKey_ReturnsDefault()
    {
        var snapshot = new ConfigurationSnapshot(new Dictionary<string, string>());

        var value = snapshot.GetInteger("missing", 42);

        await Assert.That(value).IsEqualTo(42);
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.HasBoolean" /> parses valid booleans.
    /// </summary>
    /// <param name="raw">The raw stored value.</param>
    /// <param name="expected">The expected parsed value.</param>
    [Test]
    [Arguments("true", true)]
    [Arguments("True", true)]
    [Arguments("false", false)]
    public async Task HasBoolean_ValidValue_OutputsParsed(string raw, bool expected)
    {
        var data = new Dictionary<string, string> { ["enabled"] = raw };
        var snapshot = new ConfigurationSnapshot(data);

        var ok = snapshot.HasBoolean("enabled", out var value);

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.HasBoolean" /> returns false on parse failure.
    /// </summary>
    [Test]
    public async Task HasBoolean_InvalidValue_ReturnsFalse()
    {
        var data = new Dictionary<string, string> { ["enabled"] = "maybe" };
        var snapshot = new ConfigurationSnapshot(data);

        var ok = snapshot.HasBoolean("enabled", out _);

        await Assert.That(ok).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.HasBoolean" /> returns false on absent key.
    /// </summary>
    [Test]
    public async Task HasBoolean_AbsentKey_ReturnsFalse()
    {
        var snapshot = new ConfigurationSnapshot(new Dictionary<string, string>());

        var ok = snapshot.HasBoolean("missing", out _);

        await Assert.That(ok).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.HasKey" /> returns true when present.
    /// </summary>
    [Test]
    public async Task HasKey_Present_ReturnsTrue()
    {
        var data = new Dictionary<string, string> { ["proxy.port"] = "8080" };
        var snapshot = new ConfigurationSnapshot(data);

        await Assert.That(snapshot.HasKey("proxy.port")).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.HasKey" /> returns false when absent.
    /// </summary>
    [Test]
    public async Task HasKey_Absent_ReturnsFalse()
    {
        var snapshot = new ConfigurationSnapshot(new Dictionary<string, string>());

        await Assert.That(snapshot.HasKey("missing")).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="ConfigurationSnapshot.Enumerate" /> returns all stored pairs.
    /// </summary>
    [Test]
    public async Task Enumerate_PopulatedSnapshot_ReturnsAllPairs()
    {
        var data = new Dictionary<string, string>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
        };
        var snapshot = new ConfigurationSnapshot(data);

        var count = 0;
        foreach (var _ in snapshot.Enumerate())
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that the sequence returned by <see cref="ConfigurationSnapshot.Enumerate" /> is not
    ///     the mutable backing dictionary; callers must not be able to cast it to a mutable type.
    /// </summary>
    [Test]
    public async Task Enumerate_ReturnedSequence_IsNotMutableDictionary()
    {
        var data = new Dictionary<string, string> { ["x"] = "1" };
        var snapshot = new ConfigurationSnapshot(data);

        var result = snapshot.Enumerate();

        await Assert.That(result is Dictionary<string, string>).IsFalse();
    }
}
