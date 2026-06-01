# Performance checklist

Detailed reference for the `performance` skill, covering both whole-codebase
analysis and PR-diff review.

## Analysis

1. **Algorithmic complexity.** Flag `O(n²)` or worse where a better complexity
   is reachable. The traffic store and the rule registry are both candidates
   — every accept and every request walks one of them. Avoid full scans on
   data structures that support indexed access (`Dictionary`, `HashSet`,
   `SortedList`).

2. **Excessive allocations.** Detect repeated allocation on the proxy hot
   path. Specific targets:
   - Per-frame `byte[]` allocation in `Framework.Networking` — pool through
     `ArrayPool<byte>.Shared` or use the pipe's own buffer.
   - Materialising a `string` for every header line when only a comparison
     is needed — work in `ReadOnlySpan<byte>` until comparison time.
   - LINQ in production code (banned by `s6602` at error severity);
     allocations from `Select`, `Where`, `ToArray`, `ToList` would defeat
     the budget anyway.
   - Closures capturing state needlessly — confirm captured variables are
     genuinely needed inside the lambda.
   - Large temporary buffers without pooling.

3. **Blocking calls.** Synchronous I/O on the proxy pipeline is a stall:
   - `Stream.Read` / `Stream.Write` (use the async overloads on the pipe).
   - File system access from the dispatcher thread.
   - `Thread.Sleep` (banned in tests too).
   - `Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.

4. **Sync-over-async.** Same primitives as above on `Task`-returning APIs.
   Catalogued as a separate category because the deadlock risk on the
   listener dispatcher is severe.

5. **Async misuse.** Flag:
   - `async void` outside an Avalonia event handler.
   - Methods marked `async` that just `return await x` with no other awaits
     — return the task directly.
   - Missing `ConfigureAwait(false)` in library code that does not capture a
     synchronisation context.

6. **N+1 access patterns.** Loops that make one I/O call per item where a
   batched call would suffice. The traffic store is append-mostly so this
   is less common, but the HAR exporter and the certificate cache can both
   exhibit N+1 behaviour during bulk operations.

7. **Cache misuse.** Flag:
   - Missing caches on expensive repeated computations (HPACK Huffman
     decode tables, content-decoder lookups by content-type, …).
   - Incorrect cache invalidation — the `LeafCertificateCache` LRU is the
     reference pattern; deviations should justify themselves.
   - Unbounded cache growth.
   - `ConcurrentDictionary` lookups on every call without a local variable
     to hold the result within a scope.

8. **Rendering hotspots (Avalonia).** Flag:
   - Bitmap allocations per render frame.
   - Layout passes triggered by excessive `PropertyChanged` notifications —
     pair bulk updates with `SuspendNotifications` / `ResumeNotifications`.
   - Missing `VirtualizingStackPanel` on `ItemsControl` for large
     collections (traffic flow list, scripting diagnostics, certificate
     manager).
   - Heavy work on the UI thread; offload via `Task.Run` then marshal back
     before touching bound properties.
   - Bindings whose path is a computed property that re-evaluates on every
     layout pass.

9. **Throttle accuracy.** `Domain.Throttling/TokenBucket` is the primitive.
   Flag:
   - Per-tick allocations inside the bucket update path.
   - Token deductions that hold a `lock` across an `await`.
   - Throttle decisions made outside a per-connection scope.

10. **Notification overhead.** Bulk updates to `ObservableCollection<T>`
    raise one notification per item. Use the buffered observable-collection
    helpers in `Framework` (or `SuspendNotifications` if the
    `ObservableEntity`-style helper is available) for bulk loads.

11. **HAR import / export.** Streaming write is the contract — never load a
    full HAR document into memory before writing. The session save/load
    budget (< 5 s for 10 K flows) demands streaming.

## Forbidden silencers in proposed fixes

LINQ is not an acceptable optimisation (`s6602`). `params` is not an
acceptable simplification (`ATXCS055`). Default parameter values are not an
acceptable convenience (`ATXCS057`). When the simplest answer would violate
the analyzer rules, the answer is wrong — find another.
