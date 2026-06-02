# Project Journal

> Append-only epistemic memory for Proxyfan coding agents. See
> [`.github/journal-protocol.md`](.github/journal-protocol.md) for the protocol
> that governs how entries are written and how this file is read. Past entries
> are immutable — only append.

### 2026-06-02 05:42 — [networking,security,privacy] Fix #39: strip client Proxy-Authorization on upstream-proxy hop
- **Learned:** `UpstreamProxyRequestRewriter` was the sole rewriter that did not strip `Proxy-Authorization` unconditionally; sibling rewriters (`OriginRequestRewriter` line 29-43, `UpgradeRequestRewriter`, `UpgradeResponseRewriter`, `ForwardedResponseRewriter`) all use a single `AlwaysStrippedHeaders` `HashSet<string>` with `OrdinalIgnoreCase` and `.Trim()` the header name before lookup — copy that pattern.
- **Unclear:** `UpstreamProxyRequestRewriter` still does not strip other hop-by-hop headers (`Connection`, `Proxy-Connection`, `Keep-Alive`, `TE`, `Upgrade`, `Proxy-Authenticate`, plus `Connection`-listed names) that `OriginRequestRewriter` already handles — likely a real gap worth a follow-up ticket beyond #39's scope.
- **Harder:** Existing test `RewriteHeaders_NoProxyAuthorization_PreservesClientHeader` actively asserted the buggy behavior as if intentional, so the diff inverts that test under a new name rather than deleting it silently — kept the rename visible in PR review.

### 2026-06-02 08:09 — [presentation,mvvm] Fix #259: ContainerLocator.Set discards already-resolved provider
- **Learned:** `ContainerLocator.Current` uses the C# `field` keyword backing store with `field ??= _lazyContainer?.Value` (`src/Presentation/ContainerLocator.cs:18-22`), so the resolved provider is cached independently of `_lazyContainer` — replacing the `Lazy<>` in `Set` alone is not enough; the `field` cache must also be cleared via `Current = null` for the next access to honour the new factory.
- **Unclear:** Whether any other static locator/cache in `src/Presentation` uses the same `field`-backed `??=` pattern with similar replace-without-clear bugs (LocalizationService caches, theme resources). Worth a focused grep next session.
- **Harder:** `Cli.Tests.dll` failed once under the parallel full-suite run but passed both in isolation (98/98) and on the full-suite rerun — `CliStartHandler` boots a real proxy server, so concurrent test projects can collide on listener startup; reran per PROCESS.md Step 7 instead of treating it as a real regression.