# Protocol-parsers checklist

Detailed reference for the `protocol-parsers` skill.

## HTTP/1.1

- `HypertextTransferProtocolHeaderParser.cs` — start line + header block.
- `HypertextTransferProtocolRequestParser.cs`,
  `HypertextTransferProtocolResponseParser.cs` — full message parsing.
- `HypertextTransferProtocolBodyFraming.cs`,
  `HypertextTransferProtocolBodyFramingClassifier.cs` — framing decision
  (Content-Length vs chunked vs identity).
- `HypertextTransferProtocolChunkedBodyReader.cs` — chunked decoder.
- `HypertextTransferProtocolMethodPrefixDetector.cs` — first-byte detection
  for dispatch.
- `HypertextTransferProtocolRequestLine.cs`,
  `HypertextTransferProtocolResponseStatusLine.cs`.
- `HypertextTransferProtocolPipeHelpers.cs` — pipe-based read helpers.

### Checks

1. Header parsing accepts CRLF and LF (servers in the wild are forgiving)
   but writes only CRLF. Header names are case-insensitive; values
   preserve case.
2. `Content-Length` and `Transfer-Encoding: chunked` are mutually
   exclusive; presence of both is an error (`HypertextTransferProtocolBodyFramingClassifier`
   owns the rule).
3. Trailing headers in a chunked body are parsed if present.
4. Status line `HTTP/1.1 200 OK` is parsed tolerantly (some servers omit
   the reason phrase).
5. CONNECT requests carry a `host:port` request target rather than a URI.

## HTTP/2

- `HypertextTransferProtocolVersion2ConnectionPreface.cs` — preface
  detection.
- `HypertextTransferProtocolVersion2FrameHeader.cs`,
  `HypertextTransferProtocolVersion2FrameParser.cs`,
  `HypertextTransferProtocolVersion2FrameReader.cs`,
  `HypertextTransferProtocolVersion2FrameWriter.cs`,
  `HypertextTransferProtocolVersion2Frame.cs`,
  `HypertextTransferProtocolVersion2FrameDescriptor.cs`,
  `HypertextTransferProtocolVersion2FrameFlag.cs`,
  `HypertextTransferProtocolVersion2FrameType.cs`.
- `HypertextTransferProtocolVersion2SettingsParser.cs`,
  `HypertextTransferProtocolVersion2SettingsWriter.cs`,
  `HypertextTransferProtocolVersion2SettingIdentifier.cs`,
  `HypertextTransferProtocolVersion2SettingParameter.cs`.
- `HypertextTransferProtocolVersion2HeadersFramePayloadParser.cs`,
  `HypertextTransferProtocolVersion2DataFramePayloadParser.cs`,
  `HypertextTransferProtocolVersion2WindowUpdateParser.cs`,
  `HypertextTransferProtocolVersion2ResetStreamParser.cs`,
  `HypertextTransferProtocolVersion2GoAwayParser.cs`,
  `HypertextTransferProtocolVersion2PushPromiseParser.cs`.
- `HypertextTransferProtocolVersion2HeaderBlockAssembler.cs` —
  HEADERS + CONTINUATION reassembly.
- HPACK: `HypertextTransferProtocolVersion2HpackDecoder.cs`,
  `HpackEncoder.cs`, `HpackDynamicTable.cs`, `HpackStaticTable.cs`,
  `HpackHuffman.cs`, `HpackHuffmanTable.cs`, `HpackInteger.cs`,
  `HpackIntegerDecodeResult.cs`, `HpackLiteralLayout.cs`,
  `HpackLiteralOptions.cs`, `HpackStringDecodeResult.cs`,
  `HpackStringDecoder.cs`, `HpackIndexedWriter.cs`,
  `HpackHeaderField.cs`, `HpackTableLookup.cs`.
- Stream state: `HypertextTransferProtocolVersion2Stream.cs`,
  `HypertextTransferProtocolVersion2StreamRegistry.cs`,
  `HypertextTransferProtocolVersion2StreamState.cs`,
  `HypertextTransferProtocolVersion2StreamStateMachine.cs`,
  `HypertextTransferProtocolVersion2StreamTransitionResult.cs`.
- Flow control: `HypertextTransferProtocolVersion2FlowControlWindow.cs`.

### Checks

