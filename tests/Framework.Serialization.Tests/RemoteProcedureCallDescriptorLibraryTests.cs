using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallDescriptorLibrary" /> covering load/unload,
///     index rebuild semantics, and snapshot isolation.
/// </summary>
public sealed class RemoteProcedureCallDescriptorLibraryTests
{
    /// <summary>
    ///     A freshly constructed library has an empty descriptor index and no loaded files.
    /// </summary>
    [Test]
    public async Task Construct_Empty_HasZeroLoadedFilesAndEmptyIndex()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();

        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(library.Index.Files.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Loading a descriptor set populates both the loaded-file list and the index.
    /// </summary>
    [Test]
    public async Task Load_SingleFile_AppearsInIndexAndPathList()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        var setBytes = BuildSetWithEmptyFile("hello.proto", "demo");

        library.Load("hello.pb", setBytes);

        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(1);
        await Assert.That(library.LoadedFilePaths[0]).IsEqualTo("hello.pb");
        await Assert.That(library.Index.Files.Count).IsEqualTo(1);
        await Assert.That(library.Index.Files[0].Package).IsEqualTo("demo");
    }

    /// <summary>
    ///     Loading two descriptor sets keyed by the same source path replaces the previous
    ///     content rather than accumulating it.
    /// </summary>
    [Test]
    public async Task Load_SamePathTwice_ReplacesPreviousContent()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        library.Load("hello.pb", BuildSetWithEmptyFile("hello-v1.proto", "v1"));
        library.Load("hello.pb", BuildSetWithEmptyFile("hello-v2.proto", "v2"));

        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(1);
        await Assert.That(library.Index.Files.Count).IsEqualTo(1);
        await Assert.That(library.Index.Files[0].Package).IsEqualTo("v2");
    }

    /// <summary>
    ///     Unloading a source path removes it from both the path list and the index.
    /// </summary>
    [Test]
    public async Task Unload_KnownPath_RemovesFromIndex()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        library.Load("a.pb", BuildSetWithEmptyFile("a.proto", "a"));
        library.Load("b.pb", BuildSetWithEmptyFile("b.proto", "b"));

        library.Unload("a.pb");

        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(1);
        await Assert.That(library.LoadedFilePaths[0]).IsEqualTo("b.pb");
        await Assert.That(library.Index.Files.Count).IsEqualTo(1);
        await Assert.That(library.Index.Files[0].Package).IsEqualTo("b");
    }

    /// <summary>
    ///     Unloading an unknown path is a no-op (no exception, no state change).
    /// </summary>
    [Test]
    public async Task Unload_UnknownPath_NoOp()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        library.Load("a.pb", BuildSetWithEmptyFile("a.proto", "a"));

        library.Unload("ghost.pb");

        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Clear empties the library entirely.
    /// </summary>
    [Test]
    public async Task Clear_AfterLoad_EmptiesEverything()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        library.Load("a.pb", BuildSetWithEmptyFile("a.proto", "a"));
        library.Load("b.pb", BuildSetWithEmptyFile("b.proto", "b"));

        library.Clear();

        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(0);
        await Assert.That(library.Index.Files.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     LoadedFilePaths returns a snapshot that is not affected by subsequent mutations.
    /// </summary>
    [Test]
    public async Task LoadedFilePaths_AfterMutation_IsIsolatedSnapshot()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        library.Load("a.pb", BuildSetWithEmptyFile("a.proto", "a"));
        var snapshot = library.LoadedFilePaths;

        library.Load("b.pb", BuildSetWithEmptyFile("b.proto", "b"));

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(library.LoadedFilePaths.Count).IsEqualTo(2);
    }

    private static byte[] BuildSetWithEmptyFile(string fileName, string package)
    {
        using var fileWriter = new ProtobufWireWriter();
        fileWriter.WriteStringField(1, fileName);
        fileWriter.WriteStringField(2, package);
        var fileBytes = fileWriter.ToArray();
        using var setWriter = new ProtobufWireWriter();
        setWriter.WriteBytesField(1, fileBytes);
        return setWriter.ToArray();
    }
}
