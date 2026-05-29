using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Presentation.Tests.Shortcuts;

/// <summary>
///     Tests for <see cref="FileShortcutBindingsStore" />.
/// </summary>
public sealed class FileShortcutBindingsStoreTests
{
    /// <summary>
    ///     Verifies <see cref="FileShortcutBindingsStore.Load" /> returns an empty map when the
    ///     file does not exist.
    /// </summary>
    [Test]
    public async Task Load_MissingFile_ReturnsEmpty()
    {
        var path = CreateTempPath();
        var store = new FileShortcutBindingsStore(path);

        var result = store.Load();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies <see cref="FileShortcutBindingsStore.Save" /> creates the parent directory
    ///     and writes valid JSON.
    /// </summary>
    [Test]
    public async Task Save_NewFileInNewDirectory_CreatesDirectoryAndWritesJson()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"proxyfan-shortcut-test-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "shortcuts.json");

        try
        {
            var store = new FileShortcutBindingsStore(path);
            var bindings = new Dictionary<ShortcutAction, KeyboardGesture>
            {
                [ShortcutAction.Find] = new() { Key = "F", Modifiers = KeyboardModifier.Control },
            };

            store.Save(bindings);

            await Assert.That(Directory.Exists(directory)).IsTrue();
            await Assert.That(File.Exists(path)).IsTrue();
            var content = File.ReadAllText(path);
            await Assert.That(content.Length).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    ///     Verifies a Save → Load round-trip preserves every binding.
    /// </summary>
    [Test]
    public async Task RoundTrip_LoadAfterSave_ReturnsSameBindings()
    {
        var path = CreateTempPath();

        try
        {
            var store = new FileShortcutBindingsStore(path);
            var bindings = new Dictionary<ShortcutAction, KeyboardGesture>
            {
                [ShortcutAction.ToggleCapture] = new() { Key = "R", Modifiers = KeyboardModifier.Control },
                [ShortcutAction.ClearTraffic] = new() { Key = "K", Modifiers = KeyboardModifier.Control },
                [ShortcutAction.ToggleBreakpoint] = new() { Key = "B", Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift },
            };

            store.Save(bindings);
            var loaded = store.Load();

            await Assert.That(loaded.Count).IsEqualTo(3);
            await Assert.That(loaded[ShortcutAction.ToggleCapture].Key).IsEqualTo("R");
            await Assert.That(loaded[ShortcutAction.ClearTraffic].Key).IsEqualTo("K");
            await Assert.That(loaded[ShortcutAction.ToggleBreakpoint].Modifiers)
                .IsEqualTo(KeyboardModifier.Control | KeyboardModifier.Shift);
        }
        finally
        {
            DeleteFileIfExists(path);
        }
    }

    /// <summary>
    ///     Verifies an existing file with malformed contents loads as empty (no throw).
    /// </summary>
    [Test]
    public async Task Load_MalformedFile_ReturnsEmpty()
    {
        var path = CreateTempPath();

        try
        {
            File.WriteAllText(path, "{ this is not valid json");
            var store = new FileShortcutBindingsStore(path);

            var result = store.Load();

            await Assert.That(result.Count).IsEqualTo(0);
        }
        finally
        {
            DeleteFileIfExists(path);
        }
    }

    /// <summary>
    ///     Verifies that a second Save overwrites the first.
    /// </summary>
    [Test]
    public async Task Save_TwiceWithDifferentBindings_OverwritesFirst()
    {
        var path = CreateTempPath();

        try
        {
            var store = new FileShortcutBindingsStore(path);
            store.Save(new Dictionary<ShortcutAction, KeyboardGesture>
            {
                [ShortcutAction.Find] = new() { Key = "F", Modifiers = KeyboardModifier.Control },
            });
            store.Save(new Dictionary<ShortcutAction, KeyboardGesture>
            {
                [ShortcutAction.ClearTraffic] = new() { Key = "K", Modifiers = KeyboardModifier.Control },
            });

            var result = store.Load();

            await Assert.That(result.Count).IsEqualTo(1);
            await Assert.That(result.ContainsKey(ShortcutAction.ClearTraffic)).IsTrue();
            await Assert.That(result.ContainsKey(ShortcutAction.Find)).IsFalse();
        }
        finally
        {
            DeleteFileIfExists(path);
        }
    }

    private static string CreateTempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"proxyfan-shortcuts-{Guid.NewGuid():N}.json");
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
