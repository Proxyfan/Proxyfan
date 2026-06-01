# Bug-bounty checklist

Detailed reference for the `bug-bounty` skill, covering both whole-codebase
analysis and PR-diff review.

## Vulnerability classes

1. **Injection.** Path traversal in HAR import, log injection in structured
   logging fields, header-value injection in upstream proxy chaining,
   command-line argument injection in `Cli` handlers, format-string flaws.
   Validate every external input.

2. **Authentication and authorisation.** Upstream proxy basic auth (where
   credentials flow through `ProxyAuthorizationHeader`), reverse-proxy
   route protection, and any UI surface that gates a privileged operation
   (system-proxy registration, certificate-store install). Flag missing
   checks and IDOR-style flaws.

3. **Insecure deserialization.** HAR import is the largest deserialisation
   surface; Protobuf and MessagePack content decoders are the second.
   Flag use of unsafe deserialisers (`BinaryFormatter`,
   `NetDataContractSerializer`), missing type validation on inbound
   payloads, and polymorphic deserialisation without an allow-list.

4. **Sensitive data exposure.** Secrets, tokens, PII committed to source,
   logged at runtime, cached in temp files, or surfaced through error
   messages. Particularly:
   - `Authorization`, `Cookie`, `Set-Cookie` must be redacted at every log
     level.
   - Request and response bodies must never appear in logs.
   - The root CA private key lives in
     `%LOCALAPPDATA%\Proxyfan\certificates\` and is DPAPI-protected — any
     code path that exposes it (export, copy, log) is P1.

5. **Cryptography.** Flag MD5, SHA1, DES, RC4, ECB mode, or insufficient
   entropy where security depends on the choice. The certificate
   authority's key generation must use a cryptographically secure RNG.

6. **TLS interception authority.** This is Proxyfan's most powerful
   capability. Validate:
   - Leaf certificate generation pulls hostname from the upstream
     handshake, not from a header the client controls.
   - The SNI proxying allow-list (`ServerNameIndicationProxyingList`) is
     consulted before leaf-cert generation — hosts outside the list
     receive a raw tunnel, never an intercepted handshake.
   - The root CA private key is never written to disk in plain text and is
     never exposed across the IPC / debug surface.

7. **Sandbox escapes.** `Domain.Scripting` runs user C# in an
   `AssemblyLoadContext`. Flag any path that lets user code:
   - Open files / sockets / processes.
   - Use reflection emit, `Marshal`, or P/Invoke.
   - Spawn threads or `Task.Run` outside the controlled invocation.
   - Block past the wall-clock timeout.
   - Allocate past the memory ceiling.

8. **Misconfiguration.** Debug endpoints exposed in release builds,
   verbose error pages, missing CSP on any embedded WebView, insecure
   transport defaults, missing rate limits on listener accept loops.

9. **Dependency vulnerabilities.** `Directory.Packages.props` is the
   single source of truth. Flag packages with known CVEs. The pinned
   `Tmds.DBus.Protocol` already cites a CVE — confirm comparable pins
   exist for any newly added transitive dependency.

10. **Input validation.** File paths supplied via `Cli` or the UI's file
    picker must be validated before use. HAR file imports must enforce a
    size cap before parsing. Map-Local file references must remain inside
    the user-allowed root.

11. **Race conditions.** TOCTOU on certificate-store install, ReDoS on
    user-supplied regex patterns (`BypassPatternMatcher`,
    `AllowListRule`, `BlockListRule`), unbounded growth in
    `BreakpointPauseInbox` if a UI handler never resumes.

12. **Privacy regressions.** A category specific to Proxyfan: any change
    that captures bodies into logs, sends data outside the user's
    machine, or weakens redaction is **always** P1. Privacy is a product
    promise.

## Prioritisation

| Priority | Criteria |
|---|---|
| P1 — Critical | Directly exploitable with no auth; high impact (RCE, root-CA exposure, sandbox escape, body or credentials leaked to logs/network). |
| P2 — High | Exploitable with low effort; significant data or system impact. |
| P3 — Medium | Requires specific conditions; moderate impact. |
| P4 — Low | Difficult to exploit; limited impact. |

## Forbidden silencers in proposed fixes

Never recommend `#pragma warning disable` for `CA5350` / `CA5351` (weak
crypto) or any other security analyzer. The analyzer is right; the code is
wrong.
