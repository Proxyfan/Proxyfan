using Proxyfan.Domain.Traffic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Session.Har;

/// <summary>
///     Default <see cref="IHarImporter" /> implementation that parses HAR 1.2 JSON documents
///     into <see cref="TrafficFlow" /> instances.
/// </summary>
public sealed class HarImporter : IHarImporter
{
    private const int DefaultMaxEntries = 100_000;
    private const int DefaultMaxEntryBodyBytes = 100 * 1024 * 1024;
    private const long DefaultMaxHarBytes = 200L * 1024L * 1024L;
    private readonly int _maxEntries;
    private readonly int _maxEntryBodyBytes;
    private readonly long _maxHarBytes;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HarImporter" /> class with default limits.
    /// </summary>
    public HarImporter()
        : this(DefaultMaxHarBytes, DefaultMaxEntries, DefaultMaxEntryBodyBytes)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HarImporter" /> class.
    /// </summary>
    /// <param name="maxHarBytes">Maximum HAR file size, in bytes, accepted for import.</param>
    /// <param name="maxEntries">Maximum number of HAR entries processed from the file.</param>
    /// <param name="maxEntryBodyBytes">Maximum request/response body bytes retained per entry.</param>
    public HarImporter(long maxHarBytes, int maxEntries, int maxEntryBodyBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHarBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntryBodyBytes);
        _maxHarBytes = maxHarBytes;
        _maxEntries = maxEntries;
        _maxEntryBodyBytes = maxEntryBodyBytes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrafficFlow>> ImportAsync(Stream input, CancellationToken cancellationToken)
    {
        EnsureInputSizeWithinLimit(input);
        var reader = new HarImportEntryStreamReader(_maxHarBytes, _maxEntries, _maxEntryBodyBytes);
        return await reader.ReadAsync(input, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureInputSizeWithinLimit(Stream input)
    {
        if (!input.CanSeek)
        {
            return;
        }

        var remainingLength = input.Length - input.Position;
        if (remainingLength > _maxHarBytes)
        {
            throw new InvalidDataException($"HAR file exceeds the configured {_maxHarBytes} byte import limit.");
        }
    }

    private sealed class HarImportEntryStreamReader
    {
        private const int BufferSizeInBytes = 64 * 1024;
        private readonly int _maxEntries;
        private readonly int _maxEntryBodyBytes;
        private readonly long _maxHarBytes;
        private int _bytesConsumed;
        private int _entriesDepth;
        private int _entriesRead;
        private bool _isInsideEntriesArray;
        private bool _isInsideLogObject;
        private JsonReaderState _jsonReaderState;
        private int _logDepth;
        private string? _propertyName;

        public HarImportEntryStreamReader(long maxHarBytes, int maxEntries, int maxEntryBodyBytes)
        {
            var jsonReaderOptions = new JsonReaderOptions
            {
                MaxDepth = 64,
            };
            var jsonReaderState = new JsonReaderState(jsonReaderOptions);
            _jsonReaderState = jsonReaderState;
            _maxHarBytes = maxHarBytes;
            _maxEntries = maxEntries;
            _maxEntryBodyBytes = maxEntryBodyBytes;
            _entriesDepth = -1;
            _logDepth = -1;
        }

        public async Task<IReadOnlyList<TrafficFlow>> ReadAsync(Stream input, CancellationToken cancellationToken)
        {
            var flows = new List<TrafficFlow>();
            var bytesInBuffer = 0;
            var bytesReadTotal = 0L;
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSizeInBytes);

            try
            {
                while (true)
                {
                    if (bytesInBuffer == buffer.Length)
                    {
                        throw new InvalidDataException("HAR import encountered a token larger than the supported parser buffer.");
                    }

                    var bytesRead = await ReadChunkAsync(input, buffer, bytesInBuffer, cancellationToken).ConfigureAwait(false);
                    var isFinalBlock = bytesRead == 0;
                    bytesInBuffer += bytesRead;
                    bytesReadTotal += bytesRead;

                    if (bytesReadTotal > _maxHarBytes)
                    {
                        throw new InvalidDataException($"HAR file exceeds the configured {_maxHarBytes} byte import limit.");
                    }

                    var parseBufferResult = ParseTokens(buffer, bytesInBuffer, isFinalBlock, flows);
                    if (parseBufferResult == ParseBufferResult.StopImport)
                    {
                        return flows;
                    }

                    bytesInBuffer = CompactBuffer(buffer, bytesInBuffer);
                    if (isFinalBlock)
                    {
                        return flows;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private int CompactBuffer(byte[] buffer, int bytesInBuffer)
        {
            var remainingBytes = bytesInBuffer - _bytesConsumed;
            if (remainingBytes > 0)
            {
                Buffer.BlockCopy(buffer, _bytesConsumed, buffer, 0, remainingBytes);
            }

            _bytesConsumed = 0;
            return remainingBytes;
        }

        private void HandleEndArray(Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.EndArray || !_isInsideEntriesArray || reader.CurrentDepth != _entriesDepth)
            {
                return;
            }

            _entriesDepth = -1;
            _isInsideEntriesArray = false;
        }

        private void HandleEndObject(Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.EndObject || !_isInsideLogObject || reader.CurrentDepth != _logDepth)
            {
                return;
            }

            _isInsideLogObject = false;
            _logDepth = -1;
        }

        private void HandlePropertyName(Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                _propertyName = reader.GetString();
            }
        }

        private void HandleStartArray(Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                return;
            }

            if (_isInsideLogObject && string.Equals(_propertyName, "entries", StringComparison.Ordinal))
            {
                _entriesDepth = reader.CurrentDepth;
                _isInsideEntriesArray = true;
            }

            _propertyName = null;
        }

        private void HandleStartObject(Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return;
            }

            if (!_isInsideLogObject && string.Equals(_propertyName, "log", StringComparison.Ordinal))
            {
                _isInsideLogObject = true;
                _logDepth = reader.CurrentDepth;
            }

            _propertyName = null;
        }

        private ParseBufferResult ParseTokens(byte[] buffer, int bytesInBuffer, bool isFinalBlock, List<TrafficFlow> flows)
        {
            var jsonSpan = new ReadOnlySpan<byte>(buffer, 0, bytesInBuffer);
            var reader = new Utf8JsonReader(jsonSpan, isFinalBlock, _jsonReaderState);

            while (reader.Read())
            {
                if (_isInsideEntriesArray && reader.CurrentDepth == _entriesDepth + 1)
                {
                    if (!JsonDocument.TryParseValue(ref reader, out var entryDocument))
                    {
                        _bytesConsumed = (int)reader.BytesConsumed;
                        _jsonReaderState = reader.CurrentState;
                        return ParseBufferResult.NeedMoreData;
                    }

                    using (entryDocument)
                    {
                        _entriesRead++;
                        if (_entriesRead > _maxEntries)
                        {
                            _bytesConsumed = (int)reader.BytesConsumed;
                            _jsonReaderState = reader.CurrentState;
                            return ParseBufferResult.StopImport;
                        }

                        var flow = HarEntryParser.ParseEntry(entryDocument.RootElement, _maxEntryBodyBytes);
                        if (flow is not null)
                        {
                            flows.Add(flow);
                        }
                    }

                    continue;
                }

                HandlePropertyName(reader);
                HandleStartObject(reader);
                HandleStartArray(reader);
                HandleEndObject(reader);
                HandleEndArray(reader);
            }

            _bytesConsumed = (int)reader.BytesConsumed;
            _jsonReaderState = reader.CurrentState;
            return ParseBufferResult.Continue;
        }

        private async Task<int> ReadChunkAsync(Stream input, byte[] buffer, int bytesInBuffer, CancellationToken cancellationToken)
        {
            return await input.ReadAsync(buffer.AsMemory(bytesInBuffer), cancellationToken).ConfigureAwait(false);
        }

        private enum ParseBufferResult
        {
            Continue,
            NeedMoreData,
            StopImport,
        }
    }
}
