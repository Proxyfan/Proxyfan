# Rule-engine checklist

Detailed reference for the `rule-engine` skill.

## Surfaces

- `Domain.Rules/RuleEngine.cs` — evaluates request and response phases.
- `Domain.Rules/IRuleRegistry.cs`, `RuleRegistry.cs`,
  `RuleRegistryChanged.cs` — registration and change notification.
- `Domain.Rules/IRequestPhaseRule.cs`, `IResponsePhaseRule.cs` — rule
  surface.
- `Domain.Rules/Pipeline/RequestPipelineAction.cs`,
  `ResponsePipelineAction.cs` — discriminated unions of pipeline outcomes.
- `Domain.Rules/Rules/` — individual rule implementations:
  `AllowListRule` / `MutableAllowListRule`,
  `BlockListRule` / `MutableBlockListRule`,
  `MapLocalRule` / `MutableMapLocalRule`,
  `MapRemoteRule` / `MutableMapRemoteRule`,
  `BreakpointPause`, `BreakpointPauseInbox`, `InteractiveBreakpointHandler`,
  `NoCachingRule` / `MutableNoCachingRule`,
  `HeaderStripper`.
- `Domain.Rules/Matching/` — shared matchers (host patterns, methods,
  status-code ranges).

## Rule evaluation order

The order is fixed and short-circuits on a terminal match. Confirm
implementation agrees with `AGENTS.md`'s description.

**Request phase:**
1. **Allow List** — if active and host not matched → connection closed,
   remaining rules skipped.
2. **Block List** — if matched → request rejected (403), remaining rules
   skipped.
3. **Map Remote** — URL rewritten; modified request continues.
4. **Map Local** — local response returned immediately; request NOT
   forwarded; response-phase rules still execute on the local response.
5. **Breakpoint** — request paused for user editing; after resume,
   continues through the pipeline.
6. **Script** — `OnRequest` executes; may modify request or return a mock
   response (short-circuits the server call).
7. **No Caching** — strips cache headers from the request.

**Response phase:**
1. **No Caching** — strips cache headers from the response.
2. **Script** — `OnResponse` executes; may modify the response.
3. **Breakpoint** — response paused for user editing.

## Analysis

1. **Order invariance.** The order above is the contract. Adding a rule
   slot anywhere in the middle is a Phase-3 stop-and-ask change. Flag any
   change that reorders, splits, or merges slots without a documented ADR.

2. **Short-circuit correctness.** `RuleEngine.EvaluateRequest` stops on
   `Block` and `ServeLocalResponse`. Confirm that `Redirect` and
   `ModifyRequest` flow `currentRequest` to subsequent rules. Confirm
   `EvaluateResponse` flows `currentResponse` through `ModifyResponse`
   actions.

3. **Action discriminated unions.** `RequestPipelineAction` and
   `ResponsePipelineAction` are closed unions. Adding a case ripples to
   every `switch` and pattern match. Flag a new case that is not
   exhaustively handled by the engine and by every consumer.

4. **Mutability boundary.** Rules come in two flavours: an immutable
   `XxxRule` for the engine and a `MutableXxxRule` for the UI to edit.
   The mutable form publishes a `MutableXxxChanged` event when modified;
   the registry rebuilds the immutable snapshot. Flag any path that
   mutates the immutable form, or that mutates the mutable form without
   publishing the change event.

5. **Matching helpers.** `Matching/` types own host/method/status
   matching. Flag in-line regex / glob matching inside a rule that
   should delegate to a matcher.

6. **Map Local file references.** `MapLocalEntry` carries a file path.
   Validate:
   - The path is validated by `IMapLocalFileProvider` before reading.
   - The file size is capped before loading into memory.
   - The response is constructed with the correct content type
     (extension-derived or explicit).

7. **Map Remote rewrites.** `MapRemoteUriRewriter` and
   `MapRemoteHeaderRewriter` own URI and header rewriting. Flag any
   inline rewrite that should go through them.

8. **Breakpoint inbox.** `BreakpointPauseInbox` is the queue of paused
   requests/responses awaiting user action. Validate:
   - Adding a pause publishes `BreakpointPauseInboxChanged`.
   - Resuming a pause flows the resumed request/response back into the
     pipeline correctly.
   - A configured timeout drops the pause to "no modification" rather
     than hanging the connection forever.

9. **`MutableBreakpointConfiguration`.** Drives which phases of which
   hosts pause. Flag any path that reads the configuration on the hot
   path instead of caching a snapshot.

10. **No Caching.** Strips `Cache-Control`, `Pragma`, `If-Modified-Since`,
    `If-None-Match` on the request side; strips `Cache-Control`,
    `Expires`, `Last-Modified`, `ETag` on the response side. Confirm the
    header set is the canonical one and matches the user's expectation
    in the docs.

11. **Cross-rule interactions.**
    - Allow List + Block List — Allow List runs first; if allowed, Block
      List still applies (Block wins for a specific host).
    - Map Remote + Map Local — Map Remote rewrites the URI first; if the
      rewritten URI matches Map Local, the local response is served.
    - Map Local + Breakpoint / Script — response-phase rules still fire
      on the locally-served response.
    - Block List short-circuits everything downstream.

    Flag any code path that breaks one of these interactions.

12. **Registry change notification.** `RuleRegistryChanged` fires when
    rules are added/removed/reordered. Subscribers (the engine snapshot,
    the UI rule list) rebuild accordingly. Confirm no subscriber retains
    a stale snapshot across a change.