1. Frame length is bounded by `SETTINGS_MAX_FRAME_SIZE`. Parser refuses
   larger frames with `FRAME_SIZE_ERROR`.
2. HEADERS + CONTINUATION must arrive contiguously on the same stream
   (no other frame may interleave).
3. HPACK encoder and decoder must stay in lock-step on the dynamic table.
   Updates to the static table must update both sides.
4. Huffman decode honours RFC 7541 Appendix B; rejects EOS and over-long
   padding.
5. Stream state transitions enforce HTTP/2 § 5.1: a state transition that
   the spec disallows triggers a `RST_STREAM` or a connection error.
6. Flow-control accounting honours both connection and stream-level
   windows; a `WINDOW_UPDATE` of zero is a `PROTOCOL_ERROR`.
7. `PUSH_PROMISE` arrives only when `SETTINGS_ENABLE_PUSH` is on the
   receiver's side; Proxyfan does not initiate pushes.
8. `GOAWAY` carries the highest peer-initiated stream ID; subsequent
   streams above it are ignored.

## WebSocket

- `WebSocketFrameHeader.cs`, `WebSocketFrame.cs`, `WebSocketOpcodes.cs`,
  `WebSocketOpcode.cs`, `WebSocketFrameParser.cs`,
  `WebSocketMessageAssembler.cs`, `WebSocketUpgradeDetector.cs`,
  `WebSocketUpgradeTunnel.cs`, `WebSocketRelay.cs`,
  `WebSocketRelayDirection.cs`, `WebSocketRelayDirectionRequest.cs`,
  `WebSocketMessageCallback.cs`.

### Checks

1. The mask bit is set on client-to-server frames and clear on
   server-to-client. The parser refuses to apply a mask in the wrong
   direction.
2. Continuation frames assemble into a single message until the FIN bit
   is set.
3. Control frames (CLOSE, PING, PONG) carry payloads ≤ 125 bytes and
   never interleave inside a fragmented message.
4. The relay enforces the per-connection message-buffer cap and the
   global streaming budget.

## Server-Sent Events

- `ServerSentEventsLineParser.cs`, `ServerSentEventsParser.cs`,
  `ServerSentEventField.cs`, `ServerSentEvent.cs`,
  `ServerSentEventsStreamHandler.cs`, `ServerSentEventsRelay.cs`,
  `ServerSentEventsRelayRequest.cs`, `ServerSentEventsResponseDetector.cs`,
  `ServerSentEventsStreamRequest.cs`, `ServerSentEventsUpstreamStreams.cs`,
  `ServerSentEventCallback.cs`.

### Checks

1. Line endings: LF, CR, or CRLF. The parser handles all three.
2. Fields are `event`, `data`, `id`, `retry`. Multiple `data` lines
   concatenate with `\n` separators; the parser preserves this.
3. A blank line dispatches the buffered event.
4. The relay enforces the per-connection event-buffer cap (default
   5,000) and the global streaming budget.

## gRPC

`HypertextTransferProtocolVersion2RemoteProcedureCallCapture.cs` consumes
HTTP/2 DATA frames whose `Content-Type` is `application/grpc*` and
records each gRPC message into the `RemoteProcedureCallStore`. Validate:

1. The length-prefix framing (`compressed-flag uint8, length uint32_be,
   payload bytes`) is parsed correctly.
2. Trailing-only frames carry the gRPC status; the parser surfaces it
   distinctly from a body chunk.
3. Bidirectional streaming captures messages in arrival order on both
   directions.

## SOCKS 4/5

- `Socks4ConnectRequest.cs`, `Socks4ConnectRequestParser.cs`,
  `Socks5Greeting.cs`, `Socks5GreetingParser.cs`,
  `Socks5ConnectRequest.cs`, `Socks5ConnectRequestParser.cs`,
  `Socks5AddressType.cs`, `SocksReplyWriter.cs`,
  `SocksHandshakeReader.cs`, `SocksProtocolDetector.cs`,
  `SocksTunnelHandler.cs`, `SocksVersion.cs`.

### Checks

1. Address types 1 (IPv4), 3 (FQDN), 4 (IPv6) parsed exhaustively. Type 3
   reads the length byte before the FQDN bytes.
2. Authentication methods: SOCKS5 advertises only "no auth" (0x00).
   Reject any request that requires a different method (currently).
3. Reply codes mirror RFC 1928 § 6.
4. Port fields are big-endian.
