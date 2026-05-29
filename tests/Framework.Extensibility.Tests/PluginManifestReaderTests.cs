using System.Threading.Tasks;

namespace Proxyfan.Framework.Extensibility.Tests;

/// <summary>
///     Tests for <see cref="PluginManifestReader" />.
/// </summary>
public sealed class PluginManifestReaderTests
{
    /// <summary>
    ///     Verifies that a fully-populated manifest parses successfully.
    /// </summary>
    [Test]
    public async Task Parse_ValidManifest_ReturnsSuccess()
    {
        const string text = """
            id=my.plugin
            name=My Plugin
            version=1.2.3
            author=Acme
            description=Demo
            apiVersion=1.0
            assembly=My.Plugin.dll
            entryType=My.Plugin.EntryPoint
            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Manifest).IsNotNull();
        await Assert.That(result.Manifest!.Metadata.Id).IsEqualTo("my.plugin");
        await Assert.That(result.Manifest.Metadata.Name).IsEqualTo("My Plugin");
        await Assert.That(result.Manifest.Metadata.Version).IsEqualTo("1.2.3");
        await Assert.That(result.Manifest.Metadata.ApiVersion).IsEqualTo("1.0");
        await Assert.That(result.Manifest.AssemblyFileName).IsEqualTo("My.Plugin.dll");
        await Assert.That(result.Manifest.EntryTypeName).IsEqualTo("My.Plugin.EntryPoint");
    }

    /// <summary>
    ///     Verifies that comments and blank lines are skipped.
    /// </summary>
    [Test]
    public async Task Parse_CommentsAndBlankLines_AreIgnored()
    {
        const string text = """
            # Plugin manifest

            id=p1
            name=P1
            # mid-file comment
            version=1
            author=A
            description=D
            apiVersion=1.0
            assembly=P1.dll
            entryType=P1.Main

            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Manifest!.Metadata.Id).IsEqualTo("p1");
    }

    /// <summary>
    ///     Verifies that whitespace around keys and values is trimmed.
    /// </summary>
    [Test]
    public async Task Parse_WhitespaceAroundKeysAndValues_IsTrimmed()
    {
        const string text = """
              id  =  spaced
            name=  N
            version  = 1
            author=A
            description=D
            apiVersion=1.0
            assembly=A.dll
            entryType=A.Main
            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Manifest!.Metadata.Id).IsEqualTo("spaced");
        await Assert.That(result.Manifest.Metadata.Name).IsEqualTo("N");
    }

    /// <summary>
    ///     Verifies that a missing required key produces a failure result.
    /// </summary>
    [Test]
    public async Task Parse_MissingIdKey_ReturnsFailure()
    {
        const string text = """
            name=N
            version=1
            author=A
            description=D
            apiVersion=1.0
            assembly=A.dll
            entryType=A.Main
            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("id");
    }

    /// <summary>
    ///     Verifies that a blank required value is treated as missing.
    /// </summary>
    [Test]
    public async Task Parse_BlankAssemblyValue_ReturnsFailure()
    {
        const string text = """
            id=p
            name=N
            version=1
            author=A
            description=D
            apiVersion=1.0
            assembly=
            entryType=A.Main
            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("assembly");
    }

    /// <summary>
    ///     Verifies that lines without '=' separator are silently skipped.
    /// </summary>
    [Test]
    public async Task Parse_LineWithoutSeparator_IsSkipped()
    {
        const string text = """
            id=p
            name=N
            this-line-has-no-equals
            version=1
            author=A
            description=D
            apiVersion=1.0
            assembly=A.dll
            entryType=A.Main
            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsTrue();
    }

    /// <summary>
    ///     Verifies that empty input fails with the first missing key.
    /// </summary>
    [Test]
    public async Task Parse_EmptyText_ReturnsFailureForId()
    {
        var result = PluginManifestReader.Parse(string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("id");
    }

    /// <summary>
    ///     Verifies that the entryType key is reported when only it is missing.
    /// </summary>
    [Test]
    public async Task Parse_MissingEntryType_ReturnsFailureForEntryType()
    {
        const string text = """
            id=p
            name=N
            version=1
            author=A
            description=D
            apiVersion=1.0
            assembly=A.dll
            """;

        var result = PluginManifestReader.Parse(text);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("entryType");
    }
}
