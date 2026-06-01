# Session-format checklist

Detailed reference for the `session-format` skill.

## Surfaces

- `Domain.Session/Har/` — exporter, importer, `HarDocumentWriter`,
  `IHarExporter`, supporting types.
- `Framework.Serialization` — the JSON / streaming primitives that back the
  HAR writer.
- `Domain.Traffic` — the source flow types projected into the HAR shape.

## HAR contract

- Specification: HAR 1.2.
- Top-level shape: `{ "log": { "version": "1.2", "creator": …, "entries": [ … ] } }`.
- Each `entry` projects from a `TrafficFlow` (request + response timing,
  cookies, headers, content, redirect, …).
- Custom fields are prefixed with `_proxyfan` per the HAR extension
  convention (e.g. `_proxyfanFlowId`, `_proxyfanColorTag`,
  `_proxyfanStatus`).
- Optional gzip wrapping (`.har.gz`).

## Analysis

1. **Schema fidelity.** Every HAR 1.2 required field is emitted; every
   optional field is emitted when the corresponding source data exists.
   Confirm that the imports of any field a user might have set in another
   tool round-trip cleanly through Proxyfan.

2. **Round-trip integrity.** Export → import → export must produce a
   byte-identical document for the same input flow set (modulo
   non-deterministic ordering keys, which must be stable on the second
   write). Flag exporters that emit a field on first export but drop it
   on re-export.

3. **`_proxyfan` extension fields.** These carry colour tags, comments,
   capture status, and flow IDs across export/import. Validate:
   - The prefix is consistent (`_proxyfan…`).
   - Unknown `_proxyfan…` fields on import are preserved if the
     importer cannot interpret them — never silently dropped.
   - New extension fields are documented in `docs/ARCHITECTURE.md`
     § 11.2.

4. **Streaming write.** The writer emits to the file as flows are
   serialised — never buffer the entire `entries` array in memory.
   `Utf8JsonWriter` is the primitive. Validate that no intermediate
   `string` materialises an entry's body for the writer.

5. **Streaming read.** Large HAR imports parse incrementally, applying
   each entry to the in-memory store as it arrives. Validate the reader
   honours the `capture.maxFlows` cap during import and evicts oldest
   entries when full.

6. **Encoding.** UTF-8 without a BOM on disk. The writer specifies
   `Encoding.UTF8` (or `new UTF8Encoding(false)`) explicitly. The reader
   rejects invalid UTF-8 with a typed `SessionError`.

7. **Compression.** When the user picks `.har.gz`, the writer wraps the
   `FileStream` in `GZipStream` with `CompressionLevel.Fastest` for
   balance between time and size. Validate the importer detects gzip via
   the `.gz` extension and the file magic bytes — not only one or the
   other.

8. **Body encoding.** HAR `content.text` carries text bodies; binary
   bodies are base64-encoded with `encoding: "base64"`. Validate the
   exporter picks the right form based on the captured `Content-Type`
   and the body bytes.

9. **Performance budgets.** Save/load of 10 K flows must finish in < 5 s.
   Flag changes that add a synchronous pass over the flow set or that
   reload a content decoder per entry instead of caching.

10. **Versioning.** Bumping the HAR major version is forbidden — Proxyfan
    targets 1.2. If the spec ever evolves, the change is gated by a
    Phase-3 stop-and-ask per `review-gates.instructions.md`.

11. **Privacy on export.** An opt-in redaction mode re-runs the redaction
    policy at export time, scrubbing `Authorization`, `Cookie`,
    `Set-Cookie` and any user-configured additional headers from the
    output. Flag any path that emits sensitive headers in redaction mode.

12. **Atomic write.** The writer writes to a `*.har.tmp` then renames to
    the final path. A crash mid-write leaves the previous file intact.
