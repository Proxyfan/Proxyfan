using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Default <see cref="IRemoteProcedureCallDescriptorLibrary" /> implementation backed by
///     an in-memory <see cref="Dictionary{TKey,TValue}" /> keyed by source path. The
///     <see cref="Index" /> property returns a snapshot built from the current set of loaded
///     descriptor files; it is rebuilt whenever the library mutates (load/remove/clear).
///     Thread-safe via a single internal lock.
/// </summary>
public sealed class RemoteProcedureCallDescriptorLibrary : IRemoteProcedureCallDescriptorLibrary
{
    private readonly Dictionary<string, IReadOnlyList<ProtobufFileDescriptor>> _filesBySource;
    private readonly Lock _gate;
    private ProtobufDescriptorIndex _index;

    /// <summary>
    ///     Initializes a new empty library.
    /// </summary>
    public RemoteProcedureCallDescriptorLibrary()
    {
        var gate = new Lock();
        _gate = gate;
        var filesBySource = new Dictionary<string, IReadOnlyList<ProtobufFileDescriptor>>(StringComparer.Ordinal);
        _filesBySource = filesBySource;
        var emptyIndex = new ProtobufDescriptorIndex([]);
        _index = emptyIndex;
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _filesBySource.Clear();
            RebuildIndex();
        }
    }

    /// <inheritdoc />
    public ProtobufDescriptorIndex Index => Volatile.Read(ref _index);

    /// <inheritdoc />
    public void Load(string sourcePath, byte[] payload)
    {
        var parsed = ProtobufFileDescriptorSetParser.Parse(payload);
        lock (_gate)
        {
            _filesBySource[sourcePath] = parsed;
            RebuildIndex();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> LoadedFilePaths
    {
        get
        {
            lock (_gate)
            {
                var snapshot = new List<string>(_filesBySource.Count);
                foreach (var key in _filesBySource.Keys)
                {
                    snapshot.Add(key);
                }

                return snapshot;
            }
        }
    }

    /// <inheritdoc />
    public void Unload(string sourcePath)
    {
        lock (_gate)
        {
            if (_filesBySource.Remove(sourcePath))
            {
                RebuildIndex();
            }
        }
    }

    private void RebuildIndex()
    {
        var combined = new List<ProtobufFileDescriptor>();
        foreach (var entry in _filesBySource.Values)
        {
            combined.AddRange(entry);
        }

        var fresh = new ProtobufDescriptorIndex(combined);
        Volatile.Write(ref _index, fresh);
    }
}