# Code-health rule checklist

Use this checklist when evaluating code under the `code-health` skill. Each
item maps to an enforced analyzer ID or to a Proxyfan convention.

1. **Naming.** Identifiers should express intent. Flag abbreviations beyond
   well-known ones (HTTP, TLS, ALPN, SNI, HPACK, SOCKS, SSE, gRPC, HAR,
   DPAPI, MITM). Domain types in `Domain.Traffic`, `Domain.Rules`, and
   `Domain.Proxy` should match the user-facing vocabulary (`TrafficFlow`,
   `BreakpointPause`, `MapLocalRule`, …). Check identifiers against the
   no-vague-abbreviation analyzer.

2. **Method size and complexity.** Methods that exceed ~50 non-blank lines
   should be split. Nesting deeper than three levels indicates extraction is
   needed. Use early returns / guard clauses to reduce nesting before adding
   branches.

3. **In-file duplication.** Repeated control structures within a single
   file should be extracted to a private helper or a constant. Cross-file
   structural duplication belongs to `code-duplication`.

4. **Dead code.** Flag unused locals, unreachable branches, commented-out
   code, and unused methods. Particularly common after a protocol parser
   refactor — old helpers linger.

5. **Abstraction shape.** Flag leaky abstractions, missing interfaces where
   polymorphism would reduce coupling, wrappers that delegate 100 % to an
   inner type, and "thick" abstractions that hide too much behaviour. The
   `Framework.Networking` codebase has many thin, single-purpose types
   (`HypertextTransferProtocolMethodPrefixDetector`,
   `ConnectTargetValidator`, `AcceptErrorClassifier`); follow that grain.

6. **Parameter count.** Callables max out at four parameters (`ATXCS022`).
   For longer lists, prefer a `*Dependencies` record (see existing examples
   in `Framework.Networking`).

7. **Anonymous tuple types.** Forbidden (`ATXCS012`). Use a named `record`
   or `record struct` for multi-value returns.

8. **Arrow methods.** `IDE0022` errors on methods that have parameters and
   use an arrow body. Properties may use arrow bodies.

9. **Ternary usage.** `IDE0045` errors on ternaries for assignment;
   `IDE0046` errors on ternaries for return. Use `if`/`else`.

10. **Braces.** `S121` errors on `if` / `else` / `for` / `foreach` / `while`
    bodies without curly braces.

11. **`out` / `ref` parameters.** Single `out` allowed, must be last
    (`ATXCS023` / `ATXCS024`). Single `ref` allowed (`ATXCS027`), must be
    first (`ATXCS026`); `ref` forbidden outside `SetProperty` (`ATXCS025`).

12. **Blank-line discipline.** Maximum one blank line between two
    constructs (`ATXCS044`).

13. **Default parameter values.** Forbidden (`ATXCS057`). Every caller
    passes every argument.

14. **Inline `new` in arguments.** Forbidden (`ATXCS058`). Assign to a
    named local first.

15. **Object/collection initialiser formatting.** Each member on its own
    line (`ATXCS059`); empty `{}` forbidden (`ATXCS060`).

16. **`[SuppressMessage]`.** Forbidden across `src/` and `tests/`. Fix the
    root cause.

17. **Vague type suffixes.** `Service`, `Helper`, `Helpers`, `Util`,
    `Utils`, `Utilities` are forbidden. Use precise vocabulary listed in
    `architecture.instructions.md`.

18. **`#pragma warning disable`.** Forbidden in `src/` and `tests/` per the
    repo `AGENTS.md` rule.

## Cross-cutting checks

- **File placement.** Detect misplaced types — a class in the wrong project,
  namespace, or folder. `Framework.Networking` is the home for all wire
  parsers; `Domain.Traffic` hosts in-memory store types; `Domain.Rules`
  hosts pipeline-action discriminated unions.
- **Magic numbers / strings.** Hard-coded HTTP status codes, byte counts,
  header names, and content types should be named constants. Many already
  live in `Domain.Traffic` and `Framework.Networking` — reuse before
  introducing a fresh constant.
- **Consistency with neighbours.** New code should follow the patterns of
  surrounding files: same error-handling shape, same logging style, same
  asynchronous idioms. Flag drift.
