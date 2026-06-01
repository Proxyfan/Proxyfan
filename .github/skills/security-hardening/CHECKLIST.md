# Security-hardening checklist

Detailed reference for the `security-hardening` skill.

## Hardening targets

1. **Sandbox depth (`Domain.Scripting`).**
   - Per-invocation `CancellationTokenSource` linked to the proxy token.
   - Allocation checkpoints via `GC.GetAllocatedBytesForCurrentThread()`.
   - Collectible `AssemblyLoadContext` with no long-lived references to
     script-defined types.
   - Compile-time reference restriction list maintained in
     `RoslynUserScriptCompilerHelpers`.
   - On limit breach: cancel, unload ALC, auto-disable script,
     surface `ScriptError`.

2. **Root-CA private key protection.**
   - Key material lives under
     `%LOCALAPPDATA%\Proxyfan\certificates\` and is DPAPI-encrypted via
     `Framework.Platform` interop.
   - No export path writes the key in plain text.
   - No log statement emits the key, the key fingerprint, or the
     password-equivalent unwrapping secret.
   - Rotation produces a fresh key with the same protection layer.

3. **Leaf-certificate cache discipline.**
   - `LeafCertificateCache` is LRU with a 1000-entry default cap.
   - Eviction does not leak the private key into managed strings before
     GC.
   - Cache hits avoid re-minting; misses produce one cert under a
     contention-safe primitive.

4. **Redaction layering.** The log pipeline owns header redaction
   (`Authorization`, `Cookie`, `Set-Cookie`). Additional defence:
   - Structured logging fields refuse to accept a body-shaped value.
   - The traffic store's projection helpers strip secrets before any
     diagnostics surface materialises them.
   - The HAR exporter offers an opt-in "redact" mode that re-runs the
     redaction policy at export time.

5. **Upstream credential handling.** `UpstreamProxyOptions` may carry a
   basic-auth credential. Defence:
   - The credential is held in `SecureString` or DPAPI-protected memory.
   - The credential is encoded into `ProxyAuthorizationHeader` only at
     the wire boundary.
   - The credential is never echoed into a `ToString()` /
     diagnostics path.

6. **Single-instance enforcement.** `Client.Desktop` uses a single-instance
   mutex to prevent two GUI sessions from racing on the same configuration
   directory. Validate:
   - The mutex name is scoped to the current user, not machine-global.
   - A second-instance startup hands focus to the running instance and
     exits non-zero — never overwrites configuration.

7. **System-proxy registration.** Registering Proxyfan as the Windows
   system proxy requires elevation; the elevation prompt must:
   - Describe the operation in user-facing language (no opaque MSI dialog).
   - Be cancellable without leaving the registry in a partial state.
   - Restore the previous proxy on uninstall / opt-out.

8. **Reverse-proxy listener.** Per-route listeners (`ReverseProxyRouteListener`)
   bind only the configured port; the route registry refuses overlapping
   bindings. Defence:
   - Health-check failures unbind the route until recovery.
   - Each route runs under its own `CancellationToken` scope, so a misbehaving
     route does not stall siblings.

9. **Resource limits and circuit breakers.**
   - Traffic store enforces `capture.maxFlows` and evicts oldest-first.
   - Streaming-protocol global budget caps WebSocket / SSE / gRPC memory
     pressure.
   - HAR import enforces a size ceiling before parsing.
   - Map-Local file references enforce a size ceiling and a root-prefix
     check.

10. **Detection.** The right alarms exist when defence in depth fails:
    - Auto-disabled scripts log `ScriptError` at `Warning` with the
      cause.
    - Cache eviction events log at `Trace` with structured fields.
    - Privacy regressions in test suites are caught by the
      `Privacy/` test categories (where present) — flag missing or
      removed tests as P1.

11. **Privilege.** No code path elevates without prompting the user. The
    only elevation surface is `Client.Desktop`'s system-proxy registration
    and certificate-store install paths.

12. **Logging discipline.** `%LOCALAPPDATA%\Proxyfan\logs\` retains files
    for the configured `privacy.logRetentionDays` (default 30). On change:
    - Confirm the rotation honours the new retention.
    - Confirm rotation never deletes a file while another writer holds a
      handle.
    - Confirm the log directory is created with the user's ACL only.

## Forbidden silencers in proposed fixes

Never recommend disabling a security analyzer (`CA5350`, `CA5351`,
`CA5380`, …). Never recommend "log it just this once to debug" of a
sensitive value. Never recommend storing a secret in `appsettings.json` or
in source.
