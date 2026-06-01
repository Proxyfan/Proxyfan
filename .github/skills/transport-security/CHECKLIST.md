# Transport-security checklist

Detailed reference for the `transport-security` skill.

## Surfaces

- `Framework.Networking/TransportLayerSecurityInterceptorHandler.cs` and the
  `TransportLayerSecurityInterceptorHandlerDependencies` record.
- `TransportLayerSecurityInterceptorHelpers.cs` — central ALPN negotiation
  and protocol mirroring.
- `TransportLayerSecurityInterceptionPipes.cs` /
  `TransportLayerSecurityInterceptionPipesFactory.cs` — the pipe pair
  bridging the client-side and upstream-side TLS streams.
- `TransportLayerSecurityInterceptionContext.cs` and the
  `TransportLayerSecurityIntercepted*` family — per-connection state.
- `TransportLayerSecurityStrategySelector.cs` and
  `TransportLayerSecurityHandlingStrategy.cs` — strategy resolution from the
  SNI allow-list.
- `Domain.Certificates/CertificateAuthority.cs` — root CA lifecycle.
- `Domain.Certificates/LeafCertificateCache.cs` — per-host leaf cert cache
  (LRU, default 1000 entries).
- `Domain.Certificates/ServerNameIndicationProxyingList.cs` — the allow-list
  governing which hosts get intercepted vs raw-tunnelled.
- `Domain.Certificates/Provisioning/` — provisioning helpers,
  `CertificateProvisioningResponder` in `Framework.Networking`.
- `Framework.Platform` — Windows certificate-store interop and DPAPI key
  protection.

## Analysis

1. **ALPN mirroring.** The ALPN advertised to the client must match the
   protocol selected upstream. The intended values are HTTP/1.1 and h2;
   the helper that owns the negotiation is
   `TransportLayerSecurityInterceptorHelpers`. Flag any path that:
   - Advertises a protocol the upstream did not select.
   - Forgets to advertise on the client side when the upstream chose one.
   - Hard-codes a protocol instead of mirroring.

2. **Leaf-certificate generation.** Leaf certs are signed by the
   `CertificateAuthority`. Validate:
   - Hostname comes from the upstream handshake (or the CONNECT request
     when no upstream handshake is performed), not from a header the
     client controls.
   - The subject CN, SANs, and validity window match the inspected
     upstream certificate where appropriate, or use a defaulted window
     when intercepting from scratch.
   - The signing key is the root CA's; the leaf key is freshly generated
     per (host, cert) cache entry.

3. **Cache discipline.** `LeafCertificateCache` is an LRU. Validate:
   - Cache hits avoid re-minting (which is multi-millisecond and would
     blow the per-request budget on cache-cold hosts).
   - Eviction releases the leaf's private key promptly.
   - The cache is not consulted before the SNI allow-list — the
     allow-list gates whether a leaf is even generated.

4. **SNI allow-list.** `ServerNameIndicationProxyingList` is the source of
   truth. Hosts outside the list pass through as raw TCP tunnels — the TLS
   handshake is never intercepted. Validate:
   - Wildcard and suffix matches behave consistently with the documented
     semantics.
   - Disabling a host evicts its leaf cache entries.
   - Changes to the list publish `ServerNameIndicationProxyingListChanged`
     on `IDomainEventBus`.

5. **Root-CA private key.** The most sensitive secret in the product.
   Validate:
   - The key is generated with a cryptographically secure RNG.
   - The key is DPAPI-encrypted before any write to disk.
   - No code path emits the key (or any fingerprint that could derive it)
     into logs, telemetry, or the diagnostics overlay.
   - Export operations are explicit user actions, gated by a dialog, and
     produce a password-protected PFX — never a plain PEM/key file.

6. **Trust-store operations.** Installing the root CA into the Windows
   trust store goes through `Framework.Platform`. Validate:
   - The operation prompts for elevation only when needed.
   - Removal reverses the install cleanly.
   - The CA's friendly name distinguishes a Proxyfan-installed cert
     from other CAs the user trusts.

7. **Handshake error handling.** TLS handshake failures must:
   - Be classified into `CertificateError` (or a child).
   - Be logged at `Warning` with the host and the reason.
   - Drop the intercepted connection without restarting the listener.

8. **Interception pipes.** `TransportLayerSecurityInterceptionPipes`
   bridges the client and upstream `SslStream`s through
   `System.IO.Pipelines`. Validate:
   - Both sides cancel together on either side's failure.
   - No copy occurs at the `SslStream` boundary outside the pipe pump.
   - Cancellation flushes pending writes before tearing down.

9. **HTTP/2 inside TLS.** `TransportLayerSecurityInterceptedVersion2Dispatch`
   bridges the intercepted TLS session to the HTTP/2 orchestrator. The
   orchestrator owns its own cancellation; the dispatcher does not
   re-enter the handler on a partial frame.

10. **Renewal.** When the root CA approaches expiry, the
    `CertificateAuthority` produces a fresh CA and leaf cache entries
    are evicted. Validate the renewal flow does not strand the user with
    an untrusted CA in the system store.

11. **Provisioning responder.** `CertificateProvisioningResponder` serves
    the root CA over a known URL (the convention used by debugging
    proxies). Validate:
    - The responder only listens on the intercepted listener.
    - The download offers DER and PEM forms; never the private key.

## Forbidden silencers in proposed fixes

Never recommend disabling `CA5350` / `CA5351` / `CA5380` to ship a weaker
algorithm. Never recommend writing the root key in plain text "for
debugging". Never recommend hard-coding a single ALPN value to avoid the
mirroring complexity — the bug it produces appears only on h2 sites that
also support h1.
