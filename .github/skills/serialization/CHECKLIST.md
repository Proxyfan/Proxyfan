# Serialization checklist

Detailed reference for the `serialization` skill.

## Surfaces

- **HAR 1.2** — `Domain.Session/Har/` (orchestration), `Framework.Serialization`
  (`Utf8JsonWriter`-based writer, reader). Custom `_proxyfan`-prefixed
  extension fields carry colour tags, comments, capture status.
- **YAML configuration** — `Domain.Configuration/` (snapshot, merger, key/value
  parser/writer, migration); `Framework.Serialization` (YamlDotNet adapter).
- **HTTP/2 framing** — `Framework.Networking/HypertextTransferProtocolVersion2*`
  (frame parser/writer, HPACK encoder/decoder, Huffman table, settings,
  GOAWAY, RST_STREAM, WINDOW_UPDATE, PUSH_PROMISE).
- **HTTP/1.1 framing** — `HypertextTransferProtocolHeaderParser`,
  `HypertextTransferProtocolChunkedBodyReader`,
  `HypertextTransferProtocolRequestParser`,
  `HypertextTransferProtocolResponseParser`.
- **WebSocket** — `WebSocketFrameParser`, `WebSocketMessageAssembler`.
- **Server-Sent Events** — `ServerSentEventsLineParser`, `ServerSentEventsParser`.
- **SOCKS 4/5** — `Socks4ConnectRequestParser`, `Socks5GreetingParser`,
  `Socks5ConnectRequestParser`, `SocksReplyWriter`, `SocksHandshakeReader`.
- **Content decoders** (`IContentDecoder` family) — JSON, XML, HTML,
  Protobuf, MessagePack, form data, images, hex, GraphQL, in
  `Framework.Serialization`.
- **Composer / cURL conversion** — `Domain.Traffic/CurlCommandConverter.cs`
  and the request-composer pair.

## Analysis

1. **Round-trip fidelity.** Every reader must round-trip through its
   matching writer for every documented field. Particularly:
   - HAR: `_proxyfan` extension fields must survive export → import →
     export unchanged.
   - YAML configuration: edits that the user makes by hand (key reorder,
     comments) must not be silently destroyed by a snapshot save.
   - HPACK: the decoder's dynamic table must mirror what an external
     encoder produces; the encoder's output must match the static and
     dynamic tables.

2. **Schema discipline.**
   - Required fields are required (HAR `version`, `creator`, `entries`).
     A missing required field produces a typed error, not a silent
     default.
   - Unknown fields are preserved on round-trip when the format allows it,
     dropped silently otherwise.

3. **Bounds checking.** Every binary parser sanitises lengths before
   allocating:
   - HTTP/2 frame length comes from the header — the parser refuses
     frames larger than `SETTINGS_MAX_FRAME_SIZE`.
   - HPACK integer decode rejects an indefinite-length sequence beyond a
     fixed budget.
   - Huffman decode bounds-checks the output buffer.
   - SOCKS parsers bound-check the destination address / port fields.
   - WebSocket parser refuses payload length above the configured
     ceiling.

4. **Encoding correctness.**
   - HTTP header text is ASCII / ISO-8859-1; bodies use the encoding the
     `Content-Type` advertises.
   - HAR strings are UTF-8 — confirm the writer specifies
     `Encoding.UTF8` (without BOM) and the reader rejects invalid UTF-8.
   - HPACK Huffman uses RFC 7541 Appendix B's table.
   - SOCKS host fields are ASCII (and the SOCKS 5 address-type 3 prefixes
     a length byte for the FQDN).

5. **Endianness.**
   - HTTP/2 frame fields are big-endian. The frame writer / reader use
     `BinaryPrimitives.WriteUInt32BigEndian` and friends.
   - SOCKS uses network byte order for the port.

6. **Versioning.** HAR is version 1.2; the writer emits exactly `"1.2"`,
   the reader rejects unknown majors and warns on unknown minors. YAML
   configuration version migration goes through
   `StartupConfigurationMigration` / `IMigratingConfigurationLoader`;
   never edit a stored version field in place — go through the migration.

7. **Unsafe deserialisers.** `BinaryFormatter`, `NetDataContractSerializer`,
   `LosFormatter`, and similar are forbidden. Polymorphic JSON
   deserialisation must use a typed `JsonConverter` with an allow-list.

8. **Streaming.** Large HAR sessions stream — never read the full
   document into a single `string` before parsing. The writer streams
   directly to the `FileStream` via `Utf8JsonWriter`.

9. **Compression.** HAR supports an optional `.har.gz` extension; the
   writer wraps `FileStream` in `GZipStream` only when the user opts in.
   Content decoders handle `gzip` / `deflate` / `br` / `zstd` (where
   present) consistently.

10. **Error messages.** A parse failure produces a typed error pointing at
    the byte offset / line / column. Avoid bare `FormatException` with no
    context — the user must be able to fix the source.

11. **HPACK encode/decode symmetry.** The static table is fixed; the
    dynamic table is per-direction. Updates to either must update both
    sides. The Huffman encoder and decoder must remain in sync.

12. **MIME / charset handling.** `ContentType.cs` and `ContentTypeParser.cs`
    centralise parsing. New code must consult them rather than parse
    `application/json; charset=utf-8` by hand.
