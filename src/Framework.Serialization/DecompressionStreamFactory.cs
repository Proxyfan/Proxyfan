using System.IO;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Factory delegate that wraps a source stream in a decompression stream.
/// </summary>
/// <param name="source">The source stream to wrap.</param>
/// <returns>A read-only stream that decompresses bytes from the source.</returns>
public delegate Stream DecompressionStreamFactory(Stream source);
